// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Diagnostics;
using System.Security.Cryptography;

namespace PalClam.Services;

internal static class UpdateLauncher
{
    // Ejecutamos un proceso externo; Windows no permite sustituir un EXE abierto por el propio EXE.
    public static void LaunchInstallerAfterExit(string verifiedInstaller, string expectedSha256)
    {
        if (!File.Exists(verifiedInstaller)) throw new FileNotFoundException("No existe el instalador verificado.", verifiedInstaller);
        if (expectedSha256.Length != 64 || !expectedSha256.All(Uri.IsHexDigit))
            throw new CryptographicException("Hash de instalador invalido.");

        var source = Path.Combine(AppContext.BaseDirectory, "Updater", "ApplyUpdate.ps1");
        if (!File.Exists(source)) throw new FileNotFoundException("No se ha instalado el ayudante de actualizaciones.", source);
        var folder = Path.GetDirectoryName(verifiedInstaller)!;
        var script = Path.Combine(folder, "ApplyUpdate.ps1");
        File.Copy(source, script, overwrite: true);
        var appExe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "PalClam.exe");

        var psi = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = folder,
        };
        foreach (var arg in new[]
        {
            "-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass",
            "-File", script, "-InstallerPath", verifiedInstaller,
            "-ExpectedSha256", expectedSha256, "-ParentPid", Environment.ProcessId.ToString(),
            "-AppExe", appExe
        }) psi.ArgumentList.Add(arg);
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("No se ha podido ejecutar el actualizador.");
        // El llamador debe cerrar PalClam inmediatamente despues.
    }
}
