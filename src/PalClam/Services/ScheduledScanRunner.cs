// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using PalClam.Infrastructure;

namespace PalClam.Services;

/// <summary>Escaneo desatendido en sesión de usuario; sin interfaz ni borrado automático.</summary>
internal static class ScheduledScanRunner
{
    public static async Task<int> RunAsync()
    {
        if (AppPaths.IsPortable) return 2;
        var settings = SettingsStore.Load();
        var target = settings.ScheduledTarget == "Perfil de usuario"
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var scanner = ClamAvLocator.FindScanner();
        var outcome = "Incompleto / errores";
        var detections = 0;
        var errors = 0;
        long processed = 0;
        long engineInfected = -1;
        long engineScanned = -1;
        int? code = null;
        var threats = new List<string>();
        var began = DateTimeOffset.Now;
        ScanLogWriter? log = null;
        try
        {
            log = new ScanLogWriter("[PROGRAMADO] " + target);
            if (!ClamAvLocator.IsValidScanner(scanner) || !SignatureStore.HasSignatures() || !Directory.Exists(target))
                throw new InvalidOperationException("No hay motor, firmas o carpeta de destino disponibles.");
            if (ScanExclusions.IsExcluded(target, settings.ExcludedPaths))
                throw new InvalidOperationException("El destino completo figura en las exclusiones. Análisis omitido.");
            var arguments = new List<string> { "--stdout", "--verbose", "--database=" + SignatureStore.DatabaseDirectory,
                "--scan-archive=" + (settings.ScanArchives ? "yes" : "no") };
            if (settings.DetectPua) arguments.Add("--detect-pua=yes");
            arguments.AddRange(ScanExclusions.BuildClamArguments(ScanExclusions.Normalize(settings.ExcludedPaths)));
            arguments.Add("--recursive"); arguments.Add(target);
            var result = await ProcessRunner.RunAsync(scanner, arguments, (line, error) =>
            {
                try { log.WriteLine(line, error); } catch { }
                var file = ScanOutputParser.ParseFile(line);
                if (file is not null)
                {
                    Interlocked.Increment(ref processed);
                    if (file.Status == ScanFileStatus.Detected)
                    {
                        Interlocked.Increment(ref detections);
                        lock (threats) { if (threats.Count < 250) threats.Add(file.FilePath + ": " + file.Detail); }
                    }
                    else if (file.Status == ScanFileStatus.Error) Interlocked.Increment(ref errors);
                }
                else if (ScanOutputParser.TryReadCount(line, "Infected files", out var reportedInfected))
                    Interlocked.Exchange(ref engineInfected, reportedInfected);
                else if (ScanOutputParser.TryReadCount(line, "Scanned files", out var reportedScanned))
                    Interlocked.Exchange(ref engineScanned, reportedScanned);
                else if (ScanOutputParser.TryReadCount(line, "Total errors", out var totalErrors))
                {
                    int old;
                    do { old = Volatile.Read(ref errors); }
                    while (totalErrors > old && Interlocked.CompareExchange(ref errors,
                        (int)Math.Min(totalErrors, int.MaxValue), old) != old);
                }
                else if (error || line.Contains("ERROR:", StringComparison.OrdinalIgnoreCase))
                    Interlocked.Increment(ref errors);
            }, CancellationToken.None);
            code = result.ExitCode;
            var verdict = ScanVerdictPolicy.Evaluate(code, detections, errors, engineInfected);
            detections = (int)Math.Min(verdict.Detections, int.MaxValue);
            outcome = verdict.Status switch
            {
                ScanVerdictStatus.Clean => "Sin detecciones",
                ScanVerdictStatus.Detected => "Detecciones",
                ScanVerdictStatus.DetectedWithIssues => "Detecciones e incidencias",
                _ => "Incompleto / errores"
            };
        }
        catch (Exception ex)
        {
            try { log?.WriteLine("[ERROR] " + ex.Message); } catch { }
        }
        finally
        {
            try { log?.Dispose(); } catch { }
            try
            {
                ScanHistory.Append(new ScanRecord
                {
                    CompletedAt = DateTimeOffset.Now, Target = "[PROGRAMADO] " + target,
                    Outcome = outcome, ExitCode = code, Detections = detections,
                    Errors = errors, FilesReported = processed, EngineScannedFiles = engineScanned,
                    Seconds = (long)(DateTimeOffset.Now - began).TotalSeconds,
                    LogPath = log?.FilePath ?? "", Threats = threats.ToArray()
                });
            }
            catch { }
        }
        return ScanVerdictPolicy.Evaluate(code, detections, errors, engineInfected).IsComplete ? 0 : 2;
    }
}
