// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

internal sealed class RansomwareOptions
{
    public bool StartWithApp { get; set; }
    public List<string> Folders { get; set; } = new();
    public List<string> ExcludedFolders { get; set; } = new();
    public int WindowSeconds { get; set; } = 15;
    public int MinFiles { get; set; } = 20;
    public int MinSignals { get; set; } = 5;
    public bool SampleEntropy { get; set; } = true;
    public bool WarnMassActivity { get; set; } = true;
    internal static RansomwareOptions Normalize(RansomwareOptions? value)
    {
        value ??= new();
        return new()
        {
            StartWithApp = value.StartWithApp,
            Folders = Clean(value.Folders), ExcludedFolders = Clean(value.ExcludedFolders),
            WindowSeconds = Math.Clamp(value.WindowSeconds, 5, 60), MinFiles = Math.Clamp(value.MinFiles, 5, 1000),
            MinSignals = Math.Clamp(value.MinSignals, 2, Math.Clamp(value.MinFiles, 5, 1000)),
            SampleEntropy = value.SampleEntropy, WarnMassActivity = value.WarnMassActivity
        };
    }
    private static List<string> Clean(List<string>? values) => (values ?? new()).Where(v => !string.IsNullOrWhiteSpace(v))
        .Select(v => v.Trim().Trim('"')).Distinct(StringComparer.OrdinalIgnoreCase).Take(32).ToList();
}

internal enum FileActivityKind { Changed, Created, Renamed, Deleted }
internal sealed record FileActivity(DateTimeOffset When, string Path, string OldPath, FileActivityKind Kind, bool HighEntropy);
internal sealed record RansomwareAlert(DateTimeOffset When, string Kind, int Files, int Renamed, int HighEntropy,
    int Deleted, string ExamplePath, int WindowSeconds)
{
    public string DisplayTime => When.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
    public string Status => Kind == "suspected" ? "POSIBLE RANSOMWARE" : "Actividad masiva";
    public string Detail => Kind == "suspected"
        ? $"Posible ransomware (no confirmado). Archivos: {Files} · Renombrados: {Renamed} · Muestras de alta aleatoriedad: {HighEntropy} · Eliminados: {Deleted} · Ventana: {WindowSeconds} s."
        : $"Actividad masiva (no confirma ransomware). Archivos: {Files} · Renombrados: {Renamed} · Muestras de alta aleatoriedad: {HighEntropy} · Eliminados: {Deleted} · Ventana: {WindowSeconds} s.";
}

// Motor puro: correlaciona archivos distintos. Los eventos duplicados de un archivo no aumentan el contador.
internal sealed class RansomwarePolicy
{
    private sealed record Evidence(DateTimeOffset When, bool Renamed, bool SuspiciousName, bool Entropy, bool Deleted);
    private readonly RansomwareOptions _options;
    private readonly Dictionary<string, Evidence> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> _lastAlerts = new();
    private static readonly HashSet<string> EncryptedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".locked", ".encrypted", ".crypt", ".crypto", ".enc", ".lockbit", ".ryk", ".ryuk", ".wncry", ".wncryt" };
    internal RansomwarePolicy(RansomwareOptions options) => _options = RansomwareOptions.Normalize(options);
    internal int TrackedCount => _files.Count;
    internal static bool SuspiciousName(string path, string oldPath)
    {
        var extension = System.IO.Path.GetExtension(path);
        return EncryptedExtensions.Contains(extension) ||
            (!string.IsNullOrEmpty(oldPath) && !extension.Equals(System.IO.Path.GetExtension(oldPath), StringComparison.OrdinalIgnoreCase) &&
             System.IO.Path.GetFileName(path).StartsWith(System.IO.Path.GetFileName(oldPath) + ".", StringComparison.OrdinalIgnoreCase));
    }
    internal RansomwareAlert? Observe(FileActivity activity)
    {
        var cutoff = activity.When.AddSeconds(-_options.WindowSeconds);
        foreach (var key in _files.Where(p => p.Value.When < cutoff).Select(p => p.Key).ToArray()) _files.Remove(key);
        _files.TryGetValue(activity.Path, out var previous);
        if (activity.Kind == FileActivityKind.Renamed && !string.IsNullOrEmpty(activity.OldPath) && _files.Remove(activity.OldPath, out var old))
            previous ??= old;
        // Presupuesto de memoria fijo; la cola exterior informa si se pierden eventos.
        if (_files.Count >= 8192 && previous is null) _files.Remove(_files.MinBy(p => p.Value.When).Key);
        _files[activity.Path] = new(activity.When, previous?.Renamed == true || activity.Kind == FileActivityKind.Renamed,
            previous?.SuspiciousName == true || SuspiciousName(activity.Path, activity.OldPath),
            previous?.Entropy == true || activity.HighEntropy, previous?.Deleted == true || activity.Kind == FileActivityKind.Deleted);
        var values = _files.Values; var count = _files.Count;
        var renamed = values.Count(e => e.Renamed); var suspicious = values.Count(e => e.Renamed && e.SuspiciousName);
        var entropy = values.Count(e => e.Entropy); var deleted = values.Count(e => e.Deleted);
        string? kind = count >= _options.MinFiles && (suspicious >= _options.MinSignals || entropy >= _options.MinSignals)
            ? "suspected" : _options.WarnMassActivity && deleted >= _options.MinFiles ? "deletion"
            : _options.WarnMassActivity && (renamed >= _options.MinFiles || count >= _options.MinFiles * 2) ? "mass" : null;
        if (kind is null || _lastAlerts.TryGetValue(kind, out var last) && activity.When - last < TimeSpan.FromSeconds(60)) return null;
        _lastAlerts[kind] = activity.When;
        return new(activity.When, kind, count, renamed, entropy, deleted, activity.Path, _options.WindowSeconds);
    }
    internal static double Entropy(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0) return 0;
        Span<int> histogram = stackalloc int[256]; histogram.Clear();
        foreach (var b in bytes) histogram[b]++;
        double entropy = 0;
        foreach (var count in histogram) if (count > 0) { var p = (double)count / bytes.Length; entropy -= p * Math.Log2(p); }
        return entropy;
    }
    internal static bool PlainTextExtension(string path) => System.IO.Path.GetExtension(path).ToLowerInvariant() is
        ".txt" or ".csv" or ".log" or ".xml" or ".json" or ".yaml" or ".yml" or ".ini" or ".sql" or ".cs" or ".vb" or ".html" or ".htm" or ".md";
}
