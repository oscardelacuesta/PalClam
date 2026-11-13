// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Security.Cryptography;
using System.Text.Json;

namespace PalClam.Services;

internal sealed class QuarantineEntry
{
    public string Id { get; set; } = "";
    public string OriginalPath { get; set; } = "";
    public string Detection { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public long Size { get; set; }
    public DateTimeOffset QuarantinedAt { get; set; }
    public DateTimeOffset? RestoredAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IntegrityWarning { get; set; }
    public DateTimeOffset? LastRecheckAt { get; set; }
    public string LastRecheckVerdict { get; set; } = "";
    public string LastRecheckDetail { get; set; } = "";
    public bool RequestedManualReview { get; set; }
    public DateTimeOffset? ManualReviewAt { get; set; }
    public string ReviewLabel => RequestedManualReview ? "Revisión solicitada" : "Sin solicitar";
    public string RecheckLabel => LastRecheckAt is null ? "Sin reanalizar" :
        LastRecheckAt.Value.ToLocalTime().ToString("dd/MM HH:mm") + " · " + LastRecheckVerdict;
    public string State => DeletedAt is not null ? "Eliminado" : RestoredAt is not null ? "Restaurado" : IntegrityWarning ? "REVISAR INTEGRIDAD" : "Aislado";
}

/// <summary>
/// Cuarentena voluntaria por usuario. Nunca se mueve un archivo sin confirmación.
/// No se ejecutan ni abren elementos de la cuarentena desde la aplicación.
/// </summary>
internal static class QuarantineStore
{
#if PALCLAM_SMOKETEST
    // Aislamiento exclusivo de pruebas. No tocar la cuarentena real del usuario.
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "PalClam-SmokeTestQuarantine-" + Guid.NewGuid().ToString("N"));
    internal static void CleanupTestRoot()
    {
        if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
    }
#else
    private static readonly string Root = Path.Combine(
        PalClam.Services.AppPaths.Root, "Quarantine");
#endif
    private static string DataPath(string id) => Path.Combine(Root, id + ".pclam");
    private static string InfoPath(string id) => Path.Combine(Root, id + ".json");

    public static IReadOnlyList<QuarantineEntry> List()
    {
        if (!Directory.Exists(Root)) return Array.Empty<QuarantineEntry>();
        var entries = new List<QuarantineEntry>();
        foreach (var file in Directory.EnumerateFiles(Root, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var entry = JsonSerializer.Deserialize<QuarantineEntry>(File.ReadAllText(file));
                if (entry is not null && Guid.TryParseExact(entry.Id, "N", out _) &&
                    (entry.RestoredAt is not null || entry.DeletedAt is not null || File.Exists(DataPath(entry.Id)))) entries.Add(entry);
            }
            catch (Exception) { /* Entradas parciales no deben bloquear la interfaz. */ }
        }
        return entries.OrderByDescending(e => e.QuarantinedAt).Take(500).ToArray();
    }

    public static async Task<QuarantineEntry> IsolateAsync(string source, string detection)
    {
        var original = Path.GetFullPath(source);
        if (!File.Exists(original) || Directory.Exists(original))
            throw new IOException("La ruta no identifica un archivo real. Los elementos internos de ZIP no pueden aislarse directamente.");
        if (original.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("El archivo ya pertenece a la cuarentena de PalClam.");
        if ((File.GetAttributes(original) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Por seguridad, no se trasladan enlaces ni puntos de reanálisis.");
        var info = new FileInfo(original);
        if (info.Length > 10L * 1024 * 1024 * 1024)
            throw new IOException("Archivo demasiado grande para el procedimiento seguro de cuarentena (máximo 10 GiB).");
        Directory.CreateDirectory(Root);
        var id = Guid.NewGuid().ToString("N");
        var entry = new QuarantineEntry
        {
            Id = id, OriginalPath = original, Detection = detection,
            Sha256 = await HashFileAsync(original), Size = info.Length, QuarantinedAt = DateTimeOffset.Now
        };
        // Registrar primero; no mover si el inventario no puede guardarse.
        await File.WriteAllTextAsync(InfoPath(id), JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true }));
        try
        {
            // El archivo permanece con nombre opaco y extensión no ejecutable.
            File.Move(original, DataPath(id));
        }
        catch
        {
            try { File.Delete(InfoPath(id)); } catch (IOException) { }
            throw;
        }
        // Un archivo puede cambiar entre el cálculo inicial del hash y el traslado.
        // Si ocurre, NO afirmar que la cuarentena se completó sin incidentes:
        // conservarlo aislado y bloquear la restauración hasta revisión manual.
        var relocatedHash = await HashFileAsync(DataPath(id));
        if (!relocatedHash.Equals(entry.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            entry.IntegrityWarning = true;
            var tempInfo = InfoPath(id) + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllTextAsync(tempInfo, JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true }));
                File.Move(tempInfo, InfoPath(id), overwrite: true);
            }
            finally { if (File.Exists(tempInfo)) File.Delete(tempInfo); }
            throw new IOException("El archivo cambió durante el aislamiento y se ha conservado en cuarentena. Revisar integridad antes de restaurar.");
        }
        RemediationAuditLog.Append("Aislamiento", entry.Id, "Aislado", entry.Detection);
        return entry;
    }

    public static async Task RestoreAsync(QuarantineEntry entry)
    {
        if (!Guid.TryParseExact(entry.Id, "N", out _)) throw new IOException("Identificador inválido.");
        // Nunca confiar en una ruta externa introducida en JSON: comprobar la ficha en disco.
        var metadata = JsonSerializer.Deserialize<QuarantineEntry>(await File.ReadAllTextAsync(InfoPath(entry.Id)))
            ?? throw new IOException("Registro de cuarentena dañado.");
        if (metadata.Id != entry.Id || metadata.RestoredAt is not null || metadata.DeletedAt is not null)
            throw new IOException("Este elemento ya se restauró o cambió de estado.");
        if (metadata.IntegrityWarning)
            throw new IOException("La cuarentena detectó un cambio de integridad: restauración bloqueada.");
        var source = DataPath(entry.Id);
        if (!File.Exists(source)) throw new FileNotFoundException("No existe el archivo aislado.");
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Se ha detectado un enlace inesperado en cuarentena.");
        var hash = await HashFileAsync(source);
        if (!hash.Equals(metadata.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new IOException("La huella SHA-256 del archivo ha cambiado. No se restaurará.");
        var destination = Path.GetFullPath(metadata.OriginalPath);
        if (File.Exists(destination) || Directory.Exists(destination))
            throw new IOException("La ruta original ya existe. No se sobrescribirá ningún archivo.");
        if (!Directory.Exists(Path.GetDirectoryName(destination)))
            throw new IOException("La carpeta original no existe. No se han creado directorios automáticamente.");
        // No restaurar a través de un directorio enlazado o redireccionado.
        var parent = Path.GetDirectoryName(destination)!;
        while (!string.IsNullOrEmpty(parent))
        {
            if ((File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("La ruta de restauración contiene un enlace o punto de reanálisis.");
            var next = Path.GetDirectoryName(parent);
            if (string.Equals(next, parent, StringComparison.OrdinalIgnoreCase)) break;
            parent = next ?? "";
        }
        File.Move(source, destination);
        metadata.RestoredAt = DateTimeOffset.Now;
        try
        {
            await File.WriteAllTextAsync(InfoPath(entry.Id), JsonSerializer.Serialize(metadata,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            throw new IOException("El archivo se restauró, pero no se pudo registrar el estado: " + ex.Message, ex);
        }
        RemediationAuditLog.Append("Restauración", entry.Id, "Restaurado",
            "Restauración voluntaria en la ruta original; no implica que el archivo sea seguro.");
    }

    /// <summary>
    /// Destruye EXCLUSIVAMENTE el archivo físico de cuarentena, nunca la ruta original.
    /// La interfaz debe solicitar confirmación explícita antes de llamar a este método.
    /// Conserva el registro de inventario con DeletedAt para auditoría y trazabilidad.
    /// NO es un borrado forense de sectores físicos; no promete eliminación irrecuperable.
    /// </summary>
    public static async Task DeleteIsolatedAsync(QuarantineEntry entry)
    {
        if (entry is null || !Guid.TryParseExact(entry.Id, "N", out _))
            throw new IOException("Identificador de cuarentena no válido.");
        var infoFile = InfoPath(entry.Id);
        if (!File.Exists(infoFile) || (File.GetAttributes(infoFile) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("La ficha de cuarentena no existe o es un enlace no admitido.");
        var metadata = JsonSerializer.Deserialize<QuarantineEntry>(await File.ReadAllTextAsync(infoFile))
            ?? throw new IOException("Ficha de cuarentena dañada.");
        if (metadata.Id != entry.Id || metadata.RestoredAt is not null || metadata.DeletedAt is not null)
            throw new IOException("El elemento ha cambiado de estado o ya no está aislado.");
        var isolated = DataPath(entry.Id);
        if (!File.Exists(isolated)) throw new IOException("No se encuentra el archivo aislado: no se ha borrado nada.");
        if ((File.GetAttributes(isolated) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("El archivo aislado es un enlace y no puede borrarse mediante PalClam.");
        // El nombre físico procede exclusivamente de un GUID generado por PalClam.
        // No usar OriginalPath ni cualquier ruta tomada del JSON para borrar.
        File.Delete(isolated);
        metadata.DeletedAt = DateTimeOffset.Now;
        // Archivo temporal y sustitución atómica de los metadatos siempre que sea posible.
        var temporary = infoFile + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(metadata,
                new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, infoFile, overwrite: true);
        }
        catch (Exception ex)
        {
            throw new IOException("El archivo aislado se eliminó, pero no pudo guardarse el registro. " +
                "Se requiere revisión de la cuarentena: " + ex.Message, ex);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        RemediationAuditLog.Append("Eliminación", entry.Id, "Eliminado",
            "Eliminado exclusivamente el objeto aislado; no es borrado forense.");
    }

    /// <summary>
    /// Valida el registro real, el estado, el fichero aislado y su SHA-256 sin
    /// utilizar la ruta de origen para leer, ejecutar o restaurar nada.
    /// </summary>
    internal static async Task<string> VerifyIsolatedAsync(QuarantineEntry entry,
        CancellationToken cancellationToken = default)
    {
        if (!QuarantineReviewPolicy.CanRecheck(entry))
            throw new IOException("Solo se pueden revisar elementos aislados y sin aviso de integridad.");
        var info = InfoPath(entry.Id);
        if (!File.Exists(info) || (File.GetAttributes(info) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("El registro aislado falta o es un enlace no admitido.");
        var current = JsonSerializer.Deserialize<QuarantineEntry>(await File.ReadAllTextAsync(info, cancellationToken))
            ?? throw new IOException("Registro aislado inválido.");
        if (current.Id != entry.Id || !QuarantineReviewPolicy.CanRecheck(current) ||
            (current.Sha256?.Length ?? 0) != 64)
            throw new IOException("El elemento ha cambiado de estado o tiene un registro inválido.");
        var path = DataPath(entry.Id);
        if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("El archivo aislado no existe o es un enlace no admitido.");
        if (new FileInfo(path).Length != current.Size)
            throw new IOException("El tamaño del archivo aislado ha cambiado.");
        var checksum = await HashFileAsync(path, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!checksum.Equals(current.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new IOException("SHA-256 de cuarentena diferente: operación bloqueada.");
        return path;
    }

    internal static async Task SaveRecheckAsync(QuarantineEntry entry,
        QuarantineRecheckResult result, CancellationToken cancellationToken = default)
    {
        await VerifyIsolatedAsync(entry, cancellationToken);
        var info = InfoPath(entry.Id);
        var current = JsonSerializer.Deserialize<QuarantineEntry>(await File.ReadAllTextAsync(info, cancellationToken))
            ?? throw new IOException("Registro de cuarentena inválido.");
        if (current.Id != entry.Id || !QuarantineReviewPolicy.CanRecheck(current))
            throw new IOException("El estado de cuarentena cambió durante el análisis.");
        current.LastRecheckAt = result.At;
        current.LastRecheckVerdict = result.DisplayVerdict;
        current.LastRecheckDetail = result.Detail.Length > 600 ? result.Detail[..600] : result.Detail;
        await WriteMetadataAtomicallyAsync(info, current, cancellationToken);
    }

    internal static async Task FlagManualReviewAsync(QuarantineEntry entry,
        CancellationToken cancellationToken = default)
    {
        await VerifyIsolatedAsync(entry, cancellationToken);
        var info = InfoPath(entry.Id);
        var current = JsonSerializer.Deserialize<QuarantineEntry>(await File.ReadAllTextAsync(info, cancellationToken))
            ?? throw new IOException("Registro de cuarentena inválido.");
        if (current.Id != entry.Id || !QuarantineReviewPolicy.CanRecheck(current))
            throw new IOException("El elemento ya no está disponible para revisar.");
        current.RequestedManualReview = true;
        current.ManualReviewAt = DateTimeOffset.Now;
        await WriteMetadataAtomicallyAsync(info, current, cancellationToken);
        RemediationAuditLog.Append("Revisión manual", entry.Id, "Solicitada",
            "Marcado para comprobar un posible falso positivo; no modifica el veredicto de ClamAV.");
    }

    private static async Task WriteMetadataAtomicallyAsync(string infoPath, QuarantineEntry entry,
        CancellationToken cancellationToken)
    {
        var temporary = infoPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(entry,
                new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, infoPath, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static async Task<string> HashFileAsync(string file, CancellationToken cancellationToken = default)
    {
        await using var input = new FileStream(file, FileMode.Open, FileAccess.Read,
            FileShare.Read, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Convert.ToHexString(await SHA256.HashDataAsync(input, cancellationToken));
    }
}
