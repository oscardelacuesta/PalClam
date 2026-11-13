// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.IO;

namespace PalClam.Services;

/// <summary>Pure command-line rules: tests never touch Windows registry.</summary>
internal enum StartupScope { Disabled, CurrentUser, AllUsers }

internal static class StartupLaunchPolicy
{
    // HKLM/Run para todos los usuarios solo debe apuntar a un ejecutable instalado
    // bajo Program Files, nunca a Downloads, Desktop o un perfil privado.
    internal static bool IsMachineInstallPath(string? exePath)
    {
        if (AppPaths.IsPortable || string.IsNullOrWhiteSpace(exePath)) return false;
        try
        {
            if (!Path.IsPathFullyQualified(exePath) ||
                !string.Equals(Path.GetFileName(exePath), "PalClam.exe", StringComparison.OrdinalIgnoreCase))
                return false;
            var full = Path.GetFullPath(exePath);
            foreach (var folder in new[] {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) })
            {
                if (string.IsNullOrWhiteSpace(folder)) continue;
                var prefix = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { }
        return false;
    }

    internal const string HiddenArgument = "--startup-hidden";

    internal static bool IsHiddenStartup(string[] args) =>
        args.Length == 1 && string.Equals(args[0], HiddenArgument, StringComparison.OrdinalIgnoreCase);

    internal static string BuildRunCommand(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath) || exePath.Contains('"') ||
            !Path.IsPathFullyQualified(exePath) ||
            !string.Equals(Path.GetFileName(exePath), "PalClam.exe", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Se necesita la ruta completa a PalClam.exe.", nameof(exePath));
        // Citar siempre la ruta: Program Files y perfiles pueden tener espacios.
        var command = "\"" + exePath + "\" " + HiddenArgument;
        if (command.Length > 260)
            throw new ArgumentException("La ruta es demasiado larga para el inicio de Windows.", nameof(exePath));
        return command;
    }
}
