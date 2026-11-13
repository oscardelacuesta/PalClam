// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace PalClam.Services;

// Presentación únicamente: aquí no hay escaneo, cuarentena ni ejecución de comandos.
internal enum SecurityNoticeLevel { Threat, Warning, Error, Information }

internal sealed record SecurityNotice(
    DateTimeOffset When, SecurityNoticeLevel Level, string Source,
    string Title, string Detail, string FilePath)
{
    public string DisplayTime => When.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
    public string DisplayLevel => Level switch
    {
        SecurityNoticeLevel.Threat => "DETECCIÓN",
        SecurityNoticeLevel.Warning => "Advertencia",
        SecurityNoticeLevel.Error => "Error",
        _ => "Información"
    };
    public string DisplaySource => Source switch
    {
        "machine" => "Servicio Windows",
        "device" => "USB Watch",
        "ransomware" => "Anti-ransomware",
        _ => "Guard personal"
    };
    public string DisplayDetail => string.IsNullOrWhiteSpace(FilePath)
        ? Detail : FilePath + " · " + Detail;
}

internal sealed record SecurityNoticeFeedResult(
    IReadOnlyList<SecurityNotice> Items, int UnreadableFiles, int InvalidEntries, bool Limited);

/// <summary>
/// Lector de registros existentes con presupuesto fijo de I/O. La WPF puede leer
/// el historial del servicio sin poder enviarle comandos ni escribir en sus datos.
/// </summary>
internal static class SecurityNoticeFeed
{
    private const int MaxBytesPerFile = 1_200_000;
    private const int MaxEntryLength = 8_192;
    private const int MaxItems = 600;

    public static SecurityNoticeFeedResult ReadRecent(string guardFolder, string machineFolder,
        DateTimeOffset now, int requestedLimit = 400)
    {
        var all = new List<SecurityNotice>();
        var unreadable = 0;
        var malformed = 0;
        var limited = false;
        var filenames = new[]
        {
            (System.IO.Path.Combine(guardFolder, "incidencias-anterior.jsonl"), "guard"),
            (System.IO.Path.Combine(guardFolder, "incidencias.jsonl"), "guard"),
            (System.IO.Path.Combine(machineFolder, "eventos-anterior.log"), "machine"),
            (System.IO.Path.Combine(machineFolder, "eventos.log"), "machine"),
            (System.IO.Path.Combine(DeviceWatchAudit.Folder, "eventos-anterior.jsonl"), "device"),
            (System.IO.Path.Combine(DeviceWatchAudit.Folder, "eventos.jsonl"), "device"),
            (System.IO.Path.Combine(RansomwareAudit.Folder, "eventos-anterior.jsonl"), "ransomware"),
            (System.IO.Path.Combine(RansomwareAudit.Folder, "eventos.jsonl"), "ransomware")
        };
        foreach (var (path, source) in filenames)
        {
            if (!File.Exists(path)) continue;
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                // Leer únicamente la cola de cada registro, sin cargar grandes archivos.
                var skipFirstPartial = stream.Length > MaxBytesPerFile;
                if (skipFirstPartial)
                {
                    stream.Seek(-MaxBytesPerFile, SeekOrigin.End);
                    limited = true;
                }
                using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, leaveOpen: false);
                if (skipFirstPartial) reader.ReadLine();
                while (reader.ReadLine() is { } line)
                {
                    if (line.Length > MaxEntryLength) { malformed++; continue; }
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var notice = source switch
                    {
                        "machine" => ParseMachine(line),
                        "device" => ParseDevice(line),
                        "ransomware" => ParseRansomware(line),
                        _ => ParseGuard(line)
                    };
                    if (notice is null) { malformed++; continue; }
                    if (notice.When > now.AddMinutes(5) || notice.When < now.AddYears(-10))
                    {
                        malformed++;
                        continue;
                    }
                    all.Add(notice);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
                System.Security.SecurityException or ArgumentException or NotSupportedException)
            {
                unreadable++;
            }
        }
        var limit = Math.Clamp(requestedLimit, 1, MaxItems);
        if (all.Count > limit) limited = true;
        return new SecurityNoticeFeedResult(all.OrderByDescending(n => n.When)
            .Take(limit).ToArray(), unreadable, malformed, limited);
    }

    internal static SecurityNotice? ParseGuard(string line)
    {
        try
        {
            using var json = JsonDocument.Parse(line);
            var obj = json.RootElement;
            if (obj.ValueKind != JsonValueKind.Object ||
                !obj.TryGetProperty("When", out var date) ||
                !date.TryGetDateTimeOffset(out var when) ||
                !obj.TryGetProperty("Outcome", out var raw)) return null;
            var code = raw.ValueKind switch
            {
                JsonValueKind.Number when raw.TryGetInt32(out var n) => n,
                JsonValueKind.String => raw.GetString()?.ToUpperInvariant() switch
                {
                    "CLEAN" => 0, "THREAT" => 1, "SKIPPED" => 2,
                    "ERROR" => 3, "WARNING" => 4, _ => -1
                },
                _ => -1
            };
            // Códigos estables del JSON de GuardOutcome: 0 Clean, 1 Threat,
            // 2 Skipped, 3 Error, 4 Warning. Evitar dependencias WPF en pruebas.
            if (code == 0) return null; // No se persisten registros limpios.
            if (code is < 1 or > 4) return null;
            var level = code switch
            {
                1 => SecurityNoticeLevel.Threat,
                3 => SecurityNoticeLevel.Error,
                _ => SecurityNoticeLevel.Warning
            };
            var file = Clip(GetString(obj, "FilePath"), 800);
            var detail = Clip(GetString(obj, "Detail"), 1200);
            return new SecurityNotice(when, level, "guard", "PalClam Guard", detail, file);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException)
        {
            return null;
        }
    }

    internal static SecurityNotice? ParseRansomware(string line)
    {
        try
        {
            using var json = JsonDocument.Parse(line); var obj = json.RootElement;
            if (!obj.TryGetProperty("When", out var time) || !time.TryGetDateTimeOffset(out var when)) return null;
            var kind = GetString(obj, "Kind");
            if (kind is not ("suspected" or "deletion" or "mass" or "health")) return null;
            return new(when, kind == "health" ? SecurityNoticeLevel.Error : SecurityNoticeLevel.Warning,
                "ransomware", kind == "suspected" ? "Posible ransomware (no confirmado)" : "Anti-ransomware", Clip(GetString(obj, "Detail"), 1200), Clip(GetString(obj, "ExamplePath"), 800));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException) { return null; }
    }

    internal static SecurityNotice? ParseDevice(string line)
    {
        try
        {
            using var json = JsonDocument.Parse(line);
            var obj = json.RootElement;
            if (obj.ValueKind != JsonValueKind.Object ||
                !obj.TryGetProperty("When", out var time) ||
                !time.TryGetDateTimeOffset(out var when)) return null;
            var level = GetString(obj, "Level").ToUpperInvariant() switch
            {
                "WARNING" => SecurityNoticeLevel.Warning,
                "ERROR" => SecurityNoticeLevel.Error,
                _ => SecurityNoticeLevel.Information
            };
            return new SecurityNotice(when, level, "device", "USB Watch",
                Clip(GetString(obj, "Detail"), 1200), Clip(GetString(obj, "Root"), 800));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        { return null; }
    }

    internal static SecurityNotice? ParseMachine(string line)
    {
        // MachineGuardReport.Append: fecha ISO 8601 + espacio + [NIVEL] + detalle.
        var marker = line.IndexOf(" [", StringComparison.Ordinal);
        if (marker <= 0 || marker > 45) return null;
        if (!DateTimeOffset.TryParseExact(line[..marker], "O", CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var when)) return null;
        var end = line.IndexOf(']', marker + 2);
        if (end < marker + 3 || end > marker + 20) return null;
        var level = line.Substring(marker + 2, end - marker - 2).ToUpperInvariant();
        var severity = level switch
        {
            "ALERTA" => SecurityNoticeLevel.Threat,
            "WARNING" => SecurityNoticeLevel.Warning,
            "ERROR" => SecurityNoticeLevel.Error,
            "INFO" => SecurityNoticeLevel.Information,
            _ => (SecurityNoticeLevel?)null
        };
        if (severity is null) return null;
        var detail = Clip(line[(end + 1)..].Trim(), 1200);
        return new SecurityNotice(when, severity.Value, "machine", "Machine Guard", detail, "");
    }

    public static IReadOnlyList<SecurityNotice> Filter(IEnumerable<SecurityNotice> items,
        string? severity, string? source, string? query, bool onlyUnreviewed,
        DateTimeOffset reviewedUntil)
    {
        return items.Where(item =>
            (string.IsNullOrWhiteSpace(severity) || severity == "Todas" ||
             item.DisplayLevel == severity) &&
            (string.IsNullOrWhiteSpace(source) || source == "Todos" ||
             item.DisplaySource == source) &&
            (!onlyUnreviewed || item.When > reviewedUntil) &&
            (string.IsNullOrWhiteSpace(query) ||
             item.DisplayDetail.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase) ||
             item.Title.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    private static string GetString(JsonElement obj, string key) =>
        obj.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? "" : "";

    private static string Clip(string value, int max) =>
        value.Length <= max ? value.Replace('\r', ' ').Replace('\n', ' ') :
            value[..max].Replace('\r', ' ').Replace('\n', ' ') + "…";
}

/// <summary>Fecha de revisión del usuario, separada de los registros de seguridad.</summary>
internal static class SecurityNoticeReadMarker
{
    public static DateTimeOffset Read(string file)
    {
        try
        {
            if (!File.Exists(file) || new FileInfo(file).Length > 128) return DateTimeOffset.MinValue;
            return DateTimeOffset.TryParseExact(File.ReadAllText(file).Trim(), "O",
                CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value)
                ? value : DateTimeOffset.MinValue;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            ArgumentException or System.Security.SecurityException) { return DateTimeOffset.MinValue; }
    }

    public static bool TryMark(string file, DateTimeOffset now)
    {
        try
        {
            var directory = System.IO.Path.GetDirectoryName(file);
            if (string.IsNullOrWhiteSpace(directory)) return false;
            Directory.CreateDirectory(directory);
            File.WriteAllText(file, now.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            ArgumentException or System.Security.SecurityException) { return false; }
    }
}
