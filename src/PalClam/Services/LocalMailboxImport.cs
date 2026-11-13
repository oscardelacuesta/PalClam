// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Text;
using System.Text.RegularExpressions;

namespace PalClam.Services;

internal enum MailSourceKind { Exported, Mbox, Maildir, OutlookClassic }

// Solo se crean copias con nombres generados dentro de una carpeta temporal privada por sesión.
internal sealed class MailImportSession : IDisposable
{
    internal string DirectoryPath { get; } = Path.Combine(AppPaths.PrivateTemp, "PalClam-Import-" + Guid.NewGuid().ToString("N"));
    internal Dictionary<string, string> Sources { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal int Seen, Skipped, Errors;
    internal bool Truncated;
    internal long Bytes;
    internal const long MaxTemporaryBytes = 512L * 1024 * 1024;
    private bool _cleaned;
    internal MailImportSession() => Directory.CreateDirectory(DirectoryPath);
    internal string NextPath(string extension) => Path.Combine(DirectoryPath, Guid.NewGuid().ToString("N") + extension);
    internal void Notice(Action<MailFinding> report, string source, string detail, bool error = false)
    {
        if (error) Errors++; else Skipped++;
        report(new(error ? "Incompleto" : "Omitido", source, "", detail, ""));
    }
    internal bool Accept(string path, string label, long maxMessageBytes, Action<MailFinding> report)
    {
        var length = new FileInfo(path).Length;
        if (length == 0 || length > maxMessageBytes || Bytes + length > MaxTemporaryBytes)
        {
            if (Bytes + length > MaxTemporaryBytes) Truncated = true;
            File.Delete(path); Notice(report, label, "Copia vacía o por encima del límite de mensaje o de temporales (512 MB); no analizada."); return false;
        }
        Bytes += length; Sources[path] = label; return true;
    }
    internal MailFinding Map(MailFinding finding) => Sources.TryGetValue(finding.Path, out var source)
        ? finding with { Path = source } : finding with { Path = finding.Path == DirectoryPath ? "Buzón importado" : finding.Path };
    internal MailScanSummary Merge(MailScanSummary summary)
    {
        summary.Discovered += Math.Max(Skipped, Seen - Sources.Count);
        summary.Skipped += Skipped; summary.Errors += Errors; summary.Truncated |= Truncated; return summary;
    }
    internal void Cleanup()
    {
        if (_cleaned) return;
        if (Directory.Exists(DirectoryPath)) Directory.Delete(DirectoryPath, true);
        _cleaned = true;
    }
    public void Dispose() => Cleanup();
}

internal static class LocalMailboxImport
{
    private static readonly Regex Envelope = new(@"^From \S+ (?:(?:Mon|Tue|Wed|Thu|Fri|Sat|Sun) )?(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2} \d{2}:\d{2}(?::\d{2})?(?: [^\r\n]+)? \d{4}(?: .*)?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    internal static void RequireOrdinaryPath(string path)
    {
        for (var current = Path.GetFullPath(path); !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("No se atraviesan enlaces simbólicos ni puntos de reanálisis.");
    }

    internal static void ImportMbox(string path, MailImportSession session, MailScanOptions options,
        Action<MailFinding> report, CancellationToken ct, long inputLimit = 1024L * 1024 * 1024)
    {
        RequireOrdinaryPath(path);
        var before = new FileInfo(path); var originalLength = before.Length; var originalTime = before.LastWriteTimeUtc;
        using var source = new BufferedStream(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), 65536);
        var prefix = new byte[512]; var tail = new byte[65536];
        FileStream? output = null; string? destination = null; string label = path;
        bool started = false, oversize = false, first = true; long readBytes = 0, messageBytes = 0;
        int Read()
        {
            if ((readBytes & 8191) == 0) ct.ThrowIfCancellationRequested();
            if (readBytes >= inputLimit) throw new ImportReadLimitException();
            var b = source.ReadByte(); if (b >= 0) readBytes++; return b;
        }
        void Write(ReadOnlySpan<byte> bytes)
        {
            messageBytes += bytes.Length;
            if (messageBytes > options.MaxMessageMb * 1048576L || session.Bytes + messageBytes > MailImportSession.MaxTemporaryBytes)
            {
                if (session.Bytes + messageBytes > MailImportSession.MaxTemporaryBytes) session.Truncated = true;
                if (!oversize) { output?.Dispose(); output = null; if (destination is not null) File.Delete(destination); }
                oversize = true; return;
            }
            output?.Write(bytes);
        }
        void Finish()
        {
            if (!started) return;
            output?.Dispose(); output = null;
            if (oversize) session.Notice(report, label, "Mensaje MBOX por encima del límite; no analizado.");
            else if (destination is not null) session.Accept(destination, label, options.MaxMessageMb * 1048576L, report);
            destination = null; started = false; oversize = false;
        }
        try
        {
            while (true)
            {
                int count = 0, b = -1;
                while (count < prefix.Length && (b = Read()) >= 0) { prefix[count++] = (byte)b; if (b == '\n') break; }
                if (count == 0) break;
                var start = first && count >= 3 && prefix[0] == 239 && prefix[1] == 187 && prefix[2] == 191 ? 3 : 0;
                var isEnvelope = count < prefix.Length && Envelope.IsMatch(Encoding.ASCII.GetString(prefix, start, count - start).TrimEnd('\r', '\n'));
                first = false;
                if (isEnvelope)
                {
                    Finish();
                    if (session.Seen >= options.MaxMessages || session.Truncated)
                    { session.Truncated = true; session.Notice(report, path, "Límite de importación alcanzado; quedan mensajes sin analizar."); break; }
                    session.Seen++; label = path + " [MBOX #" + session.Seen + "]";
                    started = true; oversize = false; messageBytes = 0;
                    destination = session.NextPath(".eml"); output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    continue;
                }
                if (!started) throw new InvalidDataException("No se reconoce un buzón MBOX con separadores From válidos. Exporta a EML si el formato es diferente.");
                Write(prefix.AsSpan(0, count));
                if (count == prefix.Length && prefix[count - 1] != '\n')
                {
                    int n = 0;
                    while ((b = Read()) >= 0)
                    {
                        tail[n++] = (byte)b;
                        if (n == tail.Length || b == '\n') { Write(tail.AsSpan(0, n)); n = 0; }
                        if (b == '\n') break;
                    }
                    if (n > 0) Write(tail.AsSpan(0, n));
                }
            }
            Finish();
            if (session.Seen == 0) session.Notice(report, path, "El buzón no contiene mensajes reconocibles.");
        }
        catch (ImportReadLimitException)
        {
            output?.Dispose(); output = null;
            if (destination is not null) File.Delete(destination);
            session.Truncated = true; session.Notice(report, path, "Se alcanzó el límite de lectura MBOX (1 GB); el análisis es incompleto.");
        }
        finally { output?.Dispose(); }
        var after = new FileInfo(path);
        if (after.Length != originalLength || after.LastWriteTimeUtc != originalTime)
            session.Notice(report, path, "El buzón cambió durante la copia; repite con el cliente de correo cerrado.", true);
    }

    internal static void ImportMaildir(string path, MailImportSession session, MailScanOptions options,
        Action<MailFinding> report, CancellationToken ct)
    {
        RequireOrdinaryPath(path);
        var pending = new Stack<(string Path, int Depth)>(); pending.Push((path, 0));
        var directories = 0; var hasMaildir = false;
        while (pending.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var (folder, depth) = pending.Pop();
            if (++directories > 2048) { session.Truncated = true; session.Notice(report, path, "Se alcanzó el límite de carpetas Maildir."); break; }
            try
            {
                foreach (var name in new[] { "cur", "new" })
                {
                    var messages = Path.Combine(folder, name);
                    if (!Directory.Exists(messages)) continue;
                    RequireOrdinaryPath(messages); hasMaildir = true;
                    foreach (var file in Directory.EnumerateFiles(messages))
                    {
                        ct.ThrowIfCancellationRequested();
                        if (session.Seen >= options.MaxMessages || session.Truncated)
                        { session.Truncated = true; session.Notice(report, path, "Límite de importación alcanzado; quedan mensajes sin analizar."); return; }
                        session.Seen++;
                        CopyMessage(file, session, options, report, ct);
                    }
                }
                if (!options.IncludeSubfolders) continue;
                foreach (var sub in Directory.EnumerateDirectories(folder))
                {
                    var name = Path.GetFileName(sub);
                    if (name is "cur" or "new" or "tmp") continue;
                    if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) != 0)
                    { session.Notice(report, sub, "Enlace de carpeta omitido; no analizado."); continue; }
                    if (depth >= 8) { session.Truncated = true; session.Notice(report, sub, "Límite de profundidad Maildir."); continue; }
                    pending.Push((sub, depth + 1));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { session.Notice(report, folder, "No se pudo leer la carpeta Maildir: " + ex.Message, true); }
        }
        if (!hasMaildir) session.Notice(report, path, "No se encontraron las carpetas cur/new de Maildir.", true);
    }

    private static void CopyMessage(string source, MailImportSession session, MailScanOptions options,
        Action<MailFinding> report, CancellationToken ct)
    {
        var destination = session.NextPath(".eml");
        try
        {
            RequireOrdinaryPath(source); var before = new FileInfo(source);
            var length = before.Length; var time = before.LastWriteTimeUtc;
            var max = options.MaxMessageMb * 1048576L;
            if (length > max || session.Bytes + length > MailImportSession.MaxTemporaryBytes)
            { session.Truncated |= session.Bytes + length > MailImportSession.MaxTemporaryBytes; session.Notice(report, source, "Mensaje por encima del límite de importación; no analizado."); return; }
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[65536]; int count; long total = 0;
                while ((count = input.Read(buffer)) > 0)
                {
                    ct.ThrowIfCancellationRequested(); total += count;
                    if (total > max || session.Bytes + total > MailImportSession.MaxTemporaryBytes) throw new IOException("La copia supera el límite configurado.");
                    output.Write(buffer, 0, count);
                }
            }
            var after = new FileInfo(source);
            if (after.Length != length || after.LastWriteTimeUtc != time) throw new IOException("El mensaje cambió durante la copia.");
            session.Accept(destination, source, max, report);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { if (File.Exists(destination)) File.Delete(destination); session.Notice(report, source, "Mensaje no importado: " + ex.Message, true); }
    }
    private sealed class ImportReadLimitException : IOException { }
}
