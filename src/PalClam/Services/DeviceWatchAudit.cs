// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System;
using System.Text.Json;

namespace PalClam.Services;

internal sealed record DeviceWatchRecord(DateTimeOffset When, string Level,
    string Root, string Detail);

/// <summary>Incidencias descriptivas; una conexión USB no se marca como malware.</summary>
internal static class DeviceWatchAudit
{
    private static readonly object Gate = new();
    public static string Folder => Path.Combine(
        PalClam.Services.AppPaths.Root, "DeviceWatch");

    public static void Append(DeviceWatchRecord record)
    {
        lock (Gate)
        {
            Directory.CreateDirectory(Folder);
            var path = Path.Combine(Folder, "eventos.jsonl");
            if (File.Exists(path) && new FileInfo(path).Length > 1_000_000)
                File.Move(path, Path.Combine(Folder, "eventos-anterior.jsonl"), overwrite: true);
            File.AppendAllText(path, JsonSerializer.Serialize(record) + Environment.NewLine);
        }
    }
}
