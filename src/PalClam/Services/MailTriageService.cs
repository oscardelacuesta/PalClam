// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

/// <summary>Clasificación informativa: una alerta heurística de enlace NO es malware detectado.</summary>
internal static class MailTriageService
{
    internal static bool IsDetection(MailFinding f) =>
        string.Equals(f.Status, "DETECCIÓN", StringComparison.OrdinalIgnoreCase);
    internal static bool IsUnexamined(MailFinding f) =>
        f.Status is "Error" or "Incompleto" or "Cancelado" or "Omitido" or "Límite";
    internal static bool IsLinkNotice(MailFinding f) =>
        string.Equals(f.Status, "Aviso enlace", StringComparison.OrdinalIgnoreCase);
    internal static bool NeedsAttention(MailFinding f) =>
        IsDetection(f) || IsUnexamined(f) || IsLinkNotice(f);
    internal static bool Matches(MailFinding f, string? filter) => filter switch
    {
        "Detecciones" => IsDetection(f),
        "No analizados / errores" => IsUnexamined(f),
        "Avisos de enlaces" => IsLinkNotice(f),
        "Requieren atención" => NeedsAttention(f),
        _ => true
    };
    internal static string Describe(IEnumerable<MailFinding> findings)
    {
        var list = findings.ToArray();
        var detections = list.Count(IsDetection);
        var unexamined = list.Count(IsUnexamined);
        var links = list.Count(IsLinkNotice);
        return $"Detecciones ClamAV: {detections} · No analizados/incidencias: {unexamined} · Avisos heurísticos: {links}. " +
            (detections > 0 ? "Revisa los adjuntos detectados y decide manualmente." :
             unexamined > 0 ? "No interpretes el resultado como completamente limpio." :
             links > 0 ? "Los avisos de enlaces requieren revisión humana; no son detecciones antivirus." :
             "Sin incidencias notificadas en el listado (no es garantía de ausencia de amenazas).");
    }
}
