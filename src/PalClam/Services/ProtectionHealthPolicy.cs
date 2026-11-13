// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System;
using System.Collections.Generic;
using System.Linq;

namespace PalClam.Services;

internal enum HealthLevel { Ready, Attention, Inactive, Unknown }

internal sealed record HealthFinding(string Component, HealthLevel Level, string Detail)
{
    public string DisplayLevel => Level switch
    {
        HealthLevel.Ready => "OPERATIVO",
        HealthLevel.Attention => "REVISAR",
        HealthLevel.Inactive => "NO ACTIVADO",
        _ => "NO VERIFICADO"
    };
}

internal sealed record HealthInputs(
    bool ScannerPresent, SignatureHealthState Signatures,
    bool PersonalGuardRunning, bool PersonalGuardDegraded,
    bool DownloadGuardEnabled, bool DownloadFolderWatched,
    bool UsbWatchEnabled, bool UsbMonitorRunning,
    bool MachineRegistered, MachineServiceSnapshot? Machine,
    bool StartupConfigured, bool? ScheduledTaskConfigured,
    DateTimeOffset NowUtc);

/// <summary>Diagnóstico sin efectos: ni inicia servicios ni promete prevención.</summary>
internal static class ProtectionHealthPolicy
{
    public static IReadOnlyList<HealthFinding> Evaluate(HealthInputs i)
    {
        var results = new List<HealthFinding>
        {
            new("Motor ClamAV", i.ScannerPresent ? HealthLevel.Ready : HealthLevel.Attention,
                i.ScannerPresent ? "Ejecutable localizado. No demuestra que todos los análisis funcionen." :
                "Falta clamscan.exe válido; no se puede analizar hasta instalarlo."),
            new("Firmas locales", i.Signatures switch
            {
                SignatureHealthState.Available => HealthLevel.Ready,
                SignatureHealthState.Missing or SignatureHealthState.UpdateFailed or
                SignatureHealthState.Attention => HealthLevel.Attention,
                _ => HealthLevel.Unknown
            }, i.Signatures switch
            {
                SignatureHealthState.Available => "Bases presentes y consulta reciente registrada; no garantiza estar a la última.",
                SignatureHealthState.Missing => "Faltan bases main/daily. No declarar protección activa.",
                SignatureHealthState.UpdateFailed => "La última actualización falló; comprobar FreshClam.",
                _ => "Comprobar la versión y antigüedad de las firmas con FreshClam."
            }),
            new("Guard personal", !i.PersonalGuardRunning ? HealthLevel.Inactive :
                i.PersonalGuardDegraded ? HealthLevel.Attention : HealthLevel.Ready,
                !i.PersonalGuardRunning ? "No vigila carpetas en esta sesión." :
                i.PersonalGuardDegraded ? "Se han perdido eventos: realizar análisis manual." :
                "Vigilancia reactiva activa, sin bloqueo preventivo."),
            new("Download Guard", !i.DownloadGuardEnabled ? HealthLevel.Inactive :
                i.DownloadFolderWatched ? HealthLevel.Ready : HealthLevel.Attention,
                !i.DownloadGuardEnabled ? "Vigilancia de Descargas desactivada voluntariamente." :
                i.DownloadFolderWatched ? "Descargas incluida en Guard activo; no bloquea archivos." :
                "Descargas no está bajo un observador activo; iniciar o reiniciar Guard."),
            new("USB Watch", !i.UsbWatchEnabled ? HealthLevel.Inactive :
                i.UsbMonitorRunning ? HealthLevel.Ready : HealthLevel.Attention,
                !i.UsbWatchEnabled ? "Detección de unidades extraíbles desactivada." :
                i.UsbMonitorRunning ? "Consulta unidades extraíbles clasificadas por Windows; no intercepta ejecución." :
                "El monitor de unidades no está en funcionamiento."),
            new("Servicio de equipo", !i.MachineRegistered ? HealthLevel.Inactive :
                i.Machine is null ? HealthLevel.Unknown :
                i.Machine.IsHealthy(i.NowUtc) ? HealthLevel.Ready : HealthLevel.Attention,
                !i.MachineRegistered ? "Servicio experimental no instalado; es opcional." :
                i.Machine is null ? "Registrado, sin estado legible; no declarar vigilancia activa." :
                !i.Machine.IsFresh(i.NowUtc) ? "Latido antiguo; revisar services.msc." :
                i.Machine.IsHealthy(i.NowUtc) ? "Vigilancia reactiva activa, sin incidencias notificadas." :
                "Servicio degradado, detenido o con eventos omitidos; revisar diagnóstico."),
            new("Inicio con Windows", i.StartupConfigured ? HealthLevel.Ready : HealthLevel.Inactive,
                i.StartupConfigured ? "Inicio de sesión configurado; no equivale a servicio." :
                "Inicio automático desactivado; opción voluntaria."),
            new("Análisis programados", i.ScheduledTaskConfigured switch
            {
                true => HealthLevel.Ready, false => HealthLevel.Inactive, null => HealthLevel.Unknown
            }, i.ScheduledTaskConfigured switch
            {
                true => "Tarea registrada; su existencia no acredita que el último análisis se completara.",
                false => "No hay tarea programada; es opcional.",
                null => "Estado de tareas no confirmado; revisa Análisis programados."
            })
        };
        return results;
    }

    public static string Summary(IReadOnlyCollection<HealthFinding> items)
    {
        if (items.Any(x => x.Level == HealthLevel.Attention))
            return "ATENCIÓN · Hay componentes que necesitan revisión. No implica protección preventiva.";
        if (items.Any(x => x.Level == HealthLevel.Unknown))
            return "PARCIAL · Algunas comprobaciones no se han podido confirmar.";
        return "COMPROBADO · Solo los componentes indicados están activos. PalClam no sustituye Defender.";
    }
}
