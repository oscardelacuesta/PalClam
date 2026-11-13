// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Text.RegularExpressions;

namespace PalClam.Services;

internal enum DiskLedState { Idle, Reading, Writing, ReadWrite, Scanning, Threat, LowSpace, Unavailable }

internal sealed class DiskLedOptions
{
    public bool Enabled { get; set; } = true;
    public string DriveRoot { get; set; } = "auto";
    public bool AllPhysicalDisks { get; set; } = true;
    public int IntervalMs { get; set; } = 500;
    public int ActivityThresholdKb { get; set; } = 16;
    public bool ShowAntivirusStates { get; set; } = true;
    public bool WarnLowSpace { get; set; } = true;
    public int MinFreePercent { get; set; } = 5;
    public int MinFreeGb { get; set; } = 5;
    public bool Pulse { get; set; } = true;
    public Dictionary<string, string> Colors { get; set; } = Defaults();

    internal static Dictionary<string, string> Defaults() => new(StringComparer.Ordinal)
    {
        [nameof(DiskLedState.Idle)] = "#7D8798", [nameof(DiskLedState.Reading)] = "#42D77D",
        [nameof(DiskLedState.Writing)] = "#4EA5FF", [nameof(DiskLedState.ReadWrite)] = "#21CDD2",
        [nameof(DiskLedState.Scanning)] = "#AA75FF", [nameof(DiskLedState.Threat)] = "#FF475A",
        [nameof(DiskLedState.LowSpace)] = "#FFB347", [nameof(DiskLedState.Unavailable)] = "#5E6675"
    };

    internal static DiskLedOptions Normalize(DiskLedOptions? value)
    {
        value ??= new();
        var drive = (value.DriveRoot ?? "auto").Trim();
        if (!Regex.IsMatch(drive, @"^[A-Za-z]:\\$")) drive = "auto";
        var colors = Defaults();
        foreach (var key in colors.Keys.ToArray())
            if (value.Colors is not null && value.Colors.TryGetValue(key, out var color) &&
                Regex.IsMatch(color ?? "", "^#[0-9a-fA-F]{6}$")) colors[key] = color!.ToUpperInvariant();
        return new DiskLedOptions
        {
            Enabled = value.Enabled, DriveRoot = drive.ToUpperInvariant() == "AUTO" ? "auto" : drive.ToUpperInvariant(),
            AllPhysicalDisks = value.AllPhysicalDisks,
            IntervalMs = value.IntervalMs is 250 or 500 or 1000 or 2000 ? value.IntervalMs : 500,
            ActivityThresholdKb = Math.Clamp(value.ActivityThresholdKb, 0, 1024 * 1024),
            ShowAntivirusStates = value.ShowAntivirusStates, WarnLowSpace = value.WarnLowSpace,
            MinFreePercent = Math.Clamp(value.MinFreePercent, 0, 50), MinFreeGb = Math.Clamp(value.MinFreeGb, 0, 10000),
            Pulse = value.Pulse, Colors = colors
        };
    }
}

internal sealed record DiskActivitySnapshot(bool IoAvailable, double ReadBytesPerSecond, double WriteBytesPerSecond,
    bool SpaceAvailable, long FreeBytes, long TotalBytes, string DriveRoot, DateTimeOffset CapturedAt, string Diagnostic)
{
    internal double FreePercent => SpaceAvailable && TotalBytes > 0 ? FreeBytes * 100.0 / TotalBytes : 0;
}

internal static class DiskLedPolicy
{
    internal static DiskLedState Select(DiskLedOptions options, DiskActivitySnapshot snapshot, bool scanning, bool threat)
    {
        if (options.ShowAntivirusStates && threat) return DiskLedState.Threat;
        if (options.WarnLowSpace && snapshot.SpaceAvailable && snapshot.TotalBytes > 0 &&
            ((options.MinFreePercent > 0 && snapshot.FreePercent <= options.MinFreePercent) ||
             (options.MinFreeGb > 0 && snapshot.FreeBytes <= options.MinFreeGb * 1024L * 1024L * 1024L)))
            return DiskLedState.LowSpace;
        if (options.ShowAntivirusStates && scanning) return DiskLedState.Scanning;
        if (!snapshot.IoAvailable) return DiskLedState.Unavailable;
        var threshold = options.ActivityThresholdKb * 1024.0;
        var read = double.IsFinite(snapshot.ReadBytesPerSecond) && snapshot.ReadBytesPerSecond > threshold;
        var write = double.IsFinite(snapshot.WriteBytesPerSecond) && snapshot.WriteBytesPerSecond > threshold;
        return read && write ? DiskLedState.ReadWrite : read ? DiskLedState.Reading : write ? DiskLedState.Writing : DiskLedState.Idle;
    }

    internal static string Label(DiskLedState state) => state switch
    {
        DiskLedState.Reading => "Lectura de disco", DiskLedState.Writing => "Escritura de disco",
        DiskLedState.ReadWrite => "Lectura y escritura", DiskLedState.Scanning => "Análisis antivirus en curso",
        DiskLedState.Threat => "Alerta pendiente de revisar", DiskLedState.LowSpace => "Poco espacio libre",
        DiskLedState.Unavailable => "Actividad de disco no disponible", _ => "Disco en reposo"
    };
}
