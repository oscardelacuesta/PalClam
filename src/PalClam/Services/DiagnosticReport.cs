// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Reflection;
using System.Text;

namespace PalClam.Services;

internal static class DiagnosticReport
{
    public static string Create(string scanner, string? engineVersion = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PALCLAM · DIAGNÓSTICO LOCAL");
        sb.AppendLine("Generado: " + DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss zzz"));
        sb.AppendLine("Versión PalClam: " + (Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "No disponible"));
        sb.AppendLine("Windows: " + Environment.OSVersion.VersionString);
        sb.AppendLine("Arquitectura: " + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture);
        sb.AppendLine("Directorio: " + AppContext.BaseDirectory);
        sb.AppendLine();
        sb.AppendLine("MOTOR CLAMAV");
        sb.AppendLine("Ubicación: " + (string.IsNullOrWhiteSpace(scanner) ? "No configurado" : scanner));
        sb.AppendLine("Encontrado: " + (ClamAvLocator.IsValidScanner(scanner) ? "Sí" : "No"));
        sb.AppendLine("Versión del ejecutable: " + (engineVersion ?? "No comprobada"));
        sb.AppendLine();
        sb.AppendLine("FIRMAS");
        sb.AppendLine("Carpeta: " + SignatureStore.DatabaseDirectory);
        sb.AppendLine("Bases disponibles: " + (SignatureStore.HasSignatures() ? "Sí" : "No"));
        foreach (var name in new[] { "main", "daily", "bytecode" })
        {
            var file = new[] { name + ".cld", name + ".cvd" }
                .Select(f => Path.Combine(SignatureStore.DatabaseDirectory, f))
                .FirstOrDefault(File.Exists);
            if (file is null) { sb.AppendLine($"- {name}: ausente"); continue; }
            try
            {
                var info = new FileInfo(file);
                sb.AppendLine($"- {name}: {info.Name} · {info.Length:N0} bytes · {info.LastWriteTime:dd/MM/yyyy HH:mm}");
            }
            catch (Exception ex) { sb.AppendLine($"- {name}: no se pudo leer ({ex.GetType().Name})"); }
        }
        sb.AppendLine();
        sb.AppendLine("EXPLORADOR DE WINDOWS");
        try { sb.AppendLine("Menú contextual activo: " + (ExplorerIntegration.IsEnabled() ? "Sí" : "No")); }
        catch { sb.AppendLine("Menú contextual: no se pudo comprobar"); }
        sb.AppendLine();
        sb.AppendLine("PRIVACIDAD");
        sb.AppendLine("Este diagnóstico solo recoge estado técnico local. Puede contener rutas personales.");
        sb.AppendLine("PalClam Guard es una vigilancia opcional de cambios de archivos; no bloquea accesos ni sustituye Microsoft Defender.");
        return sb.ToString();
    }
}
