// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Globalization;

namespace PalClam.Services;

internal sealed record SignatureDatabaseInfo(string Name, string Version, long? Signatures, DateTime LastModifiedUtc);

internal static class SignatureInfoService
{
    public static async Task<IReadOnlyList<SignatureDatabaseInfo>> ReadAsync(string scanner, CancellationToken token)
    {
        var sigtool = Path.Combine(Path.GetDirectoryName(scanner) ?? "", "sigtool.exe");
        return await ReadFolderAsync(SignatureStore.DatabaseDirectory, sigtool, token);
    }

    public static async Task<IReadOnlyList<SignatureDatabaseInfo>> ReadFolderAsync(
        string folder, string sigtool, CancellationToken token)
    {
        var items = new List<SignatureDatabaseInfo>();
        foreach (var family in new[] { "main", "daily", "bytecode" })
        {
            token.ThrowIfCancellationRequested();
            var file = new[] { Path.Combine(folder, family + ".cld"), Path.Combine(folder, family + ".cvd") }
                .FirstOrDefault(File.Exists);
            if (file is null) continue;
            var date = File.GetLastWriteTimeUtc(file);
            var version = "No disponible";
            long? signatures = null;
            if (File.Exists(sigtool))
            {
                try
                {
                    var lines = new List<string>();
                    var result = await ProcessRunner.RunAsync(sigtool, new[] { "--info", file },
                        (line, error) => { lock (lines) lines.Add(line); }, token);
                    if (result.ExitCode == 0)
                    {
                        foreach (var line in lines)
                        {
                            var part = line.Trim();
                            if (part.StartsWith("Version:", StringComparison.OrdinalIgnoreCase))
                                version = part[8..].Trim();
                            if (part.StartsWith("Signatures:", StringComparison.OrdinalIgnoreCase) &&
                                long.TryParse(part[11..].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                                signatures = n;
                        }
                    }
                }
                catch (Exception) when (!token.IsCancellationRequested) { /* Mostrar metadatos disponibles */ }
            }
            items.Add(new(family, version, signatures, date));
        }
        return items;
    }
}
