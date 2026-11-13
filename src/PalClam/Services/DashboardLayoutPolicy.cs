// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

internal enum DashboardWidthMode { Compact, Medium, Wide }

/// <summary>
/// Three stable dashboard arrangements, independently testable without starting WPF.
/// The breakpoint is divided by text scale so accessible typography never overlaps cards.
/// </summary>
internal static class DashboardLayoutPolicy
{
    internal static DashboardWidthMode ForWidth(double width, double textScale)
    {
        if (!double.IsFinite(width) || width <= 0) return DashboardWidthMode.Compact;
        if (!double.IsFinite(textScale) || textScale < 0.8 || textScale > 1.5)
            textScale = 1.0;
        var effective = width / textScale;
        if (effective < 640) return DashboardWidthMode.Compact;
        if (effective < 740) return DashboardWidthMode.Medium;
        return DashboardWidthMode.Wide;
    }
}
