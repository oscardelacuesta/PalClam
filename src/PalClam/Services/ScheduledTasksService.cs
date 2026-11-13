// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Diagnostics;

namespace PalClam.Services;

/// <summary>Programación por usuario mediante schtasks.exe; no crea servicios ni requiere administrador.</summary>
internal static class ScheduledTasksService
{
    private const string TaskName = "PalClam - Analisis programado";
    private static string Scheduler => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe");

    public static Task<(bool Success, string Detail)> CreateAsync(bool weekly, string hour)
    {
        if (AppPaths.IsPortable) return Task.FromResult((false, "Integración de Windows desactivada en modo portable."));
        if (!TimeSpan.TryParseExact(hour, @"hh\:mm", System.Globalization.CultureInfo.InvariantCulture, out var time) ||
            time >= TimeSpan.FromDays(1))
            return Task.FromResult((false, "Hora no válida. Utiliza HH:mm (00:00 a 23:59)."));
        var exe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "PalClam.exe");
        if (!File.Exists(exe)) return Task.FromResult((false, "No se encuentra el ejecutable de PalClam."));
        var command = "\"" + exe + "\" --scheduled-scan";
        var args = new List<string> { "/Create", "/TN", TaskName, "/TR", command,
            "/SC", weekly ? "WEEKLY" : "DAILY", "/ST", hour, "/F", "/IT" };
        if (weekly) { args.Add("/D"); args.Add("MON"); }
        return ExecuteAsync(args);
    }

    public static Task<(bool Success, string Detail)> DeleteAsync() => AppPaths.IsPortable
        ? Task.FromResult((false, "Integración de Windows desactivada en modo portable.")) :
        ExecuteAsync(new[] { "/Delete", "/TN", TaskName, "/F" });

    public static Task<(bool Success, string Detail)> StatusAsync() => AppPaths.IsPortable
        ? Task.FromResult((true, "Integración de Windows desactivada en modo portable.")) :
        ExecuteAsync(new[] { "/Query", "/TN", TaskName, "/FO", "LIST" });

    private static async Task<(bool Success, string Detail)> ExecuteAsync(IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo(Scheduler)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = psi };
        if (!process.Start()) return (false, "No se pudo iniciar el Programador de tareas.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var detail = (await stdout + "\n" + await stderr).Trim();
        return (process.ExitCode == 0, detail.Length > 1500 ? detail[..1500] : detail);
    }
}
