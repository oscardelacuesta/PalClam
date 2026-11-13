// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

/// <summary>
/// PalClam Guard 2.0: policy-only presets, safe to exercise in the
/// cross-platform SmokeTests project. Does not turn Guard into an on-access AV.
/// </summary>
internal sealed record GuardTuning(
    string Mode, int QueueCapacity, int QuietPeriodMs,
    int StabilityPollMs, int MinimumScanGapMs)
{
    public const string DefaultMode = "Equilibrado";

    public static string NormalizeMode(string? mode) => mode switch
    {
        "Eficiente" => "Eficiente",
        "Rápido" => "Rápido",
        _ => DefaultMode
    };

    public static GuardTuning ForMode(string? mode) => NormalizeMode(mode) switch
    {
        "Eficiente" => new("Eficiente", 256, 900, 500, 400),
        "Rápido" => new("Rápido", 1024, 200, 250, 0),
        _ => new("Equilibrado", 512, 450, 350, 125)
    };

    /// <summary>Time to wait after the last file-system notification.
    /// Clocks may be adjusted; never ask Task.Delay for a negative duration.</summary>
    public int QuietRemainingMs(long nowUtcTicks, long lastEventUtcTicks)
    {
        if (lastEventUtcTicks <= 0) return QuietPeriodMs;
        var elapsed = (nowUtcTicks - lastEventUtcTicks) / TimeSpan.TicksPerMillisecond;
        return (int)Math.Clamp((long)QuietPeriodMs - elapsed, 0L, (long)QuietPeriodMs);
    }
}
