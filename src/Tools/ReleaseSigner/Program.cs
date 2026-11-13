// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

// Herramienta local del autor; nunca se publica la clave privada.
internal static class Program
{
    private static string PrivateKeyPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PalClam-Publisher", "release-private.pem");

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length < 2) throw new ArgumentException("Uso: init <carpeta-proyecto> | release <carpeta-proyecto> [notas]");
            var root = Path.GetFullPath(args[1]);
            if (args[0].Equals("init", StringComparison.OrdinalIgnoreCase)) Init(root);
            else if (args[0].Equals("release", StringComparison.OrdinalIgnoreCase))
                MakeRelease(root, args.Length > 2 ? args[2] : "Actualizacion de seguridad y mejoras de PalClam.");
            else if (args[0].Equals("verify", StringComparison.OrdinalIgnoreCase)) VerifyPublishedRelease(root);
            else throw new ArgumentException("Comando desconocido: " + args[0]);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[ERROR] " + ex.Message);
            return 1;
        }
    }

    private static string PublicKeyPath(string root) => Path.Combine(root, "PalClam", "Resources", "update-public.pem");

    private static void Init(string root)
    {
        var publicKeyPath = PublicKeyPath(root);
        var hasPrivate = File.Exists(PrivateKeyPath);
        var hasPublic = File.ReadAllText(publicKeyPath).Contains("BEGIN PUBLIC KEY", StringComparison.Ordinal);
        if (!hasPrivate && hasPublic)
            throw new InvalidOperationException("Hay una clave publica existente pero falta su clave privada. RECUPERALA de una copia de seguridad. No gires las claves en una version distribuida.");
        Directory.CreateDirectory(Path.GetDirectoryName(PrivateKeyPath)!);
        using var rsa = RSA.Create();
        if (!hasPrivate)
        {
            rsa.KeySize = 3072;
            File.WriteAllText(PrivateKeyPath, rsa.ExportPkcs8PrivateKeyPem(), new UTF8Encoding(false));
            Console.WriteLine("[NUEVA CLAVE] Protegela y guardala fuera del proyecto: " + PrivateKeyPath);
        }
        else rsa.ImportFromPem(File.ReadAllText(PrivateKeyPath));
        var expectedPublic = rsa.ExportSubjectPublicKeyInfoPem();
        if (hasPublic && File.ReadAllText(publicKeyPath).Trim() != expectedPublic.Trim())
            throw new CryptographicException("La clave privada no corresponde a la clave publica instalada. No se compilara una version inconsistente.");
        if (!hasPublic) File.WriteAllText(publicKeyPath, expectedPublic, new UTF8Encoding(false));
        Console.WriteLine("[OK] Clave publica de PalClam preparada. Clave privada NO se incluye en el instalador.");
    }

    private static void VerifyPublishedRelease(string root)
    {
        var folder = Path.Combine(root, "dist", "publish");
        var json = File.ReadAllBytes(Path.Combine(folder, "latest.json"));
        var sig = Convert.FromBase64String(File.ReadAllText(Path.Combine(folder, "latest.json.sig")).Trim());
        using var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(PublicKeyPath(root)));
        if (!rsa.VerifyData(json, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            throw new CryptographicException("La firma del manifiesto publicado no es valida.");
        using var info = JsonDocument.Parse(json);
        var rootJson = info.RootElement;
        var version = rootJson.GetProperty("version").GetString() ?? "";
        var url = rootJson.GetProperty("downloadUrl").GetString() ?? "";
        var filename = $"PalClam_{version}_Setup_win-x64.exe";
        if (url != "https://www.palentino.es/palclam/updates/" + filename)
            throw new InvalidDataException("La URL del instalador no se corresponde con el canal oficial.");
        var installer = Path.Combine(folder, filename);
        using var input = File.OpenRead(installer);
        var digest = Convert.ToHexString(SHA256.HashData(input));
        if (!digest.Equals(rootJson.GetProperty("sha256").GetString(), StringComparison.OrdinalIgnoreCase))
            throw new CryptographicException("El instalador publicado NO coincide con el hash firmado.");
        if (new FileInfo(installer).Length != rootJson.GetProperty("sizeBytes").GetInt64())
            throw new InvalidDataException("El tamano del instalador publicado no es correcto.");
        Console.WriteLine("[OK] Canal de actualizaciones verificado localmente: " + version);
    }

    private static void MakeRelease(string root, string notes)
    {
        Init(root);
        var versionNode = XDocument.Load(Path.Combine(root, "PalClam", "PalClam.csproj"))
            .Descendants("Version").FirstOrDefault();
        var version = versionNode?.Value ?? throw new InvalidDataException("Falta Version en csproj.");
        if (!Regex.IsMatch(version, "^[0-9]+\\.[0-9]+\\.[0-9]+$"))
            throw new InvalidDataException("Version debe ser X.Y.Z.");
        var source = Path.Combine(root, "dist", "installer", $"PalClam_{version}_Setup_win-x64.exe");
        if (!File.Exists(source)) throw new FileNotFoundException("Primero ejecuta CREAR_INSTALADOR.bat", source);
        var destination = Path.Combine(root, "dist", "publish");
        Directory.CreateDirectory(destination);
        var installerName = Path.GetFileName(source);
        File.Copy(source, Path.Combine(destination, installerName), overwrite: true);
        using var input = File.OpenRead(source);
        var sha256 = Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
        var url = "https://www.palentino.es/palclam/updates/" + installerName;
        // El manifiesto se firma BYTE A BYTE; no cambiar su codificacion despues de publicarlo.
        var info = new
        {
            version,
            channel = "stable",
            downloadUrl = url,
            sha256,
            sizeBytes = new FileInfo(source).Length,
            notes = notes.Length <= 250 ? notes : notes[..250]
        };
        var json = JsonSerializer.SerializeToUtf8Bytes(info, new JsonSerializerOptions { WriteIndented = true });
        using var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(PrivateKeyPath));
        var sig = rsa.SignData(json, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        File.WriteAllBytes(Path.Combine(destination, "latest.json"), json);
        File.WriteAllText(Path.Combine(destination, "latest.json.sig"), Convert.ToBase64String(sig), Encoding.ASCII);
        Console.WriteLine("[OK] Actualizacion firmada: " + version);
        Console.WriteLine("[PUBLICAR] Sube el contenido de dist\\publish\\ al directorio HTTPS /palclam/updates/ de tu servidor.");
        Console.WriteLine("[ATENCION] No distribuyas " + PrivateKeyPath);
        VerifyPublishedRelease(root);
    }
}
