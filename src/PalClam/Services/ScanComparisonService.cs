// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Security.Cryptography;
using System.Text;
using PalClam.Infrastructure;

namespace PalClam.Services;

internal enum Comparability { Comparable, DifferentTarget, DifferentConfiguration, DifferentEngine,
    DifferentSignatures, DifferentResults, MissingMetadata, Incomplete, InsufficientData }

internal sealed record ComparisonResult(Comparability Status, double? SecondsDeltaPercent,
    double? FilesRateDeltaPercent, string Explanation);

/// <summary>Compara exclusivamente ejecuciones completadas con la misma configuración y base de firmas.
/// La comparación es de rendimiento operativo, NO de tasa de detección de malware.</summary>
internal static class ScanComparisonService
{
    public static string ConfigurationId(bool archives, bool pua, string priority,
        IEnumerable<string> excludedPaths)
    {
        var ordered = excludedPaths.Select(x => x.Trim().TrimEnd(Path.DirectorySeparatorChar))
            .Where(x => x.Length > 0).OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
        var data = $"archive={archives};pua={pua};priority={priority};exclusions=" +
            string.Join("|", ordered.Select(x => x.ToUpperInvariant()));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data)));
    }

    /// <summary>Identificación ligera por metadatos locales, NO un hash criptográfico de las firmas.
    /// Si alguna base cambia, la identidad cambia. Nunca lee todos los gigabytes de firmas.</summary>
    public static string SignatureSnapshot(string databaseDirectory)
    {
        try
        {
            if (!Directory.Exists(databaseDirectory)) return "";
            var files = Directory.EnumerateFiles(databaseDirectory)
                .Where(x => new[] { ".cvd", ".cld", ".cud", ".hdb", ".ndb", ".ldb" }
                    .Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase))
                .Select(x => new FileInfo(x))
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
            if (files.Length == 0) return "";
            var detail = string.Join("|", files.Select(x =>
                $"{x.Name}:{x.Length}:{x.LastWriteTimeUtc.Ticks}"));
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(detail)));
        }
        catch (Exception) { return ""; }
    }

    public static ComparisonResult Compare(ScanRecord previous, ScanRecord current)
    {
        if (previous.Outcome != "Sin detecciones" && previous.Outcome != "Detecciones" ||
            current.Outcome != "Sin detecciones" && current.Outcome != "Detecciones" ||
            previous.Errors != 0 || current.Errors != 0 ||
            previous.ExitCode is not (0 or 1) || current.ExitCode is not (0 or 1))
            return new(Comparability.Incomplete, null, null,
                "Una ejecución tiene errores, cancelación o resultado no concluyente.");
        if (previous.Seconds <= 0 || current.Seconds <= 0 ||
            previous.EngineScannedFiles <= 0 || current.EngineScannedFiles <= 0)
            return new(Comparability.InsufficientData, null, null,
                "No hay duración o recuento oficial de archivos suficiente.");
        if (string.IsNullOrWhiteSpace(previous.EngineVersion) ||
            string.IsNullOrWhiteSpace(current.EngineVersion) ||
            string.IsNullOrWhiteSpace(previous.SignatureSnapshot) ||
            string.IsNullOrWhiteSpace(current.SignatureSnapshot) ||
            string.IsNullOrWhiteSpace(previous.ScanConfigurationId) ||
            string.IsNullOrWhiteSpace(current.ScanConfigurationId))
            return new(Comparability.MissingMetadata, null, null,
                "Una ejecución es antigua o no dispone de metadatos comparables.");
        if (!string.Equals(previous.Target, current.Target, StringComparison.OrdinalIgnoreCase))
            return new(Comparability.DifferentTarget, null, null, "Los objetivos no coinciden.");
        if (!string.Equals(previous.ScanConfigurationId, current.ScanConfigurationId,
            StringComparison.Ordinal))
            return new(Comparability.DifferentConfiguration, null, null,
                "Las exclusiones, la PUA, el análisis de archivos comprimidos o la prioridad cambiaron.");
        if (previous.EngineMode != current.EngineMode || previous.EngineVersion != current.EngineVersion)
            return new(Comparability.DifferentEngine, null, null, "El motor o versión son distintos.");
        if (previous.SignatureSnapshot != current.SignatureSnapshot)
            return new(Comparability.DifferentSignatures, null, null, "La base de firmas cambió.");
        if (previous.EngineScannedFiles != current.EngineScannedFiles)
            return new(Comparability.InsufficientData, null, null,
                "El número de archivos examinados por el motor no coincide.");
        if (previous.Detections != current.Detections ||
            !string.Equals(previous.Outcome, current.Outcome, StringComparison.Ordinal))
            return new(Comparability.DifferentResults, null, null,
                "El resultado de detección cambió. No se calcula una mejora de velocidad equivalente.");
        // Negativo = reducción de tiempo, positiva = aumento de velocidad.
        var timeDelta = 100.0 * (current.Seconds - previous.Seconds) / previous.Seconds;
        var before = (double)previous.EngineScannedFiles / previous.Seconds;
        var after = (double)current.EngineScannedFiles / current.Seconds;
        var speedDelta = 100.0 * (after - before) / before;
        return new(Comparability.Comparable, timeDelta, speedDelta,
            "Comparación orientativa: mismo objetivo, configuración, motor, firmas y número de archivos. " +
            "No garantiza idéntica carga del equipo, caché del disco ni contenido de archivos.");
    }
}
