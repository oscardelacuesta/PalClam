// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

internal sealed record PerformancePoint(double Seconds, double FilesPerSecond,
    double CpuPercent, double WorkingSetMiB);

/// <summary>Serie ligera en RAM: no registra rutas ni contenido, máximo 120 muestras.</summary>
internal sealed class ScanPerformanceSeries
{
    private readonly List<PerformancePoint> _samples = [];
    public IReadOnlyList<PerformancePoint> Samples => _samples;
    public void Add(PerformancePoint point)
    {
        if (!double.IsFinite(point.Seconds) || point.Seconds < 0) return;
        _samples.Add(new(point.Seconds, Safe(point.FilesPerSecond),
            Math.Clamp(Safe(point.CpuPercent), 0, 100), Safe(point.WorkingSetMiB)));
        if (_samples.Count > 120) _samples.RemoveAt(0);
    }
    public void Clear() => _samples.Clear();
    private static double Safe(double value) => double.IsFinite(value) ? Math.Max(0, value) : 0;

    /// <summary>Escala reproducible 0..1; no usa unidades normalizadas como valores reales.</summary>
    public static double[] Normalize(IEnumerable<double> values)
    {
        var data = values.Select(Safe).ToArray();
        var maximum = Math.Max(0.001, data.Length == 0 ? 0 : data.Max());
        return data.Select(x => x / maximum).ToArray();
    }
}
