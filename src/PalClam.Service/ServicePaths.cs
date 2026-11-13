// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Text.Json;

namespace PalClam.MachineService;

internal static class ServicePaths
{
    public const string ServiceName = "PalClamGuardSvc";
    public static readonly string CommonRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PalClam");
    // No leer perfiles ni aceptar una carpeta que una cuenta sin privilegios pueda cambiar en configuración.
    public static readonly string WatchedFolder = Path.Combine(CommonRoot, "GuardWatch");
    public static readonly string StateFolder = Path.Combine(CommonRoot, "MachineGuard");
    public static readonly string StateFile = Path.Combine(StateFolder, "estado.json");
    public static readonly string EventsFile = Path.Combine(StateFolder, "eventos.log");
    public static string ScannerPath => Path.Combine(AppContext.BaseDirectory, "ClamAV", "clamscan.exe");
    // Bases compartidas, actualizadas solo desde la herramienta administrativa elevada.
    // NUNCA usar el perfil de un usuario ni aceptar cambios desde una ruta de red.
    public static string SignaturesFolder => Path.Combine(StateFolder, "Signatures");

    public static bool SignaturesPresent()
    {
        try
        {
            return HasFamily("main") && HasFamily("daily");
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }
    private static bool HasFamily(string family) =>
        new[] { ".cvd", ".cld" }.Any(extension =>
        {
            var file = Path.Combine(SignaturesFolder, family + extension);
            return File.Exists(file) && new FileInfo(file).Length >= 128;
        });
}

internal sealed record MachineGuardStatus(
    string Mode, DateTimeOffset Utc, string Detail,
    long Scanned, long Detections, long Skipped, long Errors, long LostEvents,
    string WatchFolder, string Signatures)
{
    public long Pending { get; init; }
    public long Reconciliations { get; init; }
}

internal static class MachineGuardReport
{
    private static readonly object Gate = new();
    public static void WriteStatus(MachineGuardStatus value)
    {
        lock (Gate)
        {
            Directory.CreateDirectory(ServicePaths.StateFolder);
            var temporary = ServicePaths.StateFile + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, JsonSerializer.Serialize(value));
                File.Move(temporary, ServicePaths.StateFile, overwrite: true);
            }
            finally { try { File.Delete(temporary); } catch (IOException) { } }
        }
    }

    public static void Append(string level, string detail)
    {
        lock (Gate)
        {
            Directory.CreateDirectory(ServicePaths.StateFolder);
            if (File.Exists(ServicePaths.EventsFile) && new FileInfo(ServicePaths.EventsFile).Length > 4_000_000)
                File.Move(ServicePaths.EventsFile, Path.Combine(ServicePaths.StateFolder, "eventos-anterior.log"), true);
            // Una línea por evento, sin posibles saltos de línea de rutas o mensajes externos.
            var oneLine = detail.Replace('\r', ' ').Replace('\n', ' ');
            File.AppendAllText(ServicePaths.EventsFile,
                DateTimeOffset.UtcNow.ToString("O") + " [" + level + "] " + oneLine + Environment.NewLine);
        }
    }
}
