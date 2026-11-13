// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace PalClam.Services;

/// <summary>
/// Monitor auxiliar optativo. NO es servicio Windows, no blinda procesos ni los reinicia.
/// Se ejecuta con los permisos normales del usuario desde una SEGUNDA instancia
/// de PalClam.exe sin crear la interfaz WPF.
/// </summary>
internal static class SelfDefenseMonitor
{
    private const string Command = "--self-defense-watch";
    private const string Prefix = "Local\\PalClam.SelfDefense.";
    private const long MaxLogBytes = 1024 * 1024;

    internal sealed class Incident
    {
        public DateTimeOffset Utc { get; set; }
        public string Kind { get; set; } = "";
        public string Message { get; set; } = "";
    }

    internal sealed class Session : IDisposable
    {
        private EventWaitHandle? _gracefulExit;
        private Process? _watchdog;
        public int WatchdogPid => _watchdog?.Id ?? 0;
        public bool IsAlive
        {
            get
            {
                try { return _watchdog is not null && !_watchdog.HasExited; }
                catch { return false; }
            }
        }

        internal Session(EventWaitHandle gracefulExit, Process watchdog)
        {
            _gracefulExit = gracefulExit;
            _watchdog = watchdog;
        }

        public void Dispose()
        {
            // Una parada voluntaria se comunica al monitor, sin matar procesos.
            try { _gracefulExit?.Set(); } catch { /* cierre seguro */ }
            _gracefulExit?.Dispose();
            _gracefulExit = null;
            _watchdog?.Dispose();
            _watchdog = null;
        }
    }

    internal static string AuditFolder => Path.Combine(
        PalClam.Services.AppPaths.Root, "SelfDefense");
    internal static string AuditPath => Path.Combine(AuditFolder, "eventos.jsonl");

    public static Session? TryStart(out string message)
    {
        EventWaitHandle? ready = null;
        try
        {
            using var current = Process.GetCurrentProcess();
            var exe = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe) ||
                !exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                message = "No se localiza el ejecutable de PalClam para iniciar la supervisión.";
                return null;
            }

            var pid = current.Id;
            var ticks = current.StartTime.ToUniversalTime().Ticks;
            var eventName = Prefix + pid.ToString(CultureInfo.InvariantCulture) + "." + Guid.NewGuid().ToString("N");
            ready = new EventWaitHandle(false, EventResetMode.ManualReset, eventName);
            var info = new ProcessStartInfo(exe)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = AppContext.BaseDirectory
            };
            info.ArgumentList.Add(Command);
            info.ArgumentList.Add(pid.ToString(CultureInfo.InvariantCulture));
            info.ArgumentList.Add(ticks.ToString(CultureInfo.InvariantCulture));
            info.ArgumentList.Add(eventName);
            Process? helper = Process.Start(info);
            if (helper is null) throw new InvalidOperationException("El monitor auxiliar no pudo arrancar.");
            message = "Supervisión auxiliar iniciada; cierre normal permitido.";
            var session = new Session(ready, helper);
            ready = null;
            return session;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
                                    System.ComponentModel.Win32Exception or
                                    InvalidOperationException or ArgumentException)
        {
            message = "Supervisión no disponible: " + ex.GetType().Name;
            return null;
        }
        finally { ready?.Dispose(); }
    }

    /// <summary>Solo se invoca mediante el parámetro interno con 3 valores estrictos.</summary>
    public static int RunHelper(string[] args)
    {
        if (args.Length != 4 || args[0] != Command ||
            !int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out var pid) || pid < 4 ||
            !long.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) || ticks < 1 ||
            !args[3].StartsWith(Prefix, StringComparison.Ordinal) || args[3].Length > 135)
            return 2;
        try
        {
            using var signal = EventWaitHandle.OpenExisting(args[3]);
            using var parent = Process.GetProcessById(pid);
            if (parent.StartTime.ToUniversalTime().Ticks != ticks) return 2; // PID reutilizado.
            // NO se reinicia PalClam ni se interviene en su proceso: solo observación.
            while (true)
            {
                if (signal.WaitOne(TimeSpan.FromSeconds(2))) return 0; // Salida autorizada.
                if (parent.HasExited) break;
            }
            if (signal.WaitOne(0)) return 0;
            Append("unexpected-exit",
                "PalClam terminó sin confirmar una salida voluntaria. Puede ser un cierre forzado, fallo o cierre de sesión; no demuestra un ataque.");
            return 0;
        }
        catch (ArgumentException) { return 2; }
        catch (InvalidOperationException) { return 2; }
        catch (System.Threading.WaitHandleCannotBeOpenedException) { return 2; }
        catch (Exception) { return 3; } // No continuar ante errores o permisos imprevistos.
    }

    public static void RecordUnhandled(Exception ex)
    {
        // No registrar mensajes ni rutas: pueden incluir datos personales y claves.
        Append("unhandled-exception", "Error no controlado: " + ex.GetType().Name);
    }

    private static void Append(string kind, string message)
    {
        try
        {
            Directory.CreateDirectory(AuditFolder);
            if (File.Exists(AuditPath) && new FileInfo(AuditPath).Length > MaxLogBytes)
                File.Move(AuditPath, Path.Combine(AuditFolder, "eventos-anterior.jsonl"), true);
            var incident = new Incident { Utc = DateTimeOffset.UtcNow, Kind = kind, Message = message };
            File.AppendAllText(AuditPath, JsonSerializer.Serialize(incident) + Environment.NewLine);
        }
        catch { /* El registro nunca debe impedir que Windows cierre la app. */ }
    }

    internal static IReadOnlyList<Incident> ReadRecent(int limit = 15)
    {
        try
        {
            if (!File.Exists(AuditPath)) return Array.Empty<Incident>();
            return File.ReadLines(AuditPath).TakeLast(Math.Clamp(limit, 1, 50))
                .Select(line => { try { return JsonSerializer.Deserialize<Incident>(line); } catch { return null; } })
                .Where(x => x is not null).Cast<Incident>().Reverse().ToArray();
        }
        catch { return Array.Empty<Incident>(); }
    }
}
