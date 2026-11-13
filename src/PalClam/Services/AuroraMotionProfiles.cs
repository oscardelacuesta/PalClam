// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

/// <summary>
/// Escalas de tiempo de la aurora. No afectan al análisis ni al motor ClamAV.
/// Valores limitados para evitar animaciones excesivas y estados desconocidos.
/// </summary>
internal sealed record AuroraMotionProfile(string Id, string Name, double SpeedFactor);

internal static class AuroraMotionProfiles
{
    internal const string DefaultId = "Equilibrada";
    internal static readonly IReadOnlyList<AuroraMotionProfile> All = new[]
    {
        new AuroraMotionProfile("Suave", "Suave · movimiento pausado", 1.00),
        new AuroraMotionProfile("Equilibrada", "Equilibrada · recomendada", 1.65),
        new AuroraMotionProfile("Dinamica", "Dinámica · movimiento visible", 2.30)
    };

    internal static AuroraMotionProfile Find(string? id) =>
        All.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? All[1];

    internal static double DurationSeconds(double originalSeconds, string? id) =>
        Math.Clamp(originalSeconds / Find(id).SpeedFactor, 4.0, 40.0);
}
