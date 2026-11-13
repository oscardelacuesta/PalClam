// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

/// <summary>Métricas operativas del proceso; NO representan eficacia de detección antivirus.</summary>
internal static class ScanPerformanceMath
{
    public static double Rate(long delta, double seconds) =>
        seconds > 0 && delta > 0 && double.IsFinite(seconds) ? delta / seconds : 0;

    public static double CpuShareOfMachine(TimeSpan cpuDelta, double elapsedSeconds, int logicalCores)
    {
        if (elapsedSeconds <= 0 || logicalCores <= 0 || cpuDelta.Ticks < 0) return 0;
        return Math.Clamp(100.0 * cpuDelta.TotalSeconds / (elapsedSeconds * logicalCores), 0, 100);
    }

    public static string QuietStatus(double seconds, bool engineRunning)
    {
        if (!engineRunning) return "Motor detenido";
        if (seconds < 0) return "Sin datos de actividad";
        if (seconds >= 90) return "Sin resultados recientes: revisar motor o archivo grande";
        if (seconds >= 20) return "Procesando o sin emitir resultados; posible archivo grande";
        return "Motor activo; último resultado recibido";
    }

    /// <summary>Tamaño lógico de fichero completado, NO lectura física de disco.
    /// Nunca abre el archivo. Se omiten rutas UNC, virtuales y desaparecidas.</summary>
    public static long TryGetLogicalFileBytes(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || path.StartsWith(@"\\", StringComparison.Ordinal) ||
                !Path.IsPathFullyQualified(path)) return 0;
            var file = new FileInfo(path);
            return file.Exists ? Math.Max(0, file.Length) : 0;
        }
        catch (Exception) { return 0; }
    }
}
