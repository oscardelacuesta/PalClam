// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Text;
using System.Text.Json;

namespace PalClam.Infrastructure;

internal sealed class ScanRecord
{
    public DateTimeOffset CompletedAt { get; set; }
    public string Target { get; set; } = "";
    public string Outcome { get; set; } = "";
    public int? ExitCode { get; set; }
    public int Detections { get; set; }
    public long Seconds { get; set; }
    public long FilesReported { get; set; }
    public long EngineScannedFiles { get; set; } = -1;
    public long EstimatedFiles { get; set; } = -1;
    public long LogicalBytesReported { get; set; }
    public long PeakEngineMemoryBytes { get; set; }
    public double AverageEngineCpuPercent { get; set; }
    public double PeakEngineCpuPercent { get; set; }
    public double LongestSilenceSeconds { get; set; }
    public bool MetricsEnabled { get; set; }
    public string ScanPriority { get; set; } = "";
    // RC7: comparabilidad requiere mismo objetivo, opciones, firmas y motor.
    public string EngineVersion { get; set; } = "";
    public string SignatureSnapshot { get; set; } = "";
    public string ScanConfigurationId { get; set; } = "";
    public string EngineMode { get; set; } = "clamscan";
    public int PerformanceSampleCount { get; set; }
    public double MeanFilesPerSecond { get; set; }
    public double MeanLogicalMiBPerSecond { get; set; }
    public int Errors { get; set; }
    public string LogPath { get; set; } = "";
    public string[] Threats { get; set; } = [];
}

/// <summary>Historial local: un JSON por línea; datos privados, sin telemetría.</summary>
internal static class ScanHistory
{
    private static readonly string Folder = Path.Combine(
        PalClam.Services.AppPaths.Root);
    private static readonly string FilePath = Path.Combine(Folder, "history.jsonl");

    public static void Append(ScanRecord record)
    {
        Directory.CreateDirectory(Folder);
        File.AppendAllText(FilePath, JsonSerializer.Serialize(record) + Environment.NewLine, Encoding.UTF8);
    }

    public static IReadOnlyList<ScanRecord> ReadRecent(int maximum = 100)
    {
        if (!File.Exists(FilePath)) return [];
        var records = new List<ScanRecord>();
        foreach (var line in File.ReadLines(FilePath).TakeLast(Math.Clamp(maximum, 1, 500)))
        {
            try
            {
                var value = JsonSerializer.Deserialize<ScanRecord>(line);
                if (value is not null) records.Add(value);
            }
            catch (JsonException) { /* Ignorar entradas incompletas */ }
        }
        records.Reverse();
        return records;
    }

    public static string RenderText()
    {
        var lines = new StringBuilder()
            .AppendLine("PALCLAM - HISTORIAL LOCAL DE ANÁLISIS")
            .AppendLine("Versión: " +
                (System.Reflection.Assembly.GetEntryAssembly()?.GetCustomAttributes(
                        typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                    .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
                    .FirstOrDefault()?.InformationalVersion ?? "no disponible"))
            .AppendLine("Generado: " + DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"))
            .AppendLine("Los resultados no sustituyen la protección de Microsoft Defender.")
            .AppendLine(new string('-', 68));
        foreach (var record in ReadRecent(250))
        {
            lines.AppendLine($"Fecha: {record.CompletedAt:yyyy-MM-dd HH:mm:ss zzz}");
            lines.AppendLine($"Ruta: {record.Target}");
            lines.AppendLine($"Resultado: {record.Outcome} | Código: {record.ExitCode?.ToString() ?? "sin resultado"} | Detecciones: {record.Detections} | Duración: {record.Seconds}s");
            lines.AppendLine($"Resultados por archivo: {record.FilesReported}, analizados según resumen ClamAV: {record.EngineScannedFiles}, errores: {record.Errors}");
            if (record.MetricsEnabled)
                lines.AppendLine($"Rendimiento: prioridad={record.ScanPriority}, CPU media del motor={record.AverageEngineCpuPercent:0.0}% del equipo, " +
                    $"CPU pico={record.PeakEngineCpuPercent:0.0}%, RAM pico={record.PeakEngineMemoryBytes / 1048576.0:0} MiB, " +
                    $"volumen lógico reportado={record.LogicalBytesReported / 1048576.0:0.0} MiB, " +
                    $"silencio máximo entre resultados={record.LongestSilenceSeconds:0}s");
            if (record.PerformanceSampleCount > 0)
                lines.AppendLine($"Muestras de rendimiento: {record.PerformanceSampleCount}, motor: {record.EngineMode}, " +
                    $"archivos/s (promedio): {record.MeanFilesPerSecond:0.0}, MiB lógicos/s: {record.MeanLogicalMiBPerSecond:0.0}");
            if (!string.IsNullOrWhiteSpace(record.LogPath)) lines.AppendLine("Informe detallado: " + record.LogPath);
            foreach (var threat in record.Threats) lines.AppendLine("Detección: " + threat);
            lines.AppendLine();
        }
        return lines.ToString();
    }
}
