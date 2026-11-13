// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

/// <summary>
/// Política común de interpretación del código de salida de ClamAV.
/// 0 = sin detecciones notificadas, 1 = se ha detectado malware, 2+ = error.
/// Una amenaza sin detalle por archivo no se pierde: se contabiliza como mínimo una.
/// Errores u omisiones informados impiden declarar el resultado íntegramente limpio.
/// </summary>
internal enum ScanVerdictStatus { Clean, Detected, Inconclusive, DetectedWithIssues }

internal sealed record ScanVerdict(ScanVerdictStatus Status, long Detections)
{
    public bool HasThreats => Status is ScanVerdictStatus.Detected or ScanVerdictStatus.DetectedWithIssues;
    public bool IsComplete => Status is ScanVerdictStatus.Clean or ScanVerdictStatus.Detected;
}

internal static class ScanVerdictPolicy
{
    public static ScanVerdict Evaluate(int? exitCode, long parsedDetections, long errors, long engineInfectedFiles)
    {
        var detectedCount = Math.Max(0, Math.Max(parsedDetections, engineInfectedFiles));
        // ClamAV exit 1 es indicación positiva, incluso si no se pudo extraer
        // la línea FOUND o el resumen del motor.
        if (exitCode == 1 && detectedCount == 0) detectedCount = 1;
        var hasThreats = detectedCount > 0;
        var validExit = exitCode is 0 or 1;
        var hasErrors = errors > 0 || !validExit;
        if (hasErrors)
            return new(hasThreats ? ScanVerdictStatus.DetectedWithIssues : ScanVerdictStatus.Inconclusive,
                detectedCount);
        return new(hasThreats ? ScanVerdictStatus.Detected : ScanVerdictStatus.Clean,
            detectedCount);
    }
}
