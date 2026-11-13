// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Diagnostics;
using System.Text;

namespace PalClam.Services;

/// <summary>
/// Pausa cooperativa auténtica: deja finalizar el lote EN CURSO y no inicia el siguiente.
/// No se suspenden hilos de Windows ni se finge detener un clamscan que sigue trabajando.
/// </summary>
internal sealed class ScanPauseGate
{
    private readonly object _sync = new();
    private TaskCompletionSource<bool>? _release;
    public bool IsPauseRequested { get { lock (_sync) return _release is not null; } }

    public bool RequestPause()
    {
        lock (_sync)
        {
            if (_release is not null) return false;
            _release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return true;
        }
    }

    public bool Resume()
    {
        TaskCompletionSource<bool>? release;
        lock (_sync) { release = _release; _release = null; }
        if (release is null) return false;
        release.TrySetResult(true);
        return true;
    }

    public async Task WaitIfPausedAsync(Action onPaused, CancellationToken cancel)
    {
        Task? wait;
        lock (_sync) wait = _release?.Task;
        if (wait is null) return;
        onPaused();
        await wait.WaitAsync(cancel);
    }
}

internal sealed record ScanBatchStatus(int BatchNumber, int FilesInBatch, long FilesSubmitted,
    string FirstPath, string LastPath, bool WaitingForResume, long? FileLengthBytes);
internal sealed record ControlledScanResult(int ExitCode, long ScannedFiles,
    long InfectedFiles, long TotalErrors, long TraversalErrors, long FilesSubmitted,
    long Batches, bool FinishedEnumeration);

/// <summary>
/// Controla pausas solo ENTRE lotes oficiales --file-list. Nunca se considera un lote
/// como completo mientras su proceso ClamAV siga en ejecución.
/// El modo de un archivo proporciona ruta actual exacta, pero recarga las firmas
/// por cada archivo y puede ser muy lento; el modo de lotes es el recomendado.
/// </summary>
internal static class ControlledScanRunner
{
    public const int BalancedBatchSize = 128;
    public const int PreciseBatchSize = 1;

    public static Task<ControlledScanResult> RunAsync(string scanner,
        IReadOnlyList<string> sharedArgs, string target, IReadOnlyCollection<string> excluded,
        int batchSize, ScanPauseGate gate, Action<ScanBatchStatus> reportBatch,
        Action<string, bool> reportLine, CancellationToken cancellationToken,
        Action<EngineProcessSample>? reportMetrics = null, ProcessPriorityClass? priority = null) =>
        RunAsync(scanner, sharedArgs, new[] { target }, excluded, batchSize, gate,
            reportBatch, reportLine, cancellationToken, reportMetrics, priority);

    public static async Task<ControlledScanResult> RunAsync(string scanner,
        IReadOnlyList<string> sharedArgs, IReadOnlyList<string> targets,
        IReadOnlyCollection<string> excluded,
        int batchSize, ScanPauseGate gate, Action<ScanBatchStatus> reportBatch,
        Action<string, bool> reportLine, CancellationToken cancellationToken,
        Action<EngineProcessSample>? reportMetrics = null, ProcessPriorityClass? priority = null)
    {
        if (batchSize is < 1 or > 2048) throw new ArgumentOutOfRangeException(nameof(batchSize));
        if (targets.Count == 0) throw new ArgumentException("Sin objetivos", nameof(targets));
        // El trabajo de enumeración y de preparación nunca debe bloquear el hilo UI WPF.
        return await Task.Run(async () =>
        {
            var errors = 0L;
            var submitted = 0L;
            var scanned = 0L;
            var infected = 0L;
            var engineErrors = 0L;
            var batches = 0L;
            var exitCode = 0;
            var finished = false;
            using var enumerator = EnumerateFiles(targets, excluded,
                error => { Interlocked.Increment(ref errors); reportLine("RECORRIDO INACCESIBLE: " + error, true); })
                .GetEnumerator();
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Buscar el lote siguiente se hace ANTES de entrar en pausa;
                    // nunca se considera analizado hasta recibir ExitCode de clamscan.
                    var files = new List<string>(batchSize);
                    while (files.Count < batchSize && enumerator.MoveNext())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var file = enumerator.Current;
                        if (file.Contains('\n') || file.Contains('\r'))
                        {
                            Interlocked.Increment(ref errors);
                            reportLine("ERROR: Ruta no compatible con lista de archivos (salto de línea)", true);
                            continue;
                        }
                        files.Add(file);
                    }
                    if (files.Count == 0) { finished = true; break; }
                    var batchNumber = checked((int)(batches + 1));
                    long? fileLength = null;
                    if (files.Count == 1)
                    {
                        try { fileLength = new FileInfo(files[0]).Length; }
                        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) { }
                    }
                    var currentBatch = new ScanBatchStatus(batchNumber, files.Count,
                        submitted, files[0], files[^1], false, fileLength);
                    reportBatch(currentBatch);
                    await gate.WaitIfPausedAsync(() => reportBatch(
                        currentBatch with { WaitingForResume = true }), cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    var listPath = Path.Combine(AppPaths.PrivateTemp,
                        "PalClam-scan-" + Guid.NewGuid().ToString("N") + ".lst");
                    try
                    {
                        File.WriteAllLines(listPath, files, new UTF8Encoding(false));
                        var args = new List<string>(sharedArgs) { "--file-list=" + listPath };
                        // Summary counters are PER BATCH; don't show the last batch as the total.
                        long batchScanned = -1, batchInfected = -1, batchErrors = -1;
                        var result = await ProcessRunner.RunAsync(scanner, args, (line, stderr) =>
                        {
                            if (ScanOutputParser.TryReadCount(line, "Scanned files", out var count))
                                Interlocked.Exchange(ref batchScanned, count);
                            else if (ScanOutputParser.TryReadCount(line, "Infected files", out count))
                                Interlocked.Exchange(ref batchInfected, count);
                            else if (ScanOutputParser.TryReadCount(line, "Total errors", out count))
                                Interlocked.Exchange(ref batchErrors, count);
                            reportLine(line, stderr);
                        }, cancellationToken, reportMetrics, priority);
                        submitted += files.Count;
                        batches++;
                        if (batchScanned >= 0) scanned += batchScanned;
                        if (batchInfected >= 0) infected += batchInfected;
                        if (batchErrors >= 0) engineErrors += batchErrors;
                        if (result.ExitCode is not (0 or 1))
                        {
                            exitCode = 2;
                            reportLine("ERROR: El motor ha devuelto un error en el lote " + batchNumber +
                                ". La cobertura de análisis es incompleta.", true);
                            break;
                        }
                        exitCode = Math.Max(exitCode, result.ExitCode);
                    }
                    finally
                    {
                        try { File.Delete(listPath); } catch (IOException) { }
                        catch (UnauthorizedAccessException) { }
                    }
                }
            }
            finally { gate.Resume(); }
            if (errors > 0 || engineErrors > 0) exitCode = 2;
            return new ControlledScanResult(exitCode, scanned, infected,
                engineErrors, errors, submitted, batches, finished);
        }, cancellationToken);
    }

    private static IEnumerable<string> EnumerateFiles(IReadOnlyList<string> targets,
        IReadOnlyCollection<string> excluded, Action<string> reportError)
    {
        // Reutilizar un solo enumerador y contador para todas las raíces de Smart Scan.
        var stack = new Stack<string>();
        foreach (var target in SmartScanPlanPolicy.CollapseTargets(targets).Reverse())
        {
            if (ScanExclusions.IsExcluded(target, excluded)) continue;
            if (File.Exists(target)) { yield return target; continue; }
            stack.Push(target);
        }
        while (stack.Count != 0)
        {
            var folder = stack.Pop();
            if (ScanExclusions.IsExcluded(folder, excluded)) continue;
            string[] entries;
            try { entries = Directory.GetFileSystemEntries(folder); }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                reportError("No se pudo enumerar " + folder + ": " + ex.Message);
                continue;
            }
            foreach (var entry in entries)
            {
                if (ScanExclusions.IsExcluded(entry, excluded)) continue;
                FileAttributes attributes;
                try { attributes = File.GetAttributes(entry); }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    reportError("No se pudo acceder a " + entry + ": " + ex.Message);
                    continue;
                }
                if ((attributes & FileAttributes.ReparsePoint) != 0) continue; // evita bucles de junctions
                if ((attributes & FileAttributes.Directory) != 0) stack.Push(entry);
                else yield return entry;
            }
        }
    }
}
