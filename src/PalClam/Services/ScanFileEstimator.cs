// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

internal sealed record FileEstimate(long Count, long InaccessibleDirectories, bool Truncated);

/// <summary>
/// Cuenta archivos para ofrecer una orientación de progreso. El recuento NO es
/// equivalente al total interno de ClamAV (archivos comprimidos, límites, etc.).
/// </summary>
internal static class ScanFileEstimator
{
    private const long MaxFiles = 500_000;

    public static FileEstimate Estimate(string target, CancellationToken ct,
        IReadOnlyCollection<string>? excluded = null) =>
        Estimate(new[] { target }, ct, excluded);

    public static FileEstimate Estimate(IReadOnlyList<string> targets, CancellationToken ct,
        IReadOnlyCollection<string>? excluded = null)
    {
        // El máximo de 500 000 se aplica al conjunto, no a cada unidad por separado.
        var stack = new Stack<string>();
        long initialCount = 0, missing = 0;
        foreach (var target in SmartScanPlanPolicy.CollapseTargets(targets).Reverse())
        {
            ct.ThrowIfCancellationRequested();
            if (ScanExclusions.IsExcluded(target, excluded)) continue;
            if (File.Exists(target)) { initialCount++; continue; }
            if (Directory.Exists(target)) stack.Push(target);
            else missing++;
        }
        long count = initialCount, inaccessible = missing;
        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var folder = stack.Pop();
            if (ScanExclusions.IsExcluded(folder, excluded)) continue;
            try
            {
                using var entries = Directory.EnumerateFileSystemEntries(folder).GetEnumerator();
                while (entries.MoveNext())
                {
                    ct.ThrowIfCancellationRequested();
                    var current = entries.Current;
                    if (ScanExclusions.IsExcluded(current, excluded)) continue;
                    try
                    {
                        var attributes = File.GetAttributes(current);
                        if ((attributes & FileAttributes.ReparsePoint) != 0) continue;
                        if ((attributes & FileAttributes.Directory) != 0)
                            stack.Push(current);
                        else
                        {
                            count++;
                            if (count >= MaxFiles) return new(count, inaccessible, true);
                        }
                    }
                    catch (UnauthorizedAccessException) { inaccessible++; }
                    catch (IOException) { inaccessible++; }
                }
            }
            catch (UnauthorizedAccessException) { inaccessible++; }
            catch (IOException) { inaccessible++; }
        }
        return new(count, inaccessible, false);
    }
}
