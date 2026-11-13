// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PalClam.Services;

/// <summary>
/// Información local del archivo. No ejecuta el contenido ni lo transmite.
/// La presencia de un certificado Authenticode NO verifica por sí misma
/// la integridad, confianza ni revocación de una firma.
/// </summary>
internal sealed record InspectorCertificate(string Status, string Subject,
    string Issuer, string Thumbprint, DateTime? NotBefore, DateTime? NotAfter);

internal sealed record FileInspectionReport(string FullPath, string Name, long SizeBytes,
    string Extension, DateTime CreatedLocal, DateTime ModifiedLocal,
    string Sha256, InspectorCertificate Certificate, string NameWarning);

internal static class FileInspectorService
{
    public static Task<FileInspectionReport> InspectAsync(string selectedPath, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(selectedPath))
            throw new ArgumentException("Selecciona un archivo.", nameof(selectedPath));
        return Task.Run(async () =>
        {
            token.ThrowIfCancellationRequested();
            var fullPath = Path.GetFullPath(selectedPath);
            var attributes = File.GetAttributes(fullPath);
            if ((attributes & FileAttributes.Directory) != 0)
                throw new ArgumentException("El inspector admite archivos, no carpetas.");
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("El inspector no sigue vínculos simbólicos o puntos de análisis.");

            var info = new FileInfo(fullPath);
            var length = info.Length;
            var lastWriteUtc = info.LastWriteTimeUtc;
            var createdUtc = info.CreationTimeUtc;
            byte[] digest;
            // Streaming: no se vuelca el archivo a memoria, aunque sea muy grande.
            await using (var stream = new FileStream(fullPath, new FileStreamOptions
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Share = FileShare.ReadWrite | FileShare.Delete,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                BufferSize = 128 * 1024
            }))
                digest = await SHA256.HashDataAsync(stream, token);
            token.ThrowIfCancellationRequested();
            info.Refresh();
            if (!info.Exists || info.Length != length || info.LastWriteTimeUtc != lastWriteUtc)
                throw new IOException("El archivo cambió durante la lectura. Vuelve a inspeccionarlo.");
            var certificate = ReadAuthenticodeCertificate(fullPath);
            token.ThrowIfCancellationRequested();
            info.Refresh();
            if (!info.Exists || info.Length != length || info.LastWriteTimeUtc != lastWriteUtc)
                throw new IOException("El archivo cambió durante la consulta. Vuelve a inspeccionarlo.");
            return new FileInspectionReport(fullPath, info.Name, length,
                Path.GetExtension(info.Name), createdUtc.ToLocalTime(),
                lastWriteUtc.ToLocalTime(), Convert.ToHexString(digest),
                certificate, CheckDoubleExtension(info.Name));
        }, token);
    }

    public static string CheckDoubleExtension(string name)
    {
        var finalExtension = Path.GetExtension(name).ToLowerInvariant();
        if (finalExtension is not (".exe" or ".scr" or ".com" or ".bat" or ".cmd" or ".ps1" or ".vbs"))
            return "";
        var earlierExtension = Path.GetExtension(Path.GetFileNameWithoutExtension(name)).ToLowerInvariant();
        return earlierExtension is ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or
            ".jpg" or ".jpeg" or ".png" or ".txt" or ".zip"
            ? "Aviso: doble extensión que puede confundir sobre el tipo de archivo. No es una detección de malware."
            : "";
    }

    public static string FormatSize(long bytes)
    {
        if (bytes < 1024) return bytes.ToString("N0") + " B";
        var value = (double)bytes;
        var unit = 0;
        var units = new[] { "B", "KiB", "MiB", "GiB", "TiB" };
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return value.ToString("N2") + " " + units[unit];
    }

    private static InspectorCertificate ReadAuthenticodeCertificate(string path)
    {
        if (!OperatingSystem.IsWindows())
            return new("No disponible en este sistema", "", "", "", null, null);
        try
        {
            // Solo identifica el certificado incluido. No hace WinVerifyTrust ni validación OCSP/CRL.
            // .NET 10 marca este método heredado como SYSLIB0057: a diferencia
            // de LoadCertificate, permite extraer un certificado de un PE firmado.
            // Se acota la supresión a esta llamada, NO a toda la aplicación.
#pragma warning disable SYSLIB0057
            using var embedded = X509Certificate.CreateFromSignedFile(path);
#pragma warning restore SYSLIB0057
            using var certificate = X509CertificateLoader.LoadCertificate(
                embedded.Export(X509ContentType.Cert));
            return new("Certificado Authenticode encontrado (NO verificado)",
                certificate.GetNameInfo(X509NameType.SimpleName, false),
                certificate.Issuer, certificate.Thumbprint,
                certificate.NotBefore, certificate.NotAfter);
        }
        catch (CryptographicException)
        {
            return new("Sin certificado Authenticode detectado (no concluyente para otros formatos)",
                "", "", "", null, null);
        }
        catch (PlatformNotSupportedException)
        {
            return new("Consulta Authenticode no disponible", "", "", "", null, null);
        }
        catch (IOException)
        {
            return new("No se pudo consultar el certificado", "", "", "", null, null);
        }
        catch (UnauthorizedAccessException)
        {
            return new("Sin permisos para consultar el certificado", "", "", "", null, null);
        }
    }
}
