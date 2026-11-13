// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

internal enum SmartScanPreset { Quick, Full, Custom }

internal sealed record SmartScanPlan(SmartScanPreset Preset, IReadOnlyList<string> Targets,
    string DisplayTarget, string Priority, int BatchSize, string Guidance)
{
    public bool IsMultiTarget => Targets.Count > 1;
}

/// <summary>
/// Planifica objetivos del análisis sin ejecutar archivos ni crear exclusiones ocultas.
/// Los presets cambian el ALCANCE y la prioridad, no el resultado de ClamAV.
/// </summary>
internal static class SmartScanPlanPolicy
{
    public static IReadOnlyList<string> CollapseTargets(IEnumerable<string> candidates)
    {
        var paths = new List<string>();
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            string full;
            try { full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate.Trim().Trim('"'))); }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { continue; }
            if (!Path.IsPathFullyQualified(full)) continue;
            if (!paths.Contains(full, StringComparer.OrdinalIgnoreCase)) paths.Add(full);
            if (paths.Count >= 40) break; // defensa contra listas externas ilimitadas
        }
        paths.Sort((a,b) => a.Length != b.Length ? a.Length.CompareTo(b.Length) :
            StringComparer.OrdinalIgnoreCase.Compare(a,b));
        var roots = new List<string>();
        foreach (var path in paths)
        {
            if (roots.Any(existing => IsUnder(path, existing))) continue;
            roots.Add(path);
        }
        return roots;
    }

    private static bool IsUnder(string path, string parent) =>
        path.Equals(parent, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(parent.EndsWith(Path.DirectorySeparatorChar) ? parent :
            parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    public static SmartScanPlan Create(SmartScanPreset preset, IEnumerable<string> paths,
        string selectedPriority = "Equilibrado", string controlMode = "Equilibrado")
    {
        var roots = CollapseTargets(paths);
        if (roots.Count == 0) throw new ArgumentException("No hay rutas válidas para el análisis.");
        var name = preset switch
        {
            SmartScanPreset.Quick => "Rápido",
            SmartScanPreset.Full => "Completo",
            _ => "Personalizado"
        };
        var priority = preset switch
        {
            SmartScanPreset.Full => "Económico",
            SmartScanPreset.Quick => "Equilibrado",
            _ => selectedPriority is "Económico" or "Prioritario" ? selectedPriority : "Equilibrado"
        };
        var batch = preset == SmartScanPreset.Custom && controlMode == "Preciso"
            ? ControlledScanRunner.PreciseBatchSize :
            preset == SmartScanPreset.Full ? 256 : ControlledScanRunner.BalancedBatchSize;
        var description = preset switch
        {
            SmartScanPreset.Quick => "Ubicaciones habituales del usuario; no revisa todo Windows ni todas las unidades.",
            SmartScanPreset.Full => "Discos locales fijos disponibles; puede durar horas y omitir rutas por permisos.",
            _ => "Objetivo escogido manualmente; las exclusiones de usuario siguen activas."
        };
        // Incluir raíces reales en la identidad del informe para evitar comparaciones engañosas.
        var label = preset == SmartScanPreset.Custom && roots.Count == 1
            ? roots[0] : name + " · " + string.Join(" | ", roots);
        return new SmartScanPlan(preset, roots, label, priority, batch, description);
    }

    public static IReadOnlyList<string> QuickCandidates()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return CollapseTargets(new[]
        {
            Path.Combine(home, "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.Startup)
        }.Where(Directory.Exists));
    }

    public static IReadOnlyList<string> LocalFixedDrives()
    {
        var roots = new List<string>();
        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                        roots.Add(drive.RootDirectory.FullName);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
        }
        catch (IOException) { }
        return CollapseTargets(roots);
    }
}
