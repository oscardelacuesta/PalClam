// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace PalClam.Services;

internal enum GuardOutcome { Clean, Threat, Skipped, Error, Warning }

internal sealed record GuardFinding(
    DateTimeOffset When, GuardOutcome Outcome, string FilePath, string Detail,
    long FileLength = -1, DateTime LastWriteUtc = default)
{
    public string DisplayTime => When.ToLocalTime().ToString("dd/MM HH:mm:ss");
    public string DisplayOutcome => Outcome switch
    {
        GuardOutcome.Clean => "Sin detecciones",
        GuardOutcome.Threat => "AMENAZA",
        GuardOutcome.Skipped => "No analizado",
        GuardOutcome.Error => "Error",
        _ => "Advertencia"
    };
}

internal sealed record GuardConfiguration(
    IReadOnlyList<string> Folders, string Scanner, string DatabaseDirectory,
    bool ScanArchives, bool DetectPua, IReadOnlyList<string> Exclusions,
    int MaxFileMegabytes, string ProcessingMode = GuardTuning.DefaultMode);

/// <summary>
/// Vigilancia de archivos mientras la aplicación está abierta. NO intercepta lecturas,
/// NO bloquea su ejecución y NO sustituye una protección on-access de Windows.
/// Un solo análisis por vez, cola limitada, coalescencia y detección de pérdida de eventos.
/// </summary>
internal sealed class GuardService : IDisposable
{
    private const int MaxStabilityTries = 16;
    private readonly GuardTuning _tuning;
    private readonly GuardConfiguration _config;
    private readonly Action<GuardFinding> _publish;
    private readonly Func<bool> _pauseWhileBusy;
    private readonly Channel<string> _queue;
    private readonly ConcurrentDictionary<string, long> _pending =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, FileSnapshot> _lastScanned =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource _cancellation = new();
    private readonly List<FileSystemWatcher> _watchers = new();
    private Task? _worker;
    private int _stopped;
    private int _degraded;
    private int _paused;
    private int _processing;
    private long _coalesced;
    private long _scanned;
    private long _detections;
    private long _skipped;
    private long _failures;
    private long _overflow;

    private readonly record struct FileSnapshot(long Length, DateTime ModifiedUtc);

    public IReadOnlyList<string> WatchedFolders => _config.Folders;
    public bool IsRunning => Volatile.Read(ref _stopped) == 0 && _worker is not null;
    public bool IsDegraded => Volatile.Read(ref _degraded) != 0;
    public long Scanned => Interlocked.Read(ref _scanned);
    public long Detections => Interlocked.Read(ref _detections);
    public long Skipped => Interlocked.Read(ref _skipped);
    public long Failures => Interlocked.Read(ref _failures);
    public long LostEvents => Interlocked.Read(ref _overflow);
    public long CoalescedEvents => Interlocked.Read(ref _coalesced);
    public int QueueCapacity => _tuning.QueueCapacity;
    public string ProcessingMode => _tuning.Mode;
    public bool IsPaused => Volatile.Read(ref _paused) != 0;
    // Approximate UI telemetry. Includes only pending work, not completed events.
    public int PendingCount => _pending.Count;
    public int QueuedCount => Math.Max(0, _pending.Count - Volatile.Read(ref _processing));
    public bool IsProcessing => Volatile.Read(ref _processing) != 0;

    public GuardService(GuardConfiguration configuration, Action<GuardFinding> publish, Func<bool> pauseWhileBusy)
    {
        _config = configuration;
        _publish = publish;
        _pauseWhileBusy = pauseWhileBusy;
        _tuning = GuardTuning.ForMode(configuration.ProcessingMode);
        _queue = Channel.CreateBounded<string>(new BoundedChannelOptions(_tuning.QueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    /// <summary>Pause only new processing. Watchers keep receiving events;
    /// an active clamscan is allowed to complete. Long pauses can overflow.
    /// </summary>
    public void PauseProcessing()
    {
        if (IsRunning) Volatile.Write(ref _paused, 1);
    }

    public void ResumeProcessing() => Volatile.Write(ref _paused, 0);

    public void Start()
    {
        if (_worker is not null) throw new InvalidOperationException("Guard ya se inició.");
        if (!ClamAvLocator.IsValidScanner(_config.Scanner) || !SignatureStore.HasSignatures())
            throw new InvalidOperationException("Guard requiere ClamAV y firmas disponibles. Actualízalos en Motor y firmas.");
        if (!File.Exists(_config.Scanner) || !Directory.Exists(_config.DatabaseDirectory))
            throw new InvalidOperationException("No se encuentra el motor o la base de firmas.");
        if (_config.Folders.Count is < 1 or > 5)
            throw new InvalidOperationException("Elige entre una y cinco carpetas para vigilar.");

        try
        {
            foreach (var folder in _config.Folders)
            {
                ValidateFolder(folder);
                var watcher = new FileSystemWatcher(folder)
                {
                    IncludeSubdirectories = true,
                    InternalBufferSize = 16 * 1024,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size |
                                   NotifyFilters.CreationTime,
                    Filter = "*",
                    EnableRaisingEvents = false
                };
                watcher.Created += OnCreatedOrChanged;
                watcher.Changed += OnCreatedOrChanged;
                watcher.Renamed += OnRenamed;
                watcher.Error += OnWatcherError;
                _watchers.Add(watcher);
            }
            _worker = Task.Run(ConsumeAsync);
            foreach (var watcher in _watchers) watcher.EnableRaisingEvents = true;
        }
        catch
        {
            Stop();
            throw;
        }
    }

    public static List<string> NormalizeFolders(IEnumerable<string> folders)
    {
        var result = new List<string>();
        foreach (var raw in folders)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var value = raw.Trim().Trim('"');
            if (!Path.IsPathFullyQualified(value)) throw new ArgumentException("Indica rutas absolutas de carpetas.");
            var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(value));
            ValidateFolder(full);
            if (!result.Contains(full, StringComparer.OrdinalIgnoreCase)) result.Add(full);
            if (result.Count > 5) throw new ArgumentException("Máximo cinco carpetas vigiladas.");
        }
        if (result.Count == 0) throw new ArgumentException("Selecciona al menos una carpeta.");
        return result;
    }

    private static void ValidateFolder(string folder)
    {
        if (string.Equals(folder, Path.GetPathRoot(folder), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("No se puede vigilar una unidad completa con Guard; usa un análisis manual.");
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException("No existe la carpeta: " + folder);
        if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0)
            throw new ArgumentException("No se vigilan directorios que sean enlaces o puntos de reanálisis.");
        if (IsPalClamPrivate(folder))
            throw new ArgumentException("La carpeta de datos de PalClam no puede vigilarse.");
    }

    private static bool IsPalClamPrivate(string path)
    {
        var privateRoot = Path.Combine(
            PalClam.Services.AppPaths.Root);
        return Under(path, privateRoot);
    }

    private static bool Under(string candidate, string folder) =>
        candidate.Equals(folder, StringComparison.OrdinalIgnoreCase) ||
        candidate.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    private void OnCreatedOrChanged(object sender, FileSystemEventArgs e) => Enqueue(e.FullPath);
    private void OnRenamed(object sender, RenamedEventArgs e) => Enqueue(e.FullPath);

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        MarkDegraded("El monitor de Windows perdió eventos o dejó de vigilar una carpeta: " +
            (e.GetException()?.Message ?? "Error desconocido") +
            ". Revisa la carpeta, reinicia Guard y realiza un análisis manual.");
    }

    private void MarkDegraded(string detail)
    {
        if (Interlocked.Exchange(ref _degraded, 1) == 0)
            Publish(GuardOutcome.Warning, "[VIGILANCIA]", detail);
    }

    private void Enqueue(string path)
    {
        if (Volatile.Read(ref _stopped) != 0) return;
        try
        {
            var full = Path.GetFullPath(path);
            if (IsPalClamPrivate(full) || ScanExclusions.IsExcluded(full, _config.Exclusions)) return;
            var when = DateTime.UtcNow.Ticks;
            if (!_pending.TryAdd(full, when))
            {
                _pending.AddOrUpdate(full, when, (_, previous) => Math.Max(previous, when));
                Interlocked.Increment(ref _coalesced);
                return;
            }
            if (!_queue.Writer.TryWrite(full))
            {
                _pending.TryRemove(full, out _);
                Interlocked.Increment(ref _overflow);
                MarkDegraded("La cola de Guard se llenó. Es posible que haya archivos sin analizar. " +
                    "Realiza un análisis manual y reinicia Guard.");
            }
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException or PathTooLongException)
        {
            Interlocked.Increment(ref _failures);
            Publish(GuardOutcome.Warning, path, "Ruta notificada no válida: " + ex.Message);
        }
    }

    private async Task WaitForProcessingPermissionAsync(CancellationToken ct)
    {
        while (IsPaused || _pauseWhileBusy())
            await Task.Delay(250, ct);
    }

    private async Task WaitForQuietPeriodAsync(string path, CancellationToken ct)
    {
        // New notifications update the same dictionary entry, so a burst
        // of writes does not start a premature scan.
        while (_pending.TryGetValue(path, out var lastEvent))
        {
            await WaitForProcessingPermissionAsync(ct);
            var remaining = _tuning.QuietRemainingMs(DateTime.UtcNow.Ticks, lastEvent);
            if (remaining <= 0) return;
            await Task.Delay(Math.Min(remaining, 250), ct);
        }
    }

    private async Task ConsumeAsync()
    {
        try
        {
            await foreach (var path in _queue.Reader.ReadAllAsync(_cancellation.Token))
            {
                var requeue = false;
                var startScanningAt = long.MaxValue;
                Volatile.Write(ref _processing, 1);
                try
                {
                    await WaitForProcessingPermissionAsync(_cancellation.Token);
                    await WaitForQuietPeriodAsync(path, _cancellation.Token);
                    // One worker, bounded queue, stable-file check and a minimum
                    // gap between scans. Never claim this blocks execution.
                    var stable = await WaitForStableFileAsync(path, _cancellation.Token);
                    if (stable is null) continue;
                    await WaitForProcessingPermissionAsync(_cancellation.Token);
                    var info = stable.Value;
                    startScanningAt = DateTime.UtcNow.Ticks;
                    if (_lastScanned.TryGetValue(path, out var previous) && previous == info)
                        continue;
                    var result = await ScanAsync(path, info, _cancellation.Token);
                    if (result.Outcome is GuardOutcome.Clean or GuardOutcome.Threat)
                    {
                        _lastScanned[path] = info;
                        if (_lastScanned.Count > 3000) _lastScanned.Clear();
                    }
                    if (TrySnapshot(path, out var after) && after != info)
                    {
                        requeue = true;
                        Publish(GuardOutcome.Warning, path,
                            "El archivo cambió durante el análisis: se volverá a comprobar.");
                    }
                    if (_tuning.MinimumScanGapMs > 0)
                        await Task.Delay(_tuning.MinimumScanGapMs, _cancellation.Token);
                }
                catch (OperationCanceledException) when (_cancellation.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _failures);
                    Publish(GuardOutcome.Error, path, "No se pudo completar el análisis: " + ex.Message);
                }
                finally
                {
                    Volatile.Write(ref _processing, 0);
                    if (_pending.TryRemove(path, out var lastChange) && lastChange > startScanningAt)
                        requeue = true;
                    if (requeue && !_cancellation.IsCancellationRequested) Enqueue(path);
                }
            }
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested) { }
        catch (Exception ex)
        {
            MarkDegraded("El proceso de vigilancia se interrumpió: " + ex.Message);
        }
    }

    private async Task<FileSnapshot?> WaitForStableFileAsync(string path, CancellationToken ct)
    {
        FileSnapshot? last = null;
        var consecutive = 0;
        for (var attempt = 0; attempt < MaxStabilityTries; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            if (!File.Exists(path)) return null;  // Puede haberse eliminado o renombrado.
            try
            {
                var attrs = File.GetAttributes(path);
                if ((attrs & FileAttributes.Directory) != 0 || (attrs & FileAttributes.ReparsePoint) != 0)
                    return null;
                if (!TrySnapshot(path, out var snapshot))
                {
                    await Task.Delay(_tuning.StabilityPollMs, ct);
                    continue;
                }
                if (snapshot.Length > _config.MaxFileMegabytes * 1024L * 1024L)
                {
                    Interlocked.Increment(ref _skipped);
                    Publish(GuardOutcome.Skipped, path,
                        $"Tamaño superior al límite de Guard ({_config.MaxFileMegabytes} MB). Analízalo manualmente.");
                    return null;
                }
                consecutive = last.HasValue && snapshot == last.Value ? consecutive + 1 : 0;
                last = snapshot;
                if (consecutive >= 2) return snapshot;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Reintentar mientras el archivo se está copiando o está bloqueado.
                if (attempt == MaxStabilityTries - 1)
                {
                    Interlocked.Increment(ref _skipped);
                    Publish(GuardOutcome.Skipped, path, "Archivo inaccesible: " + ex.Message);
                    return null;
                }
            }
            await Task.Delay(_tuning.StabilityPollMs, ct);
        }
        Interlocked.Increment(ref _skipped);
        Publish(GuardOutcome.Skipped, path,
            "El archivo seguía cambiando o inaccesible; no se declara limpio. Reintenta con un análisis manual.");
        return null;
    }

    private static bool TrySnapshot(string path, out FileSnapshot snapshot)
    {
        snapshot = default;
        try
        {
            var info = new FileInfo(path);
            info.Refresh();
            if (!info.Exists) return false;
            snapshot = new FileSnapshot(info.Length, info.LastWriteTimeUtc);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        { return false; }
    }

    private async Task<GuardFinding> ScanAsync(string path, FileSnapshot snapshot, CancellationToken ct)
    {
        var threats = new List<string>();
        var errors = new List<string>();
        var args = new List<string>
        {
            "--stdout", "--no-summary", "--database=" + _config.DatabaseDirectory,
            "--scan-archive=" + (_config.ScanArchives ? "yes" : "no")
        };
        if (_config.DetectPua) args.Add("--detect-pua=yes");
        args.AddRange(ScanExclusions.BuildClamArguments(_config.Exclusions));
        args.Add(path);
        var result = await ProcessRunner.RunAsync(_config.Scanner, args, (line, stderr) =>
        {
            var parsed = ScanOutputParser.ParseFile(line);
            if (parsed is { Status: ScanFileStatus.Detected })
            {
                lock (threats) { if (threats.Count < 10) threats.Add(parsed.Detail); }
            }
            else if (parsed is { Status: ScanFileStatus.Error } ||
                     (stderr && line.Contains("ERROR", StringComparison.OrdinalIgnoreCase)))
            {
                lock (errors) { if (errors.Count < 5) errors.Add(line); }
            }
        }, ct);
        if (result.ExitCode == 1 || threats.Count > 0)
        {
            Interlocked.Increment(ref _scanned);
            Interlocked.Increment(ref _detections);
            var detail = threats.Count > 0 ? string.Join("; ", threats) : "ClamAV devolvió detección (código 1).";
            if (errors.Count > 0) detail += " · También hubo incidencias: " + errors[0];
            return Publish(GuardOutcome.Threat, path, detail, snapshot);
        }
        if (result.ExitCode != 0 || errors.Count > 0)
        {
            Interlocked.Increment(ref _failures);
            return Publish(GuardOutcome.Error, path,
                "Resultado incompleto (código " + result.ExitCode + "). " +
                (errors.Count > 0 ? errors[0] : "Revisa el motor y las firmas."), snapshot);
        }
        Interlocked.Increment(ref _scanned);
        return Publish(GuardOutcome.Clean, path, "Sin detecciones en este momento.", snapshot);
    }

    private GuardFinding Publish(GuardOutcome outcome, string path, string detail, FileSnapshot? snapshot = null)
    {
        var finding = new GuardFinding(DateTimeOffset.Now, outcome, path, detail,
            snapshot?.Length ?? -1, snapshot?.ModifiedUtc ?? default);
        try { _publish(finding); } catch { /* La interfaz nunca debe bloquear los hilos de archivos. */ }
        return finding;
    }

    public void Stop()
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0) return;
        foreach (var watcher in _watchers)
        {
            try { watcher.EnableRaisingEvents = false; watcher.Dispose(); } catch (Exception) { }
        }
        _watchers.Clear();
        _queue.Writer.TryComplete();
        Volatile.Write(ref _paused, 0);
        _cancellation.Cancel();
    }

    public async Task StopAsync()
    {
        Stop();
        if (_worker is not null)
            try { await _worker; } catch (OperationCanceledException) { }
    }

    public void Dispose() => Stop();
}
