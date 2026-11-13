// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

// La ausencia de detecciones en una ejecución NO demuestra que el archivo sea seguro.
internal enum QuarantineRecheckVerdict { NoDetections, Detected, Inconclusive }

internal sealed record QuarantineRecheckResult(QuarantineRecheckVerdict Verdict,
    string Detail, DateTimeOffset At, int EngineExitCode)
{
    public string DisplayVerdict => Verdict switch
    {
        QuarantineRecheckVerdict.NoDetections => "Sin detecciones (no garantiza seguridad)",
        QuarantineRecheckVerdict.Detected => "DETECCIÓN confirmada",
        _ => "No concluyente"
    };
}

internal static class QuarantineReviewPolicy
{
    // Nunca inferir resultado limpio si el motor no terminó con código 0.
    public static QuarantineRecheckVerdict Classify(int exitCode, bool detected, bool errorReported) =>
        exitCode is < 0 or > 2 || exitCode == 2 || errorReported || (exitCode == 1 && !detected)
            ? QuarantineRecheckVerdict.Inconclusive
            : detected || exitCode == 1
                ? QuarantineRecheckVerdict.Detected
                : QuarantineRecheckVerdict.NoDetections;

    public static bool CanRecheck(QuarantineEntry? entry) =>
        entry is not null && Guid.TryParseExact(entry.Id, "N", out _) &&
        entry.RestoredAt is null && entry.DeletedAt is null && !entry.IntegrityWarning;
}
