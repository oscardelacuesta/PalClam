// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

internal static class ClamAvLocator
{
    public static string BundledScanner => Path.Combine(AppContext.BaseDirectory, "ClamAV", "clamscan.exe");
    public static string BundledUpdater => Path.Combine(AppContext.BaseDirectory, "ClamAV", "freshclam.exe");

    public static Version? BundledVersion
    {
        get
        {
            try
            {
                var versionFile = Path.Combine(AppContext.BaseDirectory, "ClamAV", "PalClam-Engine.txt");
                if (!DistributionPolicy.MatchesExecutable(BundledScanner, AppPaths.RuntimeId) || !DistributionPolicy.MatchesExecutable(BundledUpdater, AppPaths.RuntimeId) || !File.Exists(versionFile)) return null;
                return Version.TryParse(File.ReadAllText(versionFile).Trim(), out var version) ? version : null;
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }
    }

    public static Version? EffectiveManagedVersion
    {
        get
        {
            var downloaded = ClamAvEngineManager.InstalledVersion;
            var bundled = BundledVersion;
            if (downloaded is null) return bundled;
            if (bundled is null) return downloaded;
            return downloaded > bundled ? downloaded : bundled;
        }
    }

    public static string FindScanner()
    {
        var downloaded = ClamAvEngineManager.ActiveScanner;
        var downloadedVersion = ClamAvEngineManager.InstalledVersion;
        var bundledVersion = BundledVersion;
        if (!string.IsNullOrEmpty(downloaded) && (bundledVersion is null ||
            (downloadedVersion is not null && downloadedVersion >= bundledVersion))) return downloaded;
        if (bundledVersion is not null) return BundledScanner;
        if (!string.IsNullOrEmpty(downloaded)) return downloaded;
        if (AppPaths.IsPortable) return "";
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ClamAV", "clamscan.exe"),
            @"C:\ClamAV\clamscan.exe",
        };
        return candidates.FirstOrDefault(File.Exists) ?? "";
    }

    public static bool IsValidScanner(string path) =>
        !string.IsNullOrWhiteSpace(path)
        && Path.GetFileName(path).Equals("clamscan.exe", StringComparison.OrdinalIgnoreCase)
        && DistributionPolicy.MatchesExecutable(path, AppPaths.RuntimeId);

    public static string FreshClamFor(string scannerPath) =>
        Path.Combine(Path.GetDirectoryName(scannerPath) ?? "", "freshclam.exe");
}
