// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Threading.Channels;

namespace PalClam.Services;

// Monitor reactivo del usuario actual. No filtra escrituras, no mata procesos y no crea archivos señuelo.
internal sealed class RansomwareMonitor : IDisposable
{
    private readonly RansomwareOptions _options;
    private readonly RansomwarePolicy _policy;
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly Channel<FileActivity> _events = Channel.CreateBounded<FileActivity>(new BoundedChannelOptions(4096)
        { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
    private readonly CancellationTokenSource _stop = new();
    private readonly Action<RansomwareAlert> _alert;
    private readonly Action<string> _health;
    private readonly Dictionary<string, DateTimeOffset> _sampled = new(StringComparer.OrdinalIgnoreCase);
    private Task? _worker;
    private int _disposed, _degraded;
    private long _processed, _lost, _sampleFailures;
    internal bool IsRunning => Volatile.Read(ref _disposed) == 0 && _watchers.Count > 0;
    internal bool IsDegraded => Volatile.Read(ref _degraded) != 0;
    internal long Processed => Interlocked.Read(ref _processed);
    internal long Lost => Interlocked.Read(ref _lost);
    internal long SampleFailures => Interlocked.Read(ref _sampleFailures);
    internal int WatchedFolders => _watchers.Count;
    internal RansomwareMonitor(RansomwareOptions options, Action<RansomwareAlert> alert, Action<string> health)
    { _options = RansomwareOptions.Normalize(options); _policy = new(_options); _alert = alert; _health = health; }

    internal void Start()
    {
        if (_worker is not null) throw new InvalidOperationException("El monitor ya se ha iniciado.");
        foreach (var raw in _options.Folders.OrderBy(p => p.Length))
        {
            try
            {
                var path = System.IO.Path.GetFullPath(raw); EnsureOrdinaryPath(path);
                if (!Directory.Exists(path)) throw new DirectoryNotFoundException(path);
                if (OperatingSystem.IsWindows())
                {
                    var root = System.IO.Path.GetPathRoot(path)!;
                    if (new DriveInfo(root).DriveType != DriveType.Fixed || path.TrimEnd('\\').Equals(root.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                        throw new IOException("Elige carpetas de datos de un disco local fijo, no la raíz de una unidad.");
                }
                // Evitar duplicar observaciones de carpetas anidadas.
                if (_watchers.Any(w => IsUnder(path, w.Path))) continue;
                var watcher = new FileSystemWatcher(path)
                {
                    IncludeSubdirectories = true, InternalBufferSize = 32768,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
                };
                watcher.Created += (_, e) => Enqueue(e.FullPath, "", FileActivityKind.Created);
                watcher.Changed += (_, e) => Enqueue(e.FullPath, "", FileActivityKind.Changed);
                watcher.Deleted += (_, e) => Enqueue(e.FullPath, "", FileActivityKind.Deleted);
                watcher.Renamed += (_, e) => Enqueue(e.FullPath, e.OldFullPath, FileActivityKind.Renamed);
                watcher.Error += (_, _) => Degrade("Se han perdido eventos o una carpeta dejó de estar disponible. Reinicia el monitor tras revisar las rutas.");
                try { watcher.EnableRaisingEvents = true; _watchers.Add(watcher); }
                catch { watcher.Dispose(); throw; }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or System.ComponentModel.Win32Exception or System.Security.SecurityException)
            { Degrade("Carpeta sin vigilancia: " + raw + " · " + ex.Message); }
        }
        _worker = Task.Run(ProcessAsync);
        if (_watchers.Count == 0) Degrade("No hay carpetas operativas; el monitor no está vigilando.");
    }

    private void Enqueue(string path, string oldPath, FileActivityKind kind)
    {
        if (Volatile.Read(ref _disposed) != 0 || _options.ExcludedFolders.Any(excluded => IsUnder(path, excluded))) return;
        if (!_events.Writer.TryWrite(new(DateTimeOffset.UtcNow, path, oldPath, kind, false)))
        { Interlocked.Increment(ref _lost); Degrade("Cola de eventos llena; puede haber cambios sin observar."); }
    }
    private void Degrade(string message)
    {
        if (Interlocked.Exchange(ref _degraded, 1) == 0) _health(message);
    }
    private async Task ProcessAsync()
    {
        var token = _stop.Token; var sampleWindow = DateTimeOffset.UtcNow; var samples = 0;
        try
        {
            await foreach (var activity in _events.Reader.ReadAllAsync(token))
            {
                if (token.IsCancellationRequested) break;
                // No seguir enlaces ni abrir puntos de reanálisis, tampoco para tomar muestras.
                try { EnsureOrdinaryPath(activity.Path, allowMissingLeaf: activity.Kind == FileActivityKind.Deleted || !File.Exists(activity.Path)); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException) { continue; }
                if (Directory.Exists(activity.Path)) continue;
                var evidence = activity;
                var now = DateTimeOffset.UtcNow;
                if ((now - sampleWindow).TotalSeconds >= 1) { sampleWindow = now; samples = 0; }
                if (_options.SampleEntropy && samples < 20 && activity.Kind != FileActivityKind.Deleted &&
                    (RansomwarePolicy.PlainTextExtension(activity.Path) || RansomwarePolicy.SuspiciousName(activity.Path, activity.OldPath)) &&
                    (!_sampled.TryGetValue(activity.Path, out var last) || now - last >= TimeSpan.FromMilliseconds(750)))
                {
                    samples++; if (_sampled.Count >= 4096) _sampled.Clear();
                    try
                    {
                        using var input = new FileStream(activity.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                        var bytes = new byte[65536]; var count = input.Read(bytes);
                        // Una notificación puede llegar antes de terminar la escritura: no suprimir
                        // el siguiente intento si esta muestra aún está vacía o es demasiado corta.
                        if (count >= 1024) _sampled[activity.Path] = now;
                        evidence = activity with { HighEntropy = count >= 1024 && RansomwarePolicy.Entropy(bytes.AsSpan(0, count)) >= 7.6 };
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Interlocked.Increment(ref _sampleFailures); }
                }
                Interlocked.Increment(ref _processed);
                var alert = _policy.Observe(evidence);
                if (alert is not null && !token.IsCancellationRequested) _alert(alert);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex) { Degrade("El monitor dejó de procesar eventos: " + ex.Message); }
    }
    internal static bool IsUnder(string path, string root)
    {
        try
        {
            var full = System.IO.Path.GetFullPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            var parent = System.IO.Path.GetFullPath(root).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            return full.Equals(parent, StringComparison.OrdinalIgnoreCase) ||
                full.StartsWith(parent + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException) { return false; }
    }
    private static void EnsureOrdinaryPath(string path, bool allowMissingLeaf = false)
    {
        for (var current = System.IO.Path.GetFullPath(path); !string.IsNullOrEmpty(current); current = System.IO.Path.GetDirectoryName(current))
        {
            FileAttributes attributes;
            try { attributes = File.GetAttributes(current); }
            catch (Exception ex) when (allowMissingLeaf && ex is FileNotFoundException or DirectoryNotFoundException) { continue; }
            if ((attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Punto de reanálisis omitido.");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        foreach (var watcher in _watchers) watcher.Dispose();
        _stop.Cancel(); _events.Writer.TryComplete();
        if (_worker is null) _stop.Dispose(); else _ = _worker.ContinueWith(_ => _stop.Dispose(), TaskScheduler.Default);
    }
}
