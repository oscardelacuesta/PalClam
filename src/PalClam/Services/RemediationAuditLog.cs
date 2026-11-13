// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Text.Json;

namespace PalClam.Services;

internal sealed record RemediationAuditRecord(DateTimeOffset When, string Actor,
    string Action, string EntryId, string Outcome, string Detail);

/// <summary>
/// Registro local complementario (no es inmutable ni resistente a un administrador malicioso).
/// Nunca abre ni ejecuta archivos de cuarentena.
/// </summary>
internal static class RemediationAuditLog
{
#if PALCLAM_SMOKETEST
    private static readonly string Folder = Path.Combine(Path.GetTempPath(),
        "PalClam-SmokeTestRemediation-" + Guid.NewGuid().ToString("N"));
    internal static void CleanupTestRoot()
    {
        if (Directory.Exists(Folder)) Directory.Delete(Folder, recursive: true);
    }
#else
    private static readonly string Folder = Path.Combine(
        PalClam.Services.AppPaths.Root, "Remediation");
#endif
    private static readonly object Gate = new();
    public static string LogPath => Path.Combine(Folder, "acciones.jsonl");

    public static void Append(string action, string entryId, string outcome, string detail)
    {
        var item = new RemediationAuditRecord(DateTimeOffset.Now, Environment.UserName, action, entryId, outcome,
            detail.Length > 1200 ? detail[..1200] : detail);
        lock (Gate)
        {
            Directory.CreateDirectory(Folder);
            if (File.Exists(LogPath) && new FileInfo(LogPath).Length > 3_000_000)
                File.Move(LogPath, Path.Combine(Folder, "acciones-anterior.jsonl"), overwrite: true);
            File.AppendAllText(LogPath, JsonSerializer.Serialize(item) + Environment.NewLine);
        }
    }

    public static IReadOnlyList<RemediationAuditRecord> ReadRecent(int maximum = 250)
    {
        maximum = Math.Clamp(maximum, 1, 500);
        var output = new List<RemediationAuditRecord>();
        if (!File.Exists(LogPath)) return output;
        // Presupuesto fijo para no leer archivos enormes o manipulados.
        if (new FileInfo(LogPath).Length > 3_500_000) return output;
        foreach (var line in File.ReadLines(LogPath))
        {
            if (line.Length is < 2 or > 5000) continue;
            try
            {
                var item = JsonSerializer.Deserialize<RemediationAuditRecord>(line);
                if (item is not null && item.EntryId is { Length: <= 64 } && item.Detail is { Length: <= 1200 })
                    output.Add(item);
            }
            catch (JsonException) { }
        }
        return output.OrderByDescending(x => x.When).Take(maximum).ToArray();
    }
}
