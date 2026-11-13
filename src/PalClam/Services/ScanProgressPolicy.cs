// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

/// <summary>
/// Un resultado por archivo NO es una confirmación de cobertura total: ClamAV puede
/// rechazar rutas, y el recuento previo puede cambiar. El 100% se reserva para la
/// finalización confirmada; nunca para la mera estimación del explorador.
/// </summary>
internal enum ScanProgressPhase { Running, Pausing, Paused, Stopping, Completed, Incomplete }

internal sealed record ScanProgressView(
    double? Percent, string Label, string Coverage, string Guidance, string Eta)
{
    public bool IsIndeterminate => Percent is null &&
        PhaseIsActive && Label != "Pausado" && Label != "Deteniendo…";
    internal bool PhaseIsActive { get; init; }
}

internal static class ScanProgressPolicy
{
    public static ScanProgressView Evaluate(ScanProgressPhase phase, long results,
        long estimate, bool truncated, long inaccessible, bool estimatePending,
        double elapsedSeconds, double? stableRate)
    {
        results = Math.Max(0, results);
        var validEstimate = estimate > 0 && !truncated && inaccessible == 0 && results <= estimate;
        // La enumeración previa es una aproximación; NO es el total de archivos
        // confirmados por ClamAV ni incluye necesariamente el interior de archivos comprimidos.
        double? percent = phase == ScanProgressPhase.Completed ? 100 :
            validEstimate ? Math.Min(99.0, 100.0 * results / estimate) : null;
        var coverage = validEstimate
            ? $"{results:N0} resultados notificados / ≈ {estimate:N0} archivos enumerados"
            : truncated ? $"{results:N0} resultados · recuento previo truncado; sin porcentaje fiable"
            : inaccessible > 0 ? $"{results:N0} resultados · {inaccessible:N0} accesos sin enumerar; porcentaje oculto"
            : estimate > 0 && results > estimate
                ? $"{results:N0} resultados · se superó el recuento previo; porcentaje oculto"
                : estimatePending ? $"{results:N0} resultados · calculando referencia de archivos…"
                : $"{results:N0} resultados de archivo · total no estimable";

        var label = phase switch
        {
            ScanProgressPhase.Paused => "Pausado",
            ScanProgressPhase.Pausing => "Pausando…",
            ScanProgressPhase.Stopping => "Deteniendo…",
            ScanProgressPhase.Completed => "Finalizado",
            ScanProgressPhase.Incomplete => "Incompleto",
            _ => percent is double p ? $"≈ {p:0.0}%" : "En curso"
        };
        var guidance = phase switch
        {
            ScanProgressPhase.Paused => "Pausa aplicada entre lotes. Pulsa Reanudar para continuar la sesión.",
            ScanProgressPhase.Pausing => "Pausa solicitada. ClamAV debe terminar el lote actual antes de detener el avance.",
            ScanProgressPhase.Stopping => "Detención solicitada. Esperando confirmación del motor; la cobertura aún es parcial.",
            ScanProgressPhase.Completed => "Recorrido terminado. Consulta detecciones, errores y el informe para interpretar el resultado.",
            ScanProgressPhase.Incomplete => "Análisis incompleto: no se ha verificado toda la selección. Revisa los errores o vuelve a analizar.",
            _ => validEstimate
                ? "Porcentaje orientativo. Se confirma el final únicamente cuando ClamAV termina."
                : "Se muestran resultados confirmados; no se inventa un porcentaje sin referencia fiable."
        };
        var eta = "";
        // No prometer tiempo restante antes de que existan suficientes muestras,
        // durante una pausa/detención o con recuentos de referencia inconsistentes.
        if (phase == ScanProgressPhase.Running && validEstimate &&
            elapsedSeconds >= 12 && results >= 25 && results < estimate &&
            stableRate is double rate && double.IsFinite(rate) && rate >= 0.25)
        {
            var seconds = (estimate - results) / rate;
            if (seconds >= 1 && seconds <= TimeSpan.FromDays(7).TotalSeconds)
            {
                var remaining = TimeSpan.FromSeconds(seconds);
                eta = remaining.TotalDays >= 1
                    ? $"restante ≈ {(int)remaining.TotalDays}d {remaining.Hours:00}h"
                    : $"restante ≈ {(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
            }
        }
        return new ScanProgressView(percent, label, coverage, guidance, eta)
        {
            PhaseIsActive = phase is ScanProgressPhase.Running or ScanProgressPhase.Pausing
        };
    }
}

/// <summary>
/// Tasa suavizada de resultados, no de bytes físicos. Descarta muestras antiguas
/// al pausar/reanudar y oculta la ETA si deja de llegar actividad.
/// </summary>
internal sealed class ScanResultRateTracker
{
    private double _at;
    private long _results;
    private double _activeSeconds;
    private double? _rate;

    public void Reset() { _at = 0; _results = 0; _activeSeconds = 0; _rate = null; }

    public double? Observe(long results, double elapsedSeconds, bool active)
    {
        if (!active || !double.IsFinite(elapsedSeconds))
        {
            _at = elapsedSeconds;
            _results = results;
            _activeSeconds = 0;
            _rate = null;
            return null;
        }
        var dt = elapsedSeconds - _at;
        if (dt < 1.5) return _activeSeconds >= 10 ? _rate : null;
        var change = Math.Max(0, results - _results);
        _at = elapsedSeconds;
        _results = results;
        _activeSeconds += dt;
        // Sin respuestas, un ETA anterior sería una predicción engañosa.
        if (change == 0) { _rate = null; return null; }
        var instant = change / dt;
        _rate = _rate is double previous ? previous * 0.70 + instant * 0.30 : instant;
        return _activeSeconds >= 10 ? _rate : null;
    }
}
