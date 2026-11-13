// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace PalClam.Services;

/// <summary>
/// El inicio es voluntario: HKCU para una cuenta, HKLM para cada sesion.
/// El registro HKLM solo puede configurarse mediante el auxiliar elevado y desde
/// una instalación compartida de Program Files. Ninguna modalidad es un servicio.
/// </summary>
internal static class WindowsStartupRegistration
{
    private const string RunSubkey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string Name = "PalClam";

    private static string CurrentCommand() =>
        StartupLaunchPolicy.BuildRunCommand(Environment.ProcessPath ?? "");

    private static string? ReadCurrentUserCommand()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunSubkey, writable: false);
        return key?.GetValue(Name) as string;
    }

    private static string? ReadMachineCommand()
    {
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
        using var key = root.OpenSubKey(RunSubkey, writable: false);
        return key?.GetValue(Name) as string;
    }

    private static bool Matches(string? value) =>
        string.Equals(value, CurrentCommand(), StringComparison.OrdinalIgnoreCase);

    internal static StartupScope GetScope()
    {
        if (AppPaths.IsPortable) return StartupScope.Disabled;
        try
        {
            if (Matches(ReadMachineCommand())) return StartupScope.AllUsers;
            return Matches(ReadCurrentUserCommand()) ? StartupScope.CurrentUser : StartupScope.Disabled;
        }
        catch (Exception ex) when (Expected(ex)) { return StartupScope.Disabled; }
    }

    // Compatibilidad con el codigo y las pruebas de versiones previas.
    internal static bool IsEnabledForCurrentExecutable() => GetScope() != StartupScope.Disabled;

    internal static string Status()
    {
        if (AppPaths.IsPortable) return "Integración de Windows desactivada en modo portable.";
        try
        {
            var machine = ReadMachineCommand();
            var user = ReadCurrentUserCommand();
            if (Matches(machine))
                return Matches(user)
                    ? "Inicio para todas las cuentas activo. Existe también un inicio individual redundante en esta cuenta."
                    : "Inicio para todos los usuarios activo: aparece en la bandeja al entrar en cada sesión. Guard requiere configuración individual.";
            if (!string.IsNullOrWhiteSpace(machine))
                return "Hay otro PalClam registrado para todas las cuentas. No se modifica automáticamente su registro.";
            if (Matches(user)) return "Inicio automático activo solo para tu cuenta.";
            return string.IsNullOrWhiteSpace(user)
                ? "Inicio con Windows desactivado."
                : "Hay una entrada de inicio individual de otra ubicación; selecciónala de nuevo para actualizarla.";
        }
        catch (Exception ex) when (Expected(ex)) { return "No se pudo consultar el inicio automático: " + ex.Message; }
    }

    internal static bool TrySet(StartupScope scope, out string error)
    {
        error = "";
        if (AppPaths.IsPortable) { error = "Integración de Windows desactivada en modo portable."; return false; }
        try
        {
            var machine = ReadMachineCommand();
            if (!string.IsNullOrWhiteSpace(machine) && !Matches(machine))
            {
                error = "Hay otra instalación de PalClam registrada para todos los usuarios. " +
                        "Desinstálala o administra su inicio desde esa instalación.";
                return false;
            }

            if (scope == StartupScope.AllUsers)
            {
                if (!StartupLaunchPolicy.IsMachineInstallPath(Environment.ProcessPath ?? ""))
                {
                    error = "Para iniciar en todas las cuentas debes instalar PalClam para todos los usuarios " +
                            "en Program Files. Esta copia pertenece a una instalación individual o portátil.";
                    return false;
                }
                if (!Matches(machine) && !RunElevatedHelper(true, out error)) return false;
                // Evitar dos entradas para la cuenta actual. Otras cuentas mantienen
                // sus preferencias anteriores; nunca se editan perfiles ajenos.
                if (!WriteCurrentUser(false, out error)) return false;
                return true;
            }

            if (Matches(machine) && !RunElevatedHelper(false, out error)) return false;
            return WriteCurrentUser(scope == StartupScope.CurrentUser, out error);
        }
        catch (Exception ex) when (Expected(ex))
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool WriteCurrentUser(bool enabled, out string error)
    {
        error = "";
        try
        {
            using var key = enabled
                ? Registry.CurrentUser.CreateSubKey(RunSubkey, writable: true)
                : Registry.CurrentUser.OpenSubKey(RunSubkey, writable: true);
            if (enabled)
            {
                if (key is null) throw new IOException("No se pudo abrir la clave de inicio de la cuenta.");
                key.SetValue(Name, CurrentCommand(), RegistryValueKind.String);
            }
            else if (key?.GetValue(Name) is string current && Matches(current))
                key.DeleteValue(Name, throwOnMissingValue: false);
            return true;
        }
        catch (Exception ex) when (Expected(ex)) { error = ex.Message; return false; }
    }

    private static bool RunElevatedHelper(bool enabled, out string error)
    {
        error = "";
        try
        {
            var exe = Environment.ProcessPath ?? throw new IOException("No se encuentra PalClam.exe.");
            var start = new ProcessStartInfo(exe)
            {
                UseShellExecute = true,
                Verb = "runas",
                Arguments = enabled ? "--set-machine-startup enable" : "--set-machine-startup disable",
                WorkingDirectory = Path.GetDirectoryName(exe) ?? ""
            };
            using var process = Process.Start(start);
            if (process is null) throw new IOException("No se pudo iniciar la configuración administrativa.");
            process.WaitForExit();
            if (process.ExitCode == 0) return true;
            error = "Windows no permitió actualizar el inicio para todos los usuarios (código " + process.ExitCode + ").";
            return false;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            error = "Has cancelado la autorización de administrador. No se ha cambiado el inicio global.";
            return false;
        }
        catch (Exception ex) when (Expected(ex) || ex is Win32Exception)
        {
            error = ex.Message;
            return false;
        }
    }

    // Solo se ejecuta desde Program.Main mediante --set-machine-startup enable|disable.
    // Nunca abre WPF, Guard ni carga claves del usuario.
    internal static int ApplyMachineChange(bool enabled)
    {
        if (AppPaths.IsPortable) return 2;
        try
        {
            if (enabled && !StartupLaunchPolicy.IsMachineInstallPath(Environment.ProcessPath ?? "")) return 3;
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            using var key = enabled
                ? root.CreateSubKey(RunSubkey, writable: true)
                : root.OpenSubKey(RunSubkey, writable: true);
            if (key is null) return enabled ? 4 : 0;
            var old = key.GetValue(Name) as string;
            if (!string.IsNullOrWhiteSpace(old) && !Matches(old)) return 5;
            if (enabled) key.SetValue(Name, CurrentCommand(), RegistryValueKind.String);
            else key.DeleteValue(Name, throwOnMissingValue: false);
            return 0;
        }
        catch (Exception ex) when (Expected(ex))
        {
            CrashDiagnostics.Record("configuracion-inicio-global", ex);
            return 2;
        }
    }

    private static bool Expected(Exception ex) => ex is IOException or UnauthorizedAccessException
        or SecurityException or ArgumentException or NotSupportedException;
}
