// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System;

namespace PalClam.Services;

/// <summary>
/// Compara dos latidos consecutivos del servicio sin confundir contadores
/// históricos o reinicios con detecciones nuevas.
/// </summary>
internal readonly record struct MachineGuardNoticeChanges(
    bool NewDetections, bool NewCoverageGaps, bool NewErrors);

internal static class MachineGuardNoticePolicy
{
    public static MachineGuardNoticeChanges Compare(
        MachineServiceSnapshot? previous, MachineServiceSnapshot current,
        DateTimeOffset now)
    {
        if (previous is null || !previous.IsFresh(now) || !current.IsFresh(now) ||
            current.Utc <= previous.Utc ||
            current.Scanned < previous.Scanned ||
            current.Detections < previous.Detections ||
            current.Skipped < previous.Skipped ||
            current.Errors < previous.Errors ||
            current.LostEvents < previous.LostEvents)
            return default;  // Primera lectura, estado antiguo o reinicio.

        return new MachineGuardNoticeChanges(
            current.Detections > previous.Detections,
            current.LostEvents > previous.LostEvents || current.Skipped > previous.Skipped,
            current.Errors > previous.Errors);
    }
}
