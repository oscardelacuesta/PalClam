// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Globalization;

namespace PalClam.Services;

internal enum SignatureHealthState { Missing, UpdateFailed, Unknown, Attention, Available }
internal sealed record SignatureHealthResult(SignatureHealthState State, string Title, string Detail, bool NeedsAttention);

/// <summary>
/// Salud OPERATIVA de la base local, sin afirmar que sus firmas son las últimas del servidor.
/// La fecha de modificación del fichero es solo un indicio: copiar una base puede cambiarla.
/// </summary>
internal static class SignatureHealthService
{
    internal static SignatureHealthResult Evaluate(bool hasMainAndDaily, DateTimeOffset? dailyWriteUtc,
        DateTimeOffset lastSuccessfulCheckUtc, DateTimeOffset lastAttemptUtc, string? lastFailure,
        DateTimeOffset nowUtc)
    {
        if (!hasMainAndDaily)
            return new(SignatureHealthState.Missing, "Firmas incompletas",
                "Faltan las bases main o daily. No es posible iniciar análisis fiables con esta configuración.", true);

        if (!string.IsNullOrWhiteSpace(lastFailure) && lastAttemptUtc > lastSuccessfulCheckUtc)
            return new(SignatureHealthState.UpdateFailed, "Última actualización fallida",
                "Las firmas locales siguen disponibles, pero la última consulta a FreshClam falló: " +
                lastFailure.Trim() + ". Consulta el registro y vuelve a intentarlo cuando tengas conexión.", true);

        if (dailyWriteUtc is null || dailyWriteUtc > nowUtc.AddMinutes(10))
            return new(SignatureHealthState.Unknown, "Fecha de firmas no verificable",
                "Hay bases locales, pero no se puede estimar su antigüedad. Comprueba las actualizaciones con FreshClam.", true);

        var elapsed = nowUtc - dailyWriteUtc.Value;
        if (elapsed > TimeSpan.FromDays(7))
            return new(SignatureHealthState.Attention, "Revisar antigüedad de firmas",
                $"La base daily tiene una fecha de fichero de hace {Math.Floor(elapsed.TotalDays).ToString(CultureInfo.InvariantCulture)} días. " +
                "Puede estar desactualizada; la fecha local no demuestra qué versión hay publicada. Ejecuta FreshClam.", true);
        if (lastSuccessfulCheckUtc == DateTimeOffset.MinValue)
            return new(SignatureHealthState.Unknown, "Firmas locales, sin comprobación registrada",
                "Las bases main y daily están presentes. Aún no consta una comprobación correcta con FreshClam. " +
                "La fecha del archivo no acredita que estén actualizadas.", true);
        return new(SignatureHealthState.Available, "Firmas locales disponibles",
            "Las bases main y daily están presentes. Última consulta correcta: " +
            lastSuccessfulCheckUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm") +
            ". Esto no garantiza tener la última versión publicada.", false);
    }

    internal static bool AutoCheckDue(DateTimeOffset lastSuccessfulCheckUtc, DateTimeOffset lastAttemptUtc,
        int intervalHours, DateTimeOffset nowUtc)
    {
        var latest = lastSuccessfulCheckUtc >= lastAttemptUtc ? lastSuccessfulCheckUtc : lastAttemptUtc;
        return latest == DateTimeOffset.MinValue || nowUtc - latest >= TimeSpan.FromHours(Math.Clamp(intervalHours, 1, 48));
    }

    internal static DateTimeOffset? DailyFileTimeUtc(string folder)
    {
        try
        {
            var files = new[] { "daily.cld", "daily.cvd" }
                .Select(x => Path.Combine(folder, x)).Where(File.Exists).ToArray();
            if (files.Length == 0) return null;
            return new DateTimeOffset(files.Select(file => File.GetLastWriteTimeUtc(file)).Max(), TimeSpan.Zero);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }
}
