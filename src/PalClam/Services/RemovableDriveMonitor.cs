// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System;
using System.Collections.Generic;
using System.Linq;

namespace PalClam.Services;

internal sealed record RemovableVolume(string Root, string Label);

/// <summary>
/// Compara instantáneas del SO: no depende de WMI y no trata discos fijos USB como
/// extraíbles. Las unidades preexistentes forman la línea base, sin autoanalizarlas.
/// </summary>
internal sealed class RemovableDriveMonitor
{
    private HashSet<string> _previous = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;

    public void Reset(IEnumerable<RemovableVolume> snapshot)
    {
        _previous = Valid(snapshot).Select(x => x.Root).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _initialized = true;
    }

    public IReadOnlyList<RemovableVolume> Observe(IEnumerable<RemovableVolume> snapshot)
    {
        var drives = Valid(snapshot).ToArray();
        if (!_initialized) { Reset(drives); return Array.Empty<RemovableVolume>(); }
        var inserted = drives.Where(x => !_previous.Contains(x.Root)).ToArray();
        _previous = drives.Select(x => x.Root).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return inserted;
    }

    private static IEnumerable<RemovableVolume> Valid(IEnumerable<RemovableVolume> items) =>
        items.Where(x => x is not null && IsDriveRoot(x.Root))
             .GroupBy(x => x.Root, StringComparer.OrdinalIgnoreCase)
             .Select(g => g.First());

    internal static bool IsDriveRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            var full = Path.GetFullPath(path);
            var root = Path.GetPathRoot(full);
            return root is not null &&
                string.Equals(Path.TrimEndingDirectorySeparator(full),
                    Path.TrimEndingDirectorySeparator(root), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}

internal static class DownloadGuardPolicy
{
    internal static string DownloadsFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

    public static IReadOnlyList<string> Compose(IEnumerable<string> personalFolders,
        bool enabled, string downloadsPath)
    {
        var result = personalFolders.Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (enabled && !result.Contains(downloadsPath, StringComparer.OrdinalIgnoreCase))
        {
            // Si un directorio padre ya está vigilado no añadir otra raíz solapada.
            var covered = result.Any(x => IsWithin(downloadsPath, x));
            if (!covered)
            {
                // El padre sustituye a raíces hijas para no crear observadores duplicados.
                result.RemoveAll(x => IsWithin(x, downloadsPath));
                result.Add(downloadsPath);
            }
        }
        if (result.Count > 5)
            throw new ArgumentException("Guard admite como máximo cinco raíces, incluida Descargas.");
        return result;
    }

    public static bool IsWithin(string path, string root)
    {
        try
        {
            var p = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            var r = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            return p.Equals(r, StringComparison.OrdinalIgnoreCase) ||
                p.StartsWith(r + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        { return false; }
    }
}
