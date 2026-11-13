// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Text.Json;

namespace PalClam.Services;

internal static class RansomwareAudit
{
    private static readonly object Sync = new();
    internal static string Folder => Path.Combine(PalClam.Services.AppPaths.Root, "RansomwareWatch");
    internal static void Alert(RansomwareAlert alert) => Write(new { alert.When, alert.Kind, Detail = alert.Detail, alert.ExamplePath });
    internal static void Health(string detail) => Write(new { When = DateTimeOffset.UtcNow, Kind = "health", Detail = detail, ExamplePath = "" });
    private static void Write(object entry)
    {
        lock (Sync)
        {
            Directory.CreateDirectory(Folder);
            var path = Path.Combine(Folder, "eventos.jsonl");
            if (File.Exists(path) && new FileInfo(path).Length >= 1_048_576)
                File.Move(path, Path.Combine(Folder, "eventos-anterior.jsonl"), true);
            File.AppendAllText(path, JsonSerializer.Serialize(entry) + Environment.NewLine);
        }
    }
}
