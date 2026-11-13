// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

namespace PalClam.MachineService;

/// <summary>
/// Experimental machine-wide REACTIVE watcher. Watches ONLY the dedicated
/// shared folder. No quarantine, deletion, command execution or user-profile
/// access. One ClamAV child process at a time, bounded backlog.
/// </summary>
internal sealed class MachineGuard : IDisposable
{
    private const int QueueLimit = 256;
    private readonly Channel<string> _queue = Channel.CreateBounded<string>(
        new BoundedChannelOptions(QueueLimit)
        {
            SingleReader = true, SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    private readonly ConcurrentDictionary<string, long> _pending =
        new(StringComparer.OrdinalIgnoreCase);
    private FileSystemWatcher? _watcher;
    private long _scanned, _detections, _skipped, _errors, _lost;
    private int _degraded;
    private string? _lastCoreIssue;
    private int _reconcileRequested = 1; // Revisar también archivos existentes tras cada arranque.
    private int _reconciling;
    private long _reconciliations;
    private const int MaxReconcileFiles = 2_000;

    public async Task<int> RunAsync(CancellationToken cancellation, Action? onReady = null)
    {
        try
        {
            // The install helper creates and ACLs these directories. The
            // low-privilege service may not change their ownership or ACLs.
            if (!Directory.Exists(ServicePaths.WatchedFolder) ||
                !Directory.Exists(ServicePaths.StateFolder))
                throw new InvalidOperationException("Faltan carpetas de servicio. Reinstala desde Opciones con permisos de administrador.");
            if (!File.Exists(ServicePaths.ScannerPath))
                throw new InvalidOperationException("No está instalado clamscan.exe junto al servicio.");
            if (!ServicePaths.SignaturesPresent())
                throw new InvalidOperationException("Faltan firmas compartidas main/daily. Instala o sincroniza las firmas desde PalClam.");

            // Ensure writable diagnostics before telling SCM that monitoring is live.
            MachineGuardReport.Append("INFO", "Servicio experimental iniciado. Vigila solo " + ServicePaths.WatchedFolder);
            _watcher = new FileSystemWatcher(ServicePaths.WatchedFolder)
            {
                Filter = "*", IncludeSubdirectories = true,
                InternalBufferSize = 16 * 1024,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = false
            };
            _watcher.Created += (_, e) => Enqueue(e.FullPath);
            _watcher.Changed += (_, e) => Enqueue(e.FullPath);
            _watcher.Renamed += (_, e) => Enqueue(e.FullPath);
            _watcher.Error += (_, e) =>
            {
                Interlocked.Increment(ref _lost);
                Interlocked.Exchange(ref _degraded, 1);
                Interlocked.Exchange(ref _reconcileRequested, 1);
                SafeLog("WARNING", "FileSystemWatcher perdió eventos: " + e.GetException()?.Message);
            };
            _watcher.EnableRaisingEvents = true;
            Report("Reconciliando", "Revisión inicial pendiente; no se confirma cobertura completa.");
            onReady?.Invoke();

            var consumer = ConsumeAsync(cancellation);
            var reconciliation = ReconcileLoopAsync(cancellation);
            try
            {
                while (!cancellation.IsCancellationRequested)
                {
                    // If the background consumer died, never claim the watcher is healthy.
                    if (consumer.IsCompleted || reconciliation.IsFaulted)
                        throw new InvalidOperationException("Se ha detenido el procesamiento o la revisión de archivos.");
                    // Un servicio iniciado puede perder el motor o las firmas si una
                    // actualización/instalación falla después del arranque.
                    // Evitar un falso estado "Vigilando" incluso sin nuevos archivos.
                    var coreIssue = MachineGuardPolicy.CoreIssue(
                        File.Exists(ServicePaths.ScannerPath), ServicePaths.SignaturesPresent());
                    if (!string.Equals(coreIssue, _lastCoreIssue, StringComparison.Ordinal))
                    {
                        if (coreIssue is not null)
                        {
                            Interlocked.Exchange(ref _degraded, 1);
                            SafeLog("ERROR", "Machine Guard no operativo: " + coreIssue);
                        }
                        else if (_lastCoreIssue is not null)
                            SafeLog("INFO", "Motor y firmas recuperados; se requiere revisar el historial de errores.");
                        _lastCoreIssue = coreIssue;
                    }
                    var recovering = Volatile.Read(ref _reconciling) != 0 ||
                        Volatile.Read(ref _reconcileRequested) != 0;
                    var degraded = Volatile.Read(ref _degraded) != 0 ||
                        Interlocked.Read(ref _skipped) > 0 || Interlocked.Read(ref _errors) > 0;
                    Report(coreIssue is not null ? "Error" : recovering ? "Reconciliando" : degraded ? "Degradado" : "Vigilando",
                        coreIssue is not null ? coreIssue + " No se garantiza el análisis hasta repararlo." :
                        recovering ? "Revisión limitada tras inicio o pérdida de eventos; no garantiza cobertura." :
                        degraded ? "Hay eventos perdidos, archivos omitidos o errores. Realiza un análisis manual." :
                        "Vigilancia reactiva activa; no bloqueo preventivo.");
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellation);
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            finally
            {
                _watcher.EnableRaisingEvents = false;
                _queue.Writer.TryComplete();
                try { await consumer.WaitAsync(TimeSpan.FromSeconds(5)); }
                catch (TimeoutException) { SafeLog("WARNING", "El consumidor no terminó durante la detención."); }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
                try { await reconciliation.WaitAsync(TimeSpan.FromSeconds(5)); }
                catch (TimeoutException) { SafeLog("WARNING", "La revisión seguía terminando al cerrar el servicio."); }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            }
            Report("Detenido", "Servicio detenido a petición de Windows.");
            SafeLog("INFO", "Servicio detenido.");
            return 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SafeLog("ERROR", "Servicio no operativo: " + ex.Message);
            try { Report("Error", ex.Message); } catch { }
            return 1;
        }
    }

    private void Enqueue(string path)
    {
        try
        {
            var full = Path.GetFullPath(path);
            if (!WithinWatch(full)) return;
            var tick = DateTime.UtcNow.Ticks;
            if (!_pending.TryAdd(full, tick))
            {
                // An event during a scan must cause another check once the
                // in-flight scan has finished. Never silently claim coverage.
                _pending.AddOrUpdate(full, tick, (_, prior) => Math.Max(prior, tick));
                return;
            }
            if (!_queue.Writer.TryWrite(full))
            {
                _pending.TryRemove(full, out _);
                Interlocked.Increment(ref _lost);
                Interlocked.Exchange(ref _degraded, 1);
                Interlocked.Exchange(ref _reconcileRequested, 1);
                SafeLog("WARNING", "Cola saturada. Realiza un análisis manual; no se garantiza cobertura.");
            }
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException)
        {
            Interlocked.Increment(ref _errors);
            SafeLog("WARNING", "Evento inválido: " + ex.Message);
        }
    }

    /// <summary>
    /// Revisión de recuperación de mejor esfuerzo. Nunca marca como limpio todo el
    /// árbol: la cola puede saturarse y los ficheros pueden modificarse durante ella.
    /// No sigue enlaces simbólicos/junctions y aplica un límite explícito.
    /// </summary>
    private async Task ReconcileLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (Interlocked.Exchange(ref _reconcileRequested, 0) == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), token);
                    continue;
                }
                Interlocked.Exchange(ref _reconciling, 1);
                try
                {
                    var options = new EnumerationOptions
                    {
                        RecurseSubdirectories = true, IgnoreInaccessible = false,
                        AttributesToSkip = FileAttributes.ReparsePoint
                    };
                    var count = 0;
                    foreach (var path in Directory.EnumerateFiles(ServicePaths.WatchedFolder, "*", options))
                    {
                        token.ThrowIfCancellationRequested();
                        if (++count > MaxReconcileFiles)
                        {
                            Interlocked.Exchange(ref _degraded, 1);
                            SafeLog("WARNING", "Revisión parcial: límite de " + MaxReconcileFiles +
                                " archivos. Se necesita un análisis manual.");
                            break;
                        }
                        var full = Path.GetFullPath(path);
                        if (!WithinWatch(full) || !_pending.TryAdd(full, DateTime.UtcNow.Ticks))
                            continue;
                        try { await _queue.Writer.WriteAsync(full, token); }
                        catch (ChannelClosedException) when (token.IsCancellationRequested)
                        {
                            _pending.TryRemove(full, out _);
                            throw new OperationCanceledException(token);
                        }
                        catch { _pending.TryRemove(full, out _); throw; }
                    }
                    Interlocked.Increment(ref _reconciliations);
                    SafeLog("INFO", "Revisión de recuperación encolada; no constituye escaneo completo garantizado.");
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Interlocked.Exchange(ref _degraded, 1);
                    SafeLog("WARNING", "No se pudo completar revisión de recuperación: " + ex.Message);
                }
                finally { Interlocked.Exchange(ref _reconciling, 0); }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    private static bool WithinWatch(string path)
    {
        return MachineGuardPolicy.IsInside(ServicePaths.WatchedFolder, path);
    }

    private static bool HasReparseAncestor(string path)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(ServicePaths.WatchedFolder));
        var current = Path.GetDirectoryName(path);
        while (!string.IsNullOrEmpty(current) &&
               current.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return true;
            if (current.Equals(root, StringComparison.OrdinalIgnoreCase)) break;
            current = Path.GetDirectoryName(current);
        }
        return false;
    }

    private async Task ConsumeAsync(CancellationToken token)
    {
        try
        {
            await foreach (var path in _queue.Reader.ReadAllAsync(token))
            {
                var startTick = DateTime.UtcNow.Ticks;
                try { await ProcessPathAsync(path, token); }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _errors);
                    SafeLog("ERROR", Path.GetFileName(path) + ": " + ex.Message);
                }
                finally
                {
                    if (_pending.TryRemove(path, out var lastEvent) &&
                        lastEvent > startTick && !token.IsCancellationRequested)
                        Enqueue(path);
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    private static (long Length, DateTime Modified)? Snapshot(string path)
    {
        if (!File.Exists(path)) return null;
        var attributes = File.GetAttributes(path);
        if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0) return null;
        if (HasReparseAncestor(path)) return null;
        var info = new FileInfo(path);
        info.Refresh();
        return info.Exists ? (info.Length, info.LastWriteTimeUtc) : null;
    }

    private async Task ProcessPathAsync(string path, CancellationToken token)
    {
        if (!WithinWatch(path)) return;
        (long Length, DateTime Modified)? last = null;
        var stable = 0;
        for (var attempt = 0; attempt < 12; attempt++)
        {
            token.ThrowIfCancellationRequested();
            (long Length, DateTime Modified)? now;
            try { now = Snapshot(path); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                await Task.Delay(300, token);
                continue;
            }
            if (now is null) return;
            if (!MachineGuardPolicy.CanScan(now.Value.Length, false))
            {
                Interlocked.Increment(ref _skipped);
                SafeLog("WARNING", "Archivo no analizado (>100 MB): " + path);
                return;
            }
            stable = now == last ? stable + 1 : 0;
            last = now;
            if (stable >= 2) break;
            await Task.Delay(300, token);
        }
        if (stable < 2)
        {
            Interlocked.Increment(ref _skipped);
            SafeLog("WARNING", "Archivo inestable o inaccesible: " + path);
            return;
        }

        var issue = MachineGuardPolicy.CoreIssue(
            File.Exists(ServicePaths.ScannerPath), ServicePaths.SignaturesPresent());
        if (issue is not null)
        {
            Interlocked.Increment(ref _skipped);
            Interlocked.Exchange(ref _degraded, 1);
            SafeLog("WARNING", "Análisis omitido por componente no disponible: " + issue);
            return;
        }

        using var scanTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        scanTimeout.CancelAfter(TimeSpan.FromMinutes(2));
        var info = new ProcessStartInfo(ServicePaths.ScannerPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var arg in new[] { "--stdout", "--no-summary", "--database=" + ServicePaths.SignaturesFolder,
            "--scan-archive=yes", path })
            info.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = info };
        if (!process.Start()) throw new IOException("No pudo iniciarse clamscan.exe.");
        try
        {
            var output = process.StandardOutput.ReadToEndAsync(scanTimeout.Token);
            var errors = process.StandardError.ReadToEndAsync(scanTimeout.Token);
            await process.WaitForExitAsync(scanTimeout.Token);
            var standardOutput = await output;
            var standardError = await errors;
            if (process.ExitCode == 1)
            {
                Interlocked.Increment(ref _detections);
                Interlocked.Increment(ref _scanned);
                SafeLog("ALERTA", "Detección ClamAV: " + path + " " + BoundedDiagnostic(standardOutput));
            }
            else if (process.ExitCode == 0 && string.IsNullOrWhiteSpace(standardError))
            {
                Interlocked.Increment(ref _scanned);
            }
            else
            {
                Interlocked.Increment(ref _errors);
                SafeLog("ERROR", "Análisis no concluyente: " + path + " (código " + process.ExitCode + ") " + BoundedDiagnostic(standardError));
            }
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            try { await process.WaitForExitAsync(CancellationToken.None); } catch { }
            if (!token.IsCancellationRequested)
            {
                Interlocked.Increment(ref _errors);
                SafeLog("ERROR", "Tiempo de espera agotado al analizar: " + path);
            }
        }
    }

    private static string BoundedDiagnostic(string text) =>
        text.Length <= 2_000 ? text.Trim() : text[..2_000].Trim() + " [salida recortada]";

    private void Report(string mode, string detail) => MachineGuardReport.WriteStatus(new(
        mode, DateTimeOffset.UtcNow, detail,
        Interlocked.Read(ref _scanned), Interlocked.Read(ref _detections),
        Interlocked.Read(ref _skipped), Interlocked.Read(ref _errors),
        Interlocked.Read(ref _lost), ServicePaths.WatchedFolder,
        "Firmas compartidas (renovación administrativa, no automática)")
    {
        Pending = _pending.Count,
        Reconciliations = Interlocked.Read(ref _reconciliations)
    });

    private static void SafeLog(string level, string detail)
    {
        try { MachineGuardReport.Append(level, detail); } catch { /* Never fail the SCM callback. */ }
    }

    public void Dispose() => _watcher?.Dispose();
}
