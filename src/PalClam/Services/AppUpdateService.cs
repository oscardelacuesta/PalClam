// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PalClam.Services;

// El instalador NO se ejecuta nunca si falla la firma del manifiesto o el SHA-256 del EXE.
// La clave publica se incorpora al programa al compilar; la privada queda FUERA del proyecto.
internal sealed class ReleaseManifest
{
    public string Version { get; set; } = "";
    public string Channel { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public long SizeBytes { get; set; }
    public string Notes { get; set; } = "";
}

internal static class AppUpdateService
{
    public static readonly Uri FeedUrl = new(AppPaths.RuntimeId == "win-x86"
        ? "https://www.palentino.es/palclam/updates/latest-win-x86.json" : "https://www.palentino.es/palclam/updates/latest.json");
    private const long MaxInstallerBytes = 1024L * 1024 * 1024;
    private static readonly HttpClient Client = new(new HttpClientHandler
    {
        AllowAutoRedirect = false, // No permitir redireccion a un servidor externo
        AutomaticDecompression = DecompressionMethods.None
    }) { Timeout = TimeSpan.FromMinutes(35) };

    public static Version InstalledVersion
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
            // Un ensamblado suele ser 1.0.0.0, pero el feed anuncia 1.0.0.
            // Normalizar evita comparar injustamente una RC con la estable.
            return new Version(assembly.Major, assembly.Minor, Math.Max(0, assembly.Build));
        }
    }

    // Una RC tiene el mismo número de versión que la estable, pero debe poder
    // actualizarse cuando se publique el instalador estable firmado.
    private static bool IsReleaseCandidate =>
        (Assembly.GetExecutingAssembly()
          .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "")
          .Contains("-rc", StringComparison.OrdinalIgnoreCase);

    // Esta validación se repite justo antes de descargar. Un objeto en memoria
    // puede haberse modificado después de verificar el manifiesto firmado.
    // No se permiten rutas alternativas, parámetros, fragmentos o credenciales.
    internal static bool TryValidateReleaseMetadata(ReleaseManifest? release, out string reason)
    {
        reason = "";
        if (release is null || release.Channel != "stable" ||
            release.Version is null || !Regex.IsMatch(release.Version, @"^\d+\.\d+\.\d+$", RegexOptions.CultureInvariant) ||
            !Version.TryParse(release.Version, out _))
        {
            reason = "Canal o versión no autorizados.";
            return false;
        }
        if (release.Sha256 is null || !Regex.IsMatch(release.Sha256, @"^[A-Fa-f0-9]{64}$", RegexOptions.CultureInvariant) ||
            release.SizeBytes < 1024 || release.SizeBytes > MaxInstallerBytes)
        {
            reason = "Tamaño o SHA-256 del instalador no válidos.";
            return false;
        }
        var expected = $"https://www.palentino.es/palclam/updates/PalClam_{release.Version}_Setup_{AppPaths.RuntimeId}.exe";
        if (!string.Equals(release.DownloadUrl, expected, StringComparison.Ordinal) ||
            !Uri.TryCreate(release.DownloadUrl, UriKind.Absolute, out var parsed) ||
            parsed.Scheme != Uri.UriSchemeHttps || parsed.Host != FeedUrl.Host || parsed.Port != 443 ||
            parsed.UserInfo.Length > 0 || parsed.Query.Length > 0 || parsed.Fragment.Length > 0 ||
            !string.Equals(parsed.AbsoluteUri, expected, StringComparison.Ordinal))
        {
            reason = "La URL del instalador no es exactamente la dirección oficial esperada.";
            return false;
        }
        return true;
    }

    public static async Task<ReleaseManifest?> CheckAsync(CancellationToken cancellationToken)
    {
        byte[] manifest;
        try
        {
            manifest = await GetSmallResourceAsync(FeedUrl, 32768, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null; // Aun no se ha publicado el canal de actualizaciones
        }

        var signatureUrl = new Uri(FeedUrl.AbsoluteUri + ".sig");
        var signatureBytes = await GetSmallResourceAsync(signatureUrl, 8192, cancellationToken);
        var signature = Convert.FromBase64String(Encoding.ASCII.GetString(signatureBytes).Trim());
        var publicKey = ReadEmbeddedPublicKey();
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKey);
        if (!rsa.VerifyData(manifest, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            throw new CryptographicException("La firma digital del manifiesto no es valida. Actualizacion rechazada.");

        var info = JsonSerializer.Deserialize<ReleaseManifest>(manifest,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("El manifiesto de actualizacion esta vacio.");
        if (!TryValidateReleaseMetadata(info, out var validationError))
            throw new InvalidDataException("Metadatos de actualización rechazados: " + validationError);
        var version = Version.Parse(info.Version);
        return version > InstalledVersion || (version == InstalledVersion && IsReleaseCandidate) ? info : null;
    }

    public static async Task<string> DownloadVerifiedInstallerAsync(ReleaseManifest release,
        IProgress<double>? progress, CancellationToken cancellationToken)
    {
        if (AppPaths.IsPortable) throw new InvalidOperationException("En modo portable, actualiza PalClam sustituyendo su carpeta con el ZIP de la nueva versión.");
        if (!TryValidateReleaseMetadata(release, out var validationError))
            throw new InvalidDataException("Descarga de actualización rechazada: " + validationError);
        var uri = new Uri(release.DownloadUrl);
        var folder = Path.Combine(PalClam.Services.AppPaths.Root, "Updates", release.Version);
        Directory.CreateDirectory(folder);
        var filename = Path.Combine(folder, "PalClam_update_" + release.Version + ".exe");
        if (File.Exists(filename))
        {
            if (await HashMatchesAsync(filename, release.Sha256, cancellationToken)) return filename;
            File.Delete(filename);
        }
        var part = filename + ".partial";
        if (File.Exists(part)) File.Delete(part);

        try
        {
            using var response = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is long expected && expected != release.SizeBytes)
                throw new InvalidDataException("El tamano del instalador no coincide con el manifiesto firmado.");
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = new FileStream(part, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                131072, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var buffer = new byte[131072];
            long total = 0;
            int read;
            while ((read = await input.ReadAsync(buffer, cancellationToken)) != 0)
            {
                total += read;
                if (total > release.SizeBytes || total > MaxInstallerBytes)
                    throw new InvalidDataException("La descarga supera el tamano firmado.");
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                progress?.Report(total * 100.0 / release.SizeBytes);
            }
            await output.FlushAsync(cancellationToken);
            output.Close();
            if (total != release.SizeBytes)
                throw new InvalidDataException("La descarga esta incompleta.");
            if (!await HashMatchesAsync(part, release.Sha256, cancellationToken))
                throw new CryptographicException("SHA-256 incorrecto: instalador descartado.");
            File.Move(part, filename);
            progress?.Report(100);
            return filename;
        }
        finally
        {
            if (File.Exists(part)) File.Delete(part);
        }
    }

    private static async Task<byte[]> GetSmallResourceAsync(Uri url, int maximum, CancellationToken token)
    {
        using var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is long count && count > maximum)
            throw new InvalidDataException("El servidor ha enviado un archivo de metadatos demasiado grande.");
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var memory = new MemoryStream();
        var buffer = new byte[4096];
        int n;
        while ((n = await stream.ReadAsync(buffer, token)) != 0)
        {
            if (memory.Length + n > maximum)
                throw new InvalidDataException("Los metadatos superan el limite permitido.");
            memory.Write(buffer, 0, n);
        }
        return memory.ToArray();
    }

    private static string ReadEmbeddedPublicKey()
    {
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("PalClam.Updates.PublicKey")
            ?? throw new InvalidOperationException("Falta la clave publica de actualizaciones.");
        using var reader = new StreamReader(resource);
        var pem = reader.ReadToEnd();
        if (!pem.Contains("BEGIN PUBLIC KEY", StringComparison.Ordinal))
            throw new InvalidOperationException("Publicacion no configurada. Ejecuta INICIALIZAR_ACTUALIZACIONES.bat y recompila.");
        return pem;
    }

    public static async Task<bool> HashMatchesAsync(string filename, string expected, CancellationToken token)
    {
        await using var input = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read,
            131072, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(input, token);
        return string.Equals(Convert.ToHexString(hash), expected, StringComparison.OrdinalIgnoreCase);
    }
}
