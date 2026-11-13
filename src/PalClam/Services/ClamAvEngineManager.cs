// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PalClam.Services;

/// <summary>
/// Instala SOLO un motor ClamAV administrado por PalClam, nunca modifica uno ajeno.
/// Consulta la última release estable del repositorio oficial Cisco-Talos/clamav.
/// Verifica SHA-256 del asset publicado por GitHub, rechaza rutas no oficiales,
/// extrae en staging y activa con cambio atómico de puntero tras comprobar --version.
/// </summary>
internal sealed record ClamEngineRelease(Version Version, string Url, string Sha256, long SizeBytes);

internal static class ClamAvEngineManager
{
    private const long MaximumArchiveSize = 700L * 1024 * 1024;
    private const long MaximumExtractedSize = 2L * 1024 * 1024 * 1024;
    private const string OfficialApi = "https://api.github.com/repos/Cisco-Talos/clamav/releases/latest";
    private const string PinnedVersion = "1.5.4";
    private static string PinnedSha256 => AppPaths.EngineSha256;
    private static readonly HttpClient Http = BuildClient();

    public static string DataRoot => Path.Combine(PalClam.Services.AppPaths.Root, AppPaths.RuntimeId == "win-x64" ? "Engine" : "Engine-win-x86");
    private static string ActivePointer => Path.Combine(DataRoot, "active.json");
    private static string VersionFolder(string version) => Path.Combine(DataRoot, "versions", version);

    private static HttpClient BuildClient()
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true, AutomaticDecompression = DecompressionMethods.None });
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PalClam/1.2.2 (independent ClamAV client)");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.Timeout = TimeSpan.FromMinutes(40);
        return client;
    }

    private sealed class ActiveEngine
    {
        public string Version { get; set; } = "";
    }

    private static bool IsSafeVersion(string value) =>
        Regex.IsMatch(value, @"^\d+\.\d+\.\d+$", RegexOptions.CultureInvariant);

    public static string ActiveScanner
    {
        get
        {
            try
            {
                if (!File.Exists(ActivePointer)) return "";
                var version = JsonSerializer.Deserialize<ActiveEngine>(File.ReadAllText(ActivePointer))?.Version ?? "";
                if (!IsSafeVersion(version)) return "";
                var path = Path.Combine(VersionFolder(version), "clamscan.exe");
                var updater = Path.Combine(VersionFolder(version), "freshclam.exe");
                return DistributionPolicy.MatchesExecutable(path, AppPaths.RuntimeId) && DistributionPolicy.MatchesExecutable(updater, AppPaths.RuntimeId) ? path : "";
            }
            catch (Exception) { return ""; }
        }
    }

    public static Version? InstalledVersion
    {
        get
        {
            var scanner = ActiveScanner;
            return !string.IsNullOrEmpty(scanner) &&
                   Version.TryParse(new DirectoryInfo(Path.GetDirectoryName(scanner)!).Name, out var version)
                ? version : null;
        }
    }

    public static async Task<ClamEngineRelease?> CheckLatestAsync(CancellationToken token)
    {
        using var response = await Http.GetAsync(OfficialApi, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > 262144)
            throw new InvalidDataException("Respuesta de versiones demasiado grande.");
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var buffer = new MemoryStream();
        var block = new byte[8192];
        int length;
        while ((length = await stream.ReadAsync(block, token)) > 0)
        {
            if (buffer.Length + length > 262144) throw new InvalidDataException("Metadatos de GitHub demasiado grandes.");
            buffer.Write(block, 0, length);
        }
        using var json = JsonDocument.Parse(buffer.ToArray());
        var release = json.RootElement;
        if (release.GetProperty("draft").GetBoolean() || release.GetProperty("prerelease").GetBoolean())
            throw new InvalidDataException("La publicación indicada no es estable.");
        var tag = release.GetProperty("tag_name").GetString() ?? "";
        if (!tag.StartsWith("clamav-", StringComparison.Ordinal))
            throw new InvalidDataException("Etiqueta de versión oficial inesperada.");
        var releaseText = tag[7..];
        if (!IsSafeVersion(releaseText) || !Version.TryParse(releaseText, out var version))
            throw new InvalidDataException("Versión de ClamAV no válida.");
        var assetName = $"clamav-{releaseText}.win.{AppPaths.EngineSuffix}.zip";
        foreach (var asset in release.GetProperty("assets").EnumerateArray())
        {
            if (asset.GetProperty("name").GetString() != assetName) continue;
            var url = asset.GetProperty("browser_download_url").GetString() ?? "";
            var canonical = $"https://github.com/Cisco-Talos/clamav/releases/download/{tag}/{assetName}";
            if (!string.Equals(url, canonical, StringComparison.Ordinal))
                throw new InvalidDataException("GitHub devolvió una ruta de descarga inesperada.");
            var size = asset.GetProperty("size").GetInt64();
            if (size < 1024 * 1024 || size > MaximumArchiveSize)
                throw new InvalidDataException("Tamaño de archivo ClamAV fuera del límite.");
            var digest = asset.TryGetProperty("digest", out var d) ? d.GetString() ?? "" : "";
            var hash = digest.StartsWith("sha256:", StringComparison.Ordinal) ? digest[7..] : "";
            if (!Regex.IsMatch(hash, "^[a-fA-F0-9]{64}$", RegexOptions.CultureInvariant))
            {
                // Únicamente se admite el hash fijado en el código para esta release conocida.
                if (releaseText != PinnedVersion) throw new InvalidDataException("La nueva release no publica SHA-256 verificable.");
                hash = PinnedSha256;
            }
            if (releaseText == PinnedVersion && !hash.Equals(PinnedSha256, StringComparison.OrdinalIgnoreCase))
                throw new CryptographicException("El SHA-256 oficial publicado no coincide con el conocido de ClamAV 1.5.4.");
            return new ClamEngineRelease(version, url, hash, size);
        }
        throw new InvalidDataException("La última release no incluye el motor portable para la arquitectura de esta edición.");
    }

    public static async Task<string> DownloadAndActivateAsync(ClamEngineRelease release,
        IProgress<double>? progress, CancellationToken token)
    {
        var version = release.Version.ToString(3);
        if (!IsSafeVersion(version))
            throw new InvalidDataException("Versión incorrecta.");
        var filename = $"clamav-{version}.win.{AppPaths.EngineSuffix}.zip";
        var expectedUrl = $"https://github.com/Cisco-Talos/clamav/releases/download/clamav-{version}/{filename}";
        if (release.Url != expectedUrl || !Regex.IsMatch(release.Sha256, "^[0-9a-fA-F]{64}$") ||
            release.SizeBytes < 1024 * 1024 || release.SizeBytes > MaximumArchiveSize)
            throw new InvalidDataException("Metadatos del motor no autorizados.");

        var downloads = Path.Combine(DataRoot, "Downloads");
        Directory.CreateDirectory(downloads);
        var archive = Path.Combine(downloads, filename);
        var partial = archive + ".partial";
        var final = VersionFolder(version);
        var stage = Path.Combine(DataRoot, "staging-" + Guid.NewGuid().ToString("N"));
        try
        {
            if (File.Exists(archive) && !await CheckHashAsync(archive, release.Sha256, token))
                File.Delete(archive);
            if (!File.Exists(archive))
            {
                if (File.Exists(partial)) File.Delete(partial);
                using var response = await Http.GetAsync(release.Url, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();
                if (response.RequestMessage?.RequestUri is not Uri finalUri || finalUri.Scheme != "https" ||
                    !(finalUri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
                      finalUri.Host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException("La descarga ha sido redirigida fuera de GitHub.");
                if (response.Content.Headers.ContentLength is long length && length != release.SizeBytes)
                    throw new InvalidDataException("El tamaño descargado no coincide con GitHub.");
                await using (var input = await response.Content.ReadAsStreamAsync(token))
                await using (var output = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    131072, FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    var bytes = new byte[131072];
                    long received = 0;
                    int n;
                    while ((n = await input.ReadAsync(bytes, token)) != 0)
                    {
                        received += n;
                        if (received > release.SizeBytes || received > MaximumArchiveSize)
                            throw new InvalidDataException("La descarga excede el tamaño esperado.");
                        await output.WriteAsync(bytes.AsMemory(0, n), token);
                        progress?.Report(received * 75d / release.SizeBytes);
                    }
                    if (received != release.SizeBytes)
                        throw new InvalidDataException("Descarga incompleta.");
                }
                if (!await CheckHashAsync(partial, release.Sha256, token))
                    throw new CryptographicException("El SHA-256 descargado no coincide con el oficial.");
                File.Move(partial, archive, true);
            }
            progress?.Report(77);
            Directory.CreateDirectory(stage);
            await Task.Run(() => ExtractSafely(archive, stage, token), token);
            progress?.Report(94);
            var scanner = Path.Combine(stage, "clamscan.exe");
            if (!DistributionPolicy.MatchesExecutable(scanner, AppPaths.RuntimeId) || !DistributionPolicy.MatchesExecutable(Path.Combine(stage, "freshclam.exe"), AppPaths.RuntimeId))
                throw new InvalidDataException("La arquitectura del motor no coincide con esta edición.");
            var fresh = Path.Combine(stage, "freshclam.exe");
            if (!File.Exists(scanner) || !File.Exists(fresh))
                throw new InvalidDataException("ZIP incompleto: faltan clamscan.exe o freshclam.exe.");
            var check = await ProcessRunner.RunAsync(scanner, new[] { "--version" }, (_, _) => { }, token);
            if (check.ExitCode != 0)
                throw new InvalidDataException("El motor no supera la comprobación de ejecución (--version).");
            token.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.GetDirectoryName(final)!);
            if (!Directory.Exists(final)) Directory.Move(stage, final);
            else
            {
                // Un directorio de la misma versión ya instalado solo se reutiliza si funciona.
                var existing = Path.Combine(final, "clamscan.exe");
                if (!File.Exists(existing) || !File.Exists(Path.Combine(final, "freshclam.exe")))
                    throw new InvalidDataException("Ya existe una instalación de esa versión incompleta. No se modifica la versión activa.");
                var run = await ProcessRunner.RunAsync(existing, new[] { "--version" }, (_, _) => { }, token);
                if (run.ExitCode != 0) throw new InvalidDataException("La versión preexistente no funciona.");
            }
            // El cambio de motor se realiza en último lugar; un fallo mantiene la versión anterior.
            var pointerTemp = ActivePointer + ".tmp";
            File.WriteAllText(pointerTemp, JsonSerializer.Serialize(new ActiveEngine { Version = version }), Encoding.UTF8);
            File.Move(pointerTemp, ActivePointer, true);
            progress?.Report(100);
            return Path.Combine(final, "clamscan.exe");
        }
        finally
        {
            try { if (File.Exists(partial)) File.Delete(partial); } catch (IOException) { }
            try { if (Directory.Exists(stage)) Directory.Delete(stage, true); } catch (IOException) { }
        }
    }

    private static void ExtractSafely(string archivePath, string destination, CancellationToken token)
    {
        using var zip = ZipFile.OpenRead(archivePath);
        if (zip.Entries.Count > 30000) throw new InvalidDataException("El ZIP tiene demasiados archivos.");
        long extracted = 0;
        foreach (var entry in zip.Entries)
        {
            token.ThrowIfCancellationRequested();
            var name = entry.FullName.Replace('\\', '/');
            if (name.StartsWith("/", StringComparison.Ordinal) || name.Contains(':', StringComparison.Ordinal) ||
                name.Split('/').Any(p => p == ".." || p == "."))
                throw new InvalidDataException("ZIP con ruta peligrosa.");
            if (((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000)
                throw new InvalidDataException("ZIP con enlace simbólico no permitido.");
            var parts = name.Split('/', StringSplitOptions.RemoveEmptyEntries);
            // Los paquetes portables oficiales utilizan una carpeta raíz clamav-VERSION.win.x64.
            if (parts.Length == 0) continue;
            var relative = parts[0].StartsWith("clamav-", StringComparison.OrdinalIgnoreCase)
                && parts[0].EndsWith(".win." + AppPaths.EngineSuffix, StringComparison.OrdinalIgnoreCase)
                ? string.Join('/', parts.Skip(1)) : string.Join('/', parts);
            if (relative.Length == 0) continue;
            extracted = checked(extracted + entry.Length);
            if (extracted > MaximumExtractedSize || entry.Length > MaximumExtractedSize)
                throw new InvalidDataException("ZIP demasiado grande al descomprimir.");
            var target = Path.GetFullPath(Path.Combine(destination, relative.Replace('/', Path.DirectorySeparatorChar)));
            var prefix = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("ZIP intenta extraer fuera de la carpeta temporal.");
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
                Directory.CreateDirectory(target);
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                entry.ExtractToFile(target, overwrite: false);
            }
        }
    }

    private static async Task<bool> CheckHashAsync(string file, string expected, CancellationToken token)
    {
        await using var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read,
            131072, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var actual = await SHA256.HashDataAsync(input, token);
        return Convert.ToHexString(actual).Equals(expected, StringComparison.OrdinalIgnoreCase);
    }
}
