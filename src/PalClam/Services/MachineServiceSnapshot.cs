// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System;
using System.IO;
using System.Text.Json;

namespace PalClam.Services;

/// <summary>
/// Contrato de lectura del estado emitido por el servicio: comunicación
/// unidireccional, sin ejecutar comandos ni permitir escrituras desde WPF.
/// Compatible con los estados JSON emitidos por Preview 15.
/// </summary>
internal sealed record MachineServiceSnapshot(
    string Mode, DateTimeOffset Utc, string Detail,
    long Scanned, long Detections, long Skipped, long Errors, long LostEvents,
    long Pending, long Reconciliations, string Signatures)
{
    public bool IsFresh(DateTimeOffset now) =>
        Utc <= now.AddMinutes(1) && now - Utc <= TimeSpan.FromSeconds(35);

    public bool IsHealthy(DateTimeOffset now) => IsFresh(now) &&
        string.Equals(Mode, "Vigilando", StringComparison.OrdinalIgnoreCase) &&
        LostEvents == 0 && Skipped == 0 && Errors == 0 && Pending == 0;

    public static MachineServiceSnapshot? TryRead(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            if (stream.Length > 64_000) return null;
            using var json = JsonDocument.Parse(stream);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            var timestamp = root.GetProperty("Utc").GetDateTimeOffset();
            var mode = GetString(root, "Mode", "Desconocido");
            return new MachineServiceSnapshot(
                mode, timestamp, GetString(root, "Detail", ""),
                GetCount(root, "Scanned"), GetCount(root, "Detections"),
                GetCount(root, "Skipped"), GetCount(root, "Errors"),
                GetCount(root, "LostEvents"), GetCount(root, "Pending"),
                GetCount(root, "Reconciliations"), GetString(root, "Signatures", "No indicado"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            JsonException or InvalidOperationException or FormatException or ArgumentException or
            KeyNotFoundException or OverflowException)
        {
            return null;
        }
    }

    private static long GetCount(JsonElement root, string name)
    {
        // Missing optional counters from Preview 15 remain compatible.
        // Existing but malformed/negative values invalidate the whole report.
        if (!root.TryGetProperty(name, out var value)) return 0;
        if (value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt64(out var result) || result < 0)
            throw new JsonException("Contador de servicio inválido: " + name);
        return result;
    }

    private static string GetString(JsonElement root, string name, string fallback) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? (value.GetString() ?? fallback) : fallback;
}
