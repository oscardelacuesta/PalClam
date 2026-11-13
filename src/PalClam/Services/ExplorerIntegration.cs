// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using Microsoft.Win32;

namespace PalClam.Services;

/// <summary>
/// Integra el menú contextual solo en HKCU; no requiere elevación.
/// Nunca modifica los verbos de otros programas.
/// </summary>
internal static class ExplorerIntegration
{
    private const string Owner = "palentino.es/PalClam";
    private static readonly string[] Keys =
    {
        @"Software\Classes\*\shell\PalClamScan",
        @"Software\Classes\Directory\shell\PalClamScan",
        @"Software\Classes\Drive\shell\PalClamScan"
    };

    public static string Status()
    {
        if (AppPaths.IsPortable) return "Integración de Windows desactivada en modo portable.";
        var exe = Environment.ProcessPath ?? "";
        if (string.IsNullOrWhiteSpace(exe) ||
            !Path.GetFileName(exe).Equals("PalClam.exe", StringComparison.OrdinalIgnoreCase))
            return "Integración no verificable: ejecuta PalClam.exe instalado, no el proceso de pruebas.";
        if (IsEnabled()) return "Integración activa para archivos, carpetas y unidades del usuario actual.";
        foreach (var key in Keys)
        {
            using var node = Registry.CurrentUser.OpenSubKey(key);
            if (node?.GetValue("PalClamOwner") is string owner && owner == Owner)
                return "Se detectó una entrada antigua o incompleta. Desmarca y vuelve a marcar la opción para repararla.";
        }
        return "Integración no activada en este perfil de Windows.";
    }

    public static bool IsEnabled()
    {
        if (AppPaths.IsPortable) return false;
        var exe = Environment.ProcessPath ?? "";
        if (!File.Exists(exe) || !Path.GetFileName(exe).Equals("PalClam.exe", StringComparison.OrdinalIgnoreCase)) return false;
        var expectedCommand = $"\"{exe}\" --scan \"%1\"";
        return Keys.All(key =>
        {
            using var node = Registry.CurrentUser.OpenSubKey(key);
            if (node?.GetValue("PalClamOwner") is not string owner || owner != Owner) return false;
            using var cmd = node.OpenSubKey("command");
            return string.Equals(cmd?.GetValue("") as string, expectedCommand,
                StringComparison.OrdinalIgnoreCase);
        });
    }

    public static void SetEnabled(bool enabled, string menuLabel = "Analizar con PalClam")
    {
        if (AppPaths.IsPortable) throw new InvalidOperationException("Integración de Windows desactivada en modo portable.");
        var exe = Environment.ProcessPath;
        if (enabled && (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe) ||
            !Path.GetFileName(exe).Equals("PalClam.exe", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("No se encuentra PalClam.exe.");

        foreach (var key in Keys)
        {
            if (enabled)
            {
                using var node = Registry.CurrentUser.CreateSubKey(key, true)
                    ?? throw new InvalidOperationException("No se pudo crear la clave: " + key);
                var currentOwner = node.GetValue("PalClamOwner") as string;
                if (currentOwner is not null && currentOwner != Owner)
                    throw new InvalidOperationException("La clave pertenece a otro programa: " + key);
                node.SetValue("PalClamOwner", Owner, RegistryValueKind.String);
                node.SetValue("MUIVerb", menuLabel, RegistryValueKind.String);
                node.SetValue("Icon", exe!, RegistryValueKind.String);
                using var command = node.CreateSubKey("command", true)
                    ?? throw new InvalidOperationException("No se pudo escribir el comando del Explorador.");
                command.SetValue("", $"\"{exe}\" --scan \"%1\"", RegistryValueKind.String);
            }
            else
            {
                using var existing = Registry.CurrentUser.OpenSubKey(key);
                if (existing?.GetValue("PalClamOwner") is string owner && owner == Owner)
                    Registry.CurrentUser.DeleteSubKeyTree(key, throwOnMissingSubKey: false);
            }
        }
    }
}
