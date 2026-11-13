// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Text.Json;

namespace PalClam.Services;

/// <summary>Registro local de incidencias de Guard; nunca se sube a ningún servidor.</summary>
internal static class GuardAuditLog
{
    private static readonly object Gate = new();
    private static readonly string Folder = Path.Combine(
        PalClam.Services.AppPaths.Root, "Guard");
    public static string FilePath => Path.Combine(Folder, "incidencias.jsonl");

    public static void Append(GuardFinding item)
    {
        if (item.Outcome == GuardOutcome.Clean) return;
        lock (Gate)
        {
            Directory.CreateDirectory(Folder);
            var path = FilePath;
            if (File.Exists(path) && new FileInfo(path).Length > 4_000_000)
                File.Move(path, Path.Combine(Folder, "incidencias-anterior.jsonl"), true);
            File.AppendAllText(path, JsonSerializer.Serialize(item) + Environment.NewLine);
        }
    }
}
