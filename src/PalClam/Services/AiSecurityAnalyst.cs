// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using PalClam.Infrastructure;

namespace PalClam.Services;

/// <summary>
/// Plantillas DE LISTA BLANCA, exclusivamente locales. Nunca utiliza rutas,
/// nombres, firmas textuales, informes completos, identificadores o contenido.
/// Los textos del proveedor son asesoramiento no fiable: ninguna acción automática.
/// </summary>
internal static class AiSecurityAnalyst
{
    private const string Rules = "\nLímites: ClamAV puede omitir archivos o no detectar amenazas nuevas. " +
        "No afirmes que el equipo está protegido ni recomiendes desactivar Defender. " +
        "Indica comprobaciones prudentes y qué datos faltan. No propongas borrado automático.";

    public static string ForScan(ScanRecord? scan)
    {
        if (scan is null)
            return "Analiza la ausencia de historial de escaneos. No supongas que hubo uno. " + Rules;
        var state = scan.Detections > 0 || scan.ExitCode == 1 ? "con detecciones" :
            scan.Errors > 0 || scan.ExitCode is null or > 1 ||
            (scan.Outcome?.Contains("incomplet", StringComparison.OrdinalIgnoreCase) == true) ||
            (scan.Outcome?.Contains("cancel", StringComparison.OrdinalIgnoreCase) == true)
                ? "incompleto / no concluyente" :
            scan.ExitCode == 0 ? "sin detecciones reportadas" : "no concluyente";
        return "PalClam AI Security Analyst: explica este resumen estadístico de un análisis ClamAV.\n" +
            "Estado interpretado: " + state + "\n" +
            "Detecciones reportadas: " + Math.Max(0, scan.Detections) + "\n" +
            "Errores reportados: " + Math.Max(0, scan.Errors) + "\n" +
            "Archivos que el motor declara analizados: " +
            (scan.EngineScannedFiles < 0 ? "sin dato" : scan.EngineScannedFiles.ToString()) + "\n" +
            "Duración aproximada: " + Math.Max(0, scan.Seconds) + " segundos" + Rules;
    }

    public static string ForNotices(SecurityNoticeFeedResult? result)
    {
        if (result is null)
            return "Interpreta la ausencia de un diagnóstico legible de Guard y Machine Guard. " + Rules;
        var items = result.Items.Take(200).ToArray();
        return "PalClam AI Security Analyst: interpreta estos contadores de alertas LOCALES.\n" +
            "Registros considerados: " + items.Length + "\n" +
            "Detecciones: " + items.Count(x => x.Level == SecurityNoticeLevel.Threat) + "\n" +
            "Advertencias: " + items.Count(x => x.Level == SecurityNoticeLevel.Warning) + "\n" +
            "Errores: " + items.Count(x => x.Level == SecurityNoticeLevel.Error) + "\n" +
            "Avisos de Guard personal: " + items.Count(x => x.Source == "guard") + "\n" +
            "Avisos de Machine Guard: " + items.Count(x => x.Source == "machine") + "\n" +
            "Avisos de USB Watch: " + items.Count(x => x.Source == "device") + "\n" +
            "Registros inaccesibles: " + Math.Max(0, result.UnreadableFiles) + "\n" +
            "Registros inválidos: " + Math.Max(0, result.InvalidEntries) + "\n" +
            "Lectura limitada: " + (result.Limited || result.Items.Count > 200 ? "sí" : "no") + "\n" +
            "Las alertas registradas NO acreditan que se haya bloqueado o aislado un archivo." + Rules;
    }

    public static string ForHealth(IReadOnlyCollection<HealthFinding>? findings)
    {
        if (findings is null || findings.Count == 0)
            return "No se ha ejecutado Security Health 360 en esta sesión. Indica cómo comprobar " +
                "motor, firmas y Guard sin presumir que están operativos." + Rules;
        var known = new HashSet<string>(StringComparer.Ordinal)
        {
            "Motor ClamAV", "Firmas locales", "Guard personal", "Download Guard",
            "USB Watch", "Servicio de equipo", "Inicio con Windows", "Análisis programados"
        };
        // La cadena final nunca incluye Detail: puede contener información privada.
        var lines = findings.Where(x => known.Contains(x.Component)).Take(8)
            .Select(x => x.Component + ": " + (x.Level switch
            {
                HealthLevel.Ready => "operativo según comprobación local",
                HealthLevel.Attention => "requiere revisión",
                HealthLevel.Inactive => "desactivado",
                _ => "no verificado"
            })).ToArray();
        return "PalClam AI Security Analyst: interpreta estos estados locales, no una garantía antivirus.\n" +
            string.Join("\n", lines) + Rules;
    }

    public static string ForQuarantine(IReadOnlyCollection<QuarantineEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
            return "PalClam no tiene elementos de cuarentena registrados en la lista consultada. " +
                "Esto no significa ausencia de malware." + Rules;
        var active = entries.Where(x => x.RestoredAt is null && x.DeletedAt is null).ToArray();
        return "PalClam AI Security Analyst: interpreta estos contadores de cuarentena.\n" +
            "Entradas visibles: " + entries.Count + "\n" +
            "Aisladas: " + active.Length + "\n" +
            "Con advertencia de integridad: " + active.Count(x => x.IntegrityWarning) + "\n" +
            "Reanalizadas: " + active.Count(x => x.LastRecheckAt is not null) + "\n" +
            "Solicitudes de revisión manual: " + active.Count(x => x.RequestedManualReview) + "\n" +
            "Restauradas anteriormente: " + entries.Count(x => x.RestoredAt is not null) + "\n" +
            "Eliminadas anteriormente: " + entries.Count(x => x.DeletedAt is not null) + "\n" +
            "Ni el reanálisis sin detecciones ni la IA autorizan restauración automática." + Rules;
    }

    public static string ForNotice(SecurityNotice? notice)
    {
        if (notice is null)
            return "No hay alerta seleccionada. Explica cómo revisar el historial Guard." + Rules;
        var level = notice.Level switch
        {
            SecurityNoticeLevel.Threat => "detección ClamAV registrada",
            SecurityNoticeLevel.Warning => "advertencia de vigilancia",
            SecurityNoticeLevel.Error => "error de vigilancia o motor",
            _ => "información de actividad"
        };
        var source = notice.Source switch
        {
            "guard" => "Guard personal", "machine" => "Machine Guard",
            "device" => "USB Watch", _ => "fuente no identificada"
        };
        return "PalClam AI Security Analyst: guía de investigación de un aviso local.\n" +
            "Nivel: " + level + "\nOrigen: " + source + "\n" +
            "No se envían el nombre, la ruta, los detalles ni el contenido del archivo. " +
            "Propón cómo verificar el aviso localmente sin ejecutar muestras." + Rules;
    }

    /// <summary>Indicador conservador de privacidad para texto escrito por el usuario.
    /// No sustituye revisar el texto visible ni autoriza un envío.</summary>
    public static bool MayContainPrivateData(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(text,
                @"(?im)([a-z]:\\|\\\\[a-z0-9_.-]+\\|/(?:home|users)/|[\w.+-]+@[\w.-]+\.[a-z]{2,}|\b(?:api[_-]?key|contrase(?:n|ñ)a|password|bearer|secret|token)\s*[:=])",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(150));
        }
        catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
        {
            // Un texto adversarial no debe permitir saltarse la advertencia.
            return true;
        }
    }
}
