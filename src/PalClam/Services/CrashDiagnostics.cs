// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Text;
using System.Text.RegularExpressions;

namespace PalClam.Services;

/// <summary>
/// Errores locales sin datos de documentos, claves, contenido de archivos ni telemetría remota.
/// El mensaje se recorta y oculta rutas potencialmente personales. Se conservan los
/// nombres de los tipos de excepción y métodos de la pila, útiles para depuración.
/// </summary>
internal static class CrashDiagnostics
{
    private static readonly object Sync = new();
    private const long MaxBytes = 512 * 1024;
    private static readonly Regex WindowsPath = new(
        """(?i)(?:[A-Z]:\\|\\\\)[^\s"<>|]+(?:\\[^\s"<>|]+)*""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static string LogPath => Path.Combine(
        PalClam.Services.AppPaths.Root, "Diagnostics", "fallos.log");

    internal static void Record(string context, Exception exception)
    {
        try
        {
            var details = new StringBuilder();
            details.AppendLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}] {context}");
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                details.Append("  Excepción: ").AppendLine(current.GetType().FullName);
                var message = current.Message.Replace('\r', ' ').Replace('\n', ' ');
                if (message.Length > 500) message = message[..500];
                details.Append("  Motivo: ").AppendLine(WindowsPath.Replace(message, "[ruta omitida]"));
                if (current.StackTrace is not null)
                {
                    foreach (var line in current.StackTrace.Split('\n').Take(12))
                    {
                        // Eliminar rutas a fuentes locales en una pila con PDB.
                        var frame = line.Trim().Split(" in ", 2, StringSplitOptions.None)[0];
                        details.Append("    ").AppendLine(frame);
                    }
                }
            }
            details.AppendLine();
            lock (Sync)
            {
                var path = LogPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                if (File.Exists(path) && new FileInfo(path).Length > MaxBytes)
                    File.Move(path, path + ".anterior", true);
                File.AppendAllText(path, details.ToString(), Encoding.UTF8);
            }
        }
        catch { /* El diagnóstico jamás debe provocar otra excepción. */ }
    }
}
