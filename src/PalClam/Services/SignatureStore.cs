// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Text;

namespace PalClam.Services;

/// <summary>
/// Las firmas se mantienen en el perfil local. Si hay firmas iniciales en el
/// instalador se copian UNA VEZ, sin reemplazar las posteriores de FreshClam.
/// </summary>
internal static class SignatureStore
{
    private static readonly string BaseDirectoryPath = Path.Combine(
        PalClam.Services.AppPaths.Root);

    public static string DatabaseDirectory => Path.Combine(BaseDirectoryPath, "Signatures");
    public static string FreshClamConfigPath => Path.Combine(BaseDirectoryPath, "freshclam.conf");
    private static string InstallerDatabase => Path.Combine(AppContext.BaseDirectory, "OfflineSignatures");

    private static bool IsPresent(string filename)
    {
        try { return File.Exists(filename) && new FileInfo(filename).Length >= 128; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    // Existencia y tamaño mínimo, nunca afirmar que una base vacía está disponible.
    // ClamAV validará su contenido real al cargarla.
    private static bool HasFamily(string family) =>
        IsPresent(Path.Combine(DatabaseDirectory, family + ".cvd")) ||
        IsPresent(Path.Combine(DatabaseDirectory, family + ".cld"));

    // Tener solamente bytecode.cvd no constituye una base operativa.
    public static bool HasSignatures() => HasFamily("main") && HasFamily("daily");

    public static int SeedFromInstaller()
    {
        if (!Directory.Exists(InstallerDatabase)) return 0;
        Directory.CreateDirectory(DatabaseDirectory);
        int imported = 0;
        foreach (var family in new[] { "main", "daily", "bytecode" })
        {
            // NO sustituir firmas ya presentes (CVD o CLD), que pueden ser más recientes.
            if (HasFamily(family)) continue;
            var source = new[] { family + ".cld", family + ".cvd" }
                .Select(f => Path.Combine(InstallerDatabase, f))
                .FirstOrDefault(File.Exists);
            if (source is null) continue;
            if (new FileInfo(source).Length < 128) continue;
            var destination = Path.Combine(DatabaseDirectory, Path.GetFileName(source));
            var temp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.Copy(source, temp, overwrite: false);
                if (!HasFamily(family))
                {
                    File.Move(temp, destination, overwrite: false);
                    imported++;
                }
            }
            finally { try { if (File.Exists(temp)) File.Delete(temp); } catch (IOException) { } }
        }
        return imported;
    }

    public static DateTime? OfflineDailyDateUtc()
    {
        var found = new[] { "daily.cld", "daily.cvd" }
            .Select(f => Path.Combine(InstallerDatabase, f)).FirstOrDefault(File.Exists);
        return found is null ? null : File.GetLastWriteTimeUtc(found);
    }

    public static string PrepareFreshClamConfig()
    {
        Directory.CreateDirectory(BaseDirectoryPath);
        Directory.CreateDirectory(DatabaseDirectory);
        var dbPath = DatabaseDirectory.Replace('\\', '/');
        if (dbPath.Contains('"') || dbPath.Contains('\n') || dbPath.Contains('\r'))
            throw new InvalidOperationException("La ruta de firmas contiene caracteres no admitidos.");
        if (AppPaths.IsPortable || !File.Exists(FreshClamConfigPath))
        {
            var config = new StringBuilder()
                .AppendLine("# PalClam - configuración FreshClam local (generada por PalClam)")
                .AppendLine($"DatabaseDirectory \"{dbPath}\"")
                .AppendLine("DatabaseMirror database.clamav.net")
                .AppendLine("Checks 12")
                .AppendLine("LogTime yes")
                .ToString();
            File.WriteAllText(FreshClamConfigPath, config, new UTF8Encoding(false));
        }
        return FreshClamConfigPath;
    }
}
