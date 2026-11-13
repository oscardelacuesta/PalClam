// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using PalClam.Infrastructure;

namespace PalClam.Services;

internal sealed record DashboardReadResult(DashboardOverview Overview,
    MachineServiceSnapshot? Machine, bool? MachineInstalled);

/// <summary>
/// E/S limitada, de solo lectura. Se invoca desde Task.Run, no desde el Dispatcher.
/// No modifica los ficheros de seguridad, firmas ni cuarentena.
/// </summary>
internal static class DashboardOverviewReader
{
    private const long MaxHistoryBytes = 1_000_000;
    private const int MaxHistoryLine = 16_384;

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal static DashboardReadResult Read(string historyPath, string guardFolder,
        string machineFolder, string reviewMarkerPath, DateTimeOffset now)
    {
        var history = ReadHistoryTail(historyPath, out var truncated, out var unreadable,
            out var invalidHistoryEntries);
        SecurityNoticeFeedResult notices;
        try
        {
            notices = SecurityNoticeFeed.ReadRecent(guardFolder, machineFolder, now, 80);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            System.Security.SecurityException or ArgumentException or NotSupportedException)
        {
            notices = new SecurityNoticeFeedResult(Array.Empty<SecurityNotice>(), 1, 0, false);
        }
        var marker = SecurityNoticeReadMarker.Read(reviewMarkerPath);
        MachineServiceSnapshot? machine = MachineServiceSnapshot.TryRead(
            Path.Combine(machineFolder, "estado.json"));
        bool? installed;
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Services\PalClamGuardSvc");
            installed = key is not null;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or
            System.Security.SecurityException or IOException)
        {
            installed = null;
        }
        return new DashboardReadResult(
            DashboardOverviewPolicy.Build(history, notices, now, marker, truncated, unreadable,
                invalidHistoryEntries),
            machine, installed);
    }

    internal static IReadOnlyList<ScanRecord> ReadHistoryTail(string file,
        out bool truncated, out bool unreadable) =>
        ReadHistoryTail(file, out truncated, out unreadable, out _);

    internal static IReadOnlyList<ScanRecord> ReadHistoryTail(string file,
        out bool truncated, out bool unreadable, out int invalidEntries)
    {
        invalidEntries = 0;
        truncated = false;
        unreadable = false;
        if (!File.Exists(file)) return Array.Empty<ScanRecord>();
        var items = new List<ScanRecord>();
        try
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            truncated = stream.Length > MaxHistoryBytes;
            if (truncated) stream.Seek(-MaxHistoryBytes, SeekOrigin.End);
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, leaveOpen: false);
            if (truncated) reader.ReadLine(); // Descartar la línea cortada.
            while (reader.ReadLine() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Length > MaxHistoryLine) { invalidEntries++; continue; }
                try
                {
                    var record = JsonSerializer.Deserialize<ScanRecord>(line);
                    if (record is not null) items.Add(record);
                    else invalidEntries++;
                }
                catch (JsonException) { invalidEntries++; /* No convertir registros dañados en resultados válidos. */ }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            System.Security.SecurityException or ArgumentException or NotSupportedException)
        {
            unreadable = true;
            return Array.Empty<ScanRecord>();
        }
        return items.TakeLast(80).ToArray();
    }
}
