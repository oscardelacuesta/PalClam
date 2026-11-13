// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

/// <summary>
/// Reanaliza el fichero de nombre opaco en la cuarentena, sin sacarlo, ejecutarlo
/// ni restaurarlo. Las bases han de existir en el perfil local de la cuenta.
/// </summary>
internal static class QuarantineRescanService
{
    public static async Task<QuarantineRecheckResult> RecheckAsync(QuarantineEntry entry,
        CancellationToken cancellationToken)
    {
        if (!SignatureStore.HasSignatures())
            throw new IOException("No hay bases main y daily locales; actualiza las firmas antes de reanalizar.");
        var scanner = ClamAvLocator.FindScanner();
        if (!ClamAvLocator.IsValidScanner(scanner))
            throw new IOException("No se encuentra clamscan.exe para reanalizar la cuarentena.");

        var isolated = await QuarantineStore.VerifyIsolatedAsync(entry, cancellationToken);
        var events = new List<ScanFileEvent>();
        var issues = new List<string>();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        try
        {
            var result = await ProcessRunner.RunAsync(scanner,
                new[] { "--no-summary", "--stdout", "--database=" + SignatureStore.DatabaseDirectory, isolated },
                (line, isError) =>
                {
                    // Los callbacks proceden de dos hilos de lectura distintos.
                    lock (events)
                    {
                        var item = ScanOutputParser.ParseFile(line);
                        if (item is not null && string.Equals(item.FilePath, isolated,
                            StringComparison.OrdinalIgnoreCase)) events.Add(item);
                        else if (isError || line.Contains(" ERROR", StringComparison.OrdinalIgnoreCase))
                            issues.Add(line.Length > 350 ? line[..350] : line);
                    }
                }, timeout.Token);
            cancellationToken.ThrowIfCancellationRequested();
            var detected = events.Any(x => x.Status == ScanFileStatus.Detected);
            var errorReported = issues.Count != 0 || events.Any(x => x.Status == ScanFileStatus.Error);
            var verdict = QuarantineReviewPolicy.Classify(result.ExitCode, detected, errorReported);
            var detail = verdict switch
            {
                QuarantineRecheckVerdict.Detected => events.FirstOrDefault(x => x.Status == ScanFileStatus.Detected)?.Detail
                    ?? "ClamAV indica detección, sin nombre de firma detallado.",
                QuarantineRecheckVerdict.NoDetections => "ClamAV finalizó sin detecciones; no se garantiza que sea seguro.",
                _ => "Análisis incompleto o errores de lectura: " + string.Join(" | ", issues.Take(2))
            };
            var reviewed = new QuarantineRecheckResult(verdict, detail, DateTimeOffset.Now, result.ExitCode);
            // La comprobación del hash se repite DESPUÉS del escaneo antes de registrar resultados.
            await QuarantineStore.SaveRecheckAsync(entry, reviewed, cancellationToken);
            RemediationAuditLog.Append("Reanalizar", entry.Id, reviewed.DisplayVerdict, reviewed.Detail);
            return reviewed;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("El nuevo análisis tardó más de cinco minutos. Sin veredicto.");
        }
    }
}
