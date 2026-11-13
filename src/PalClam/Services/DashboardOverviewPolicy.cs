// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System;
using System.Collections.Generic;
using System.Linq;
using PalClam.Infrastructure;

namespace PalClam.Services;

// Datos del panel, no un «porcentaje de seguridad» ni una prueba de protección preventiva.
internal sealed record DashboardOverview(
    IReadOnlyList<ScanRecord> RecentScans,
    IReadOnlyList<SecurityNotice> RecentNotices,
    int ScanCount, long DetectionsInScans, int IncompleteScans,
    int NoticeThreats, int NoticeWarnings, int NoticeErrors, int NoticeInformation,
    int UnreviewedNotices, ScanRecord? LastScanWithMetrics,
    bool HistoryTruncated, bool HistoryUnreadable, int HistoryInvalidEntries,
    bool NoticesLimited, int NoticeFilesUnreadable, int NoticeEntriesInvalid)
{
    public bool HasPartialData => HistoryTruncated || HistoryUnreadable || HistoryInvalidEntries > 0 || NoticesLimited ||
        NoticeFilesUnreadable > 0 || NoticeEntriesInvalid > 0;
}

/// <summary>Política calculada sobre registros existentes, independiente de WPF y sin efectos.</summary>
internal static class DashboardOverviewPolicy
{
    internal static DashboardOverview Build(IEnumerable<ScanRecord> scanHistory,
        SecurityNoticeFeedResult noticeFeed, DateTimeOffset now,
        DateTimeOffset reviewedUntil, bool historyTruncated = false,
        bool historyUnreadable = false, int historyInvalidEntries = 0)
    {
        var scans = scanHistory
            .Where(x => x.CompletedAt >= now.AddYears(-10) && x.CompletedAt <= now.AddMinutes(5))
            .OrderByDescending(x => x.CompletedAt).Take(30).ToArray();
        var notices = noticeFeed.Items
            .Where(x => x.When >= now.AddYears(-10) && x.When <= now.AddMinutes(5))
            .OrderByDescending(x => x.When).Take(80).ToArray();
        return new DashboardOverview(
            scans, notices.Take(6).ToArray(), scans.Length,
            scans.Sum(x => (long)Math.Max(0, x.Detections)),
            scans.Count(x => x.Errors > 0 || x.ExitCode is null || x.ExitCode > 1 ||
                (x.Outcome?.Contains("Cancel", StringComparison.OrdinalIgnoreCase) == true) ||
                (x.Outcome?.Contains("Incomplet", StringComparison.OrdinalIgnoreCase) == true)),
            notices.Count(x => x.Level == SecurityNoticeLevel.Threat),
            notices.Count(x => x.Level == SecurityNoticeLevel.Warning),
            notices.Count(x => x.Level == SecurityNoticeLevel.Error),
            notices.Count(x => x.Level == SecurityNoticeLevel.Information),
            notices.Count(x => x.When > reviewedUntil),
            scans.FirstOrDefault(x => x.MetricsEnabled && x.PerformanceSampleCount > 0),
            historyTruncated, historyUnreadable, historyInvalidEntries, noticeFeed.Limited,
            noticeFeed.UnreadableFiles, noticeFeed.InvalidEntries);
    }
}
