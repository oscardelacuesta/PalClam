// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using MimeKit;
using MsgReader.Outlook;

namespace PalClam.Services;

/// <summary>
/// Análisis local de mensajes exportados. No abre Outlook, buzones PST/OST,
/// enlaces, adjuntos ni servicios externos. Nunca modifica los originales.
/// </summary>
internal static class MailScanService
{
    private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase)
    { ".eml", ".msg", ".oft" };

    internal static bool IsSupported(string path) => Supported.Contains(Path.GetExtension(path));

    internal static async Task<MailScanSummary> ScanAsync(string path, string scanner, string database,
        MailScanOptions options, Action<MailFinding> report, Action<int, int> progress, CancellationToken ct)
    {
        if (!ClamAvLocator.IsValidScanner(scanner))
            throw new FileNotFoundException("El motor ClamAV no está disponible.");
        if (!SignatureStore.HasSignatures())
            throw new InvalidOperationException("No hay firmas antivirus. Actualízalas antes de analizar correo.");

        var summary = new MailScanSummary();
        var files = new List<string>();
        if (File.Exists(path))
        {
            if (!IsSupported(path)) throw new ArgumentException("Selecciona un archivo .eml, .msg o .oft.");
            files.Add(path);
        }
        else if (Directory.Exists(path))
        {
            var e = new EnumerationOptions { RecurseSubdirectories = options.IncludeSubfolders,
                IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint };
            foreach (var f in Directory.EnumerateFiles(path, "*", e))
            {
                ct.ThrowIfCancellationRequested();
                if (!IsSupported(f)) continue;
                if (files.Count >= options.MaxMessages)
                {
                    summary.Truncated = true;
                    break;
                }
                files.Add(f);
            }
        }
        else throw new FileNotFoundException("No existe el archivo o directorio seleccionado.");

        summary.Discovered = files.Count;
        progress(0, files.Count);
        if (summary.Truncated) report(new("Límite", path, "", "Se alcanzó el máximo de mensajes configurado; hay mensajes sin analizar.", ""));
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var subject = "";
            var hasIncidents = false;
            string? temp = null;
            var attachments = new List<AttachmentFile>();
            try
            {
                var size = new FileInfo(file).Length;
                if (size > options.MaxMessageMb * 1024L * 1024L)
                {
                    summary.Skipped++;
                    report(new("Omitido", file, "", $"Mensaje de más de {options.MaxMessageMb} MB: no examinado.", subject));
                    continue;
                }
                if (options.ExtractAttachments)
                {
                    temp = Path.Combine(AppPaths.PrivateTemp, "PalClam-Mail-" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(temp);
                    try
                    {
                        if (Path.GetExtension(file).Equals(".eml", StringComparison.OrdinalIgnoreCase))
                            ExtractEml(file, temp, options, attachments, out subject);
                        else ExtractMsg(file, temp, options, attachments, out subject);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // El análisis del original sigue siendo necesario aunque falle el parser.
                        hasIncidents = true;
                        summary.Errors++;
                        report(new("Incompleto", file, "", "Error extrayendo adjuntos: " + ex.Message +
                            ". Se analizará el mensaje original, pero los adjuntos pueden quedar sin examinar.", subject));
                    }
                    foreach (var a in attachments.Where(a => a.Status != "Preparado"))
                    {
                        hasIncidents = true;
                        summary.Skipped++;
                        report(new("Omitido", file, a.Name, a.Status, subject));
                    }
                }
                var targets = new List<string> { file };
                targets.AddRange(attachments.Where(a => a.Status == "Preparado").Select(a => a.Path));
                var map = attachments.Where(a => a.Status == "Preparado")
                    .ToDictionary(a => a.Path, a => a.Name, StringComparer.OrdinalIgnoreCase);
                var args = new List<string> { "--stdout", "--database=" + database,
                    "--scan-mail=yes", "--scan-archive=" + (options.ScanArchives ? "yes" : "no"),
                    "--phishing-sigs=" + (options.PhishingHeuristics ? "yes" : "no"),
                    "--phishing-scan-urls=" + (options.PhishingHeuristics ? "yes" : "no") };
                if (options.DetectPua) args.Add("--detect-pua=yes");
                args.AddRange(targets);
                var found = 0;
                var errors = 0;
                var evaluated = 0;
                var result = await ProcessRunner.RunAsync(scanner, args, (line, stderr) =>
                {
                    var entry = ScanOutputParser.ParseFile(line);
                    if (entry is not null)
                    {
                        Interlocked.Increment(ref evaluated);
                        var display = map.TryGetValue(entry.FilePath, out var name) ? name : "Mensaje completo";
                        if (entry.Status == ScanFileStatus.Detected)
                        {
                            Interlocked.Increment(ref found);
                            report(new("DETECCIÓN", file, display, entry.Detail, subject));
                        }
                        else if (entry.Status == ScanFileStatus.Error)
                        {
                            Interlocked.Increment(ref errors);
                            report(new("Error", file, display, entry.Detail, subject));
                        }
                        else if (display != "Mensaje completo")
                            report(new("Adjunto OK", file, display, "Sin detecciones notificadas", subject));
                    }
                    else if (stderr || line.Contains("ERROR:", StringComparison.OrdinalIgnoreCase))
                    {
                        Interlocked.Increment(ref errors);
                        report(new("Error", file, "", line, subject));
                    }
                }, ct);
                summary.AttachmentsAnalyzed += attachments.Count(a => a.Status == "Preparado");
                summary.Detections += Math.Max(found, result.ExitCode == 1 ? 1 : 0);
                summary.Errors += errors;
                if (options.CheckLinks)
                {
                    // Solo se analiza texto local; nunca se visita ninguna URL.
                    var hints = MailLinkInspector.Check(file);
                    foreach (var hint in hints)
                        report(new("Aviso enlace", file, "", hint, subject));
                    summary.LinkWarnings += hints.Count;
                }
                if (result.ExitCode > 1 || errors > 0 || evaluated < targets.Count || hasIncidents)
                {
                    hasIncidents = true;
                    summary.Errors++;
                    report(new("Incompleto", file, "", $"Resultado no concluyente: código ClamAV {result.ExitCode}; respuestas {evaluated}/{targets.Count}.", subject));
                }
                if (found == 0 && !hasIncidents && result.ExitCode == 0)
                    report(new("Mensaje OK", file, "", "Finalizado sin detecciones notificadas (no garantiza ausencia de amenazas).", subject));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                summary.Errors++;
                report(new("Error", file, "", "No se pudo completar el mensaje: " + ex.Message, subject));
            }
            finally
            {
                summary.Processed++;
                progress(summary.Processed, files.Count);
                if (temp is not null)
                {
                    try { Directory.Delete(temp, recursive: true); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        summary.Errors++;
                        report(new("Error", file, "", "No se pudieron borrar los temporales: " + temp, subject));
                    }
                }
            }
        }
        return summary;
    }

    private static void ExtractEml(string file, string temp, MailScanOptions options,
        List<AttachmentFile> output, out string subject)
    {
        using var input = File.OpenRead(file);
        var message = MimeMessage.Load(input);
        subject = SafeLabel(message.Subject, 120);
        var index = 0;
        foreach (var entity in message.BodyParts)
        {
            if (entity is not MimePart part) continue;
            if (!part.IsAttachment && !(options.IncludeInline &&
                !part.ContentType.MediaType.Equals("text", StringComparison.OrdinalIgnoreCase))) continue;
            if (++index > 100) { output.Add(new("—", "", "Se superó el límite de 100 partes MIME.")); break; }
            var name = SafeLabel(part.FileName ?? "adjunto-" + index, 120);
            var dest = Path.Combine(temp, Guid.NewGuid().ToString("N") + SafeExtension(name));
            // Los mensajes MIME pueden declarar una parte sin contenido real.
            // No informar de "limpio" cuando un adjunto no se ha podido extraer.
            var content = part.Content;
            if (content is null)
            {
                output.Add(new(name, "", "Parte MIME sin contenido decodificable; no analizada."));
                continue;
            }
            try
            {
                using var stream = new FileStream(dest, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                using var bounded = new BoundedWriteStream(stream, options.MaxAttachmentMb * 1024L * 1024L);
                content.DecodeTo(bounded);
            }
            catch (AttachmentTooLargeException)
            {
                File.Delete(dest);
                output.Add(new(name, "", "Adjunto por encima del límite configurado."));
                continue;
            }
            output.Add(new(name, dest, "Preparado"));
        }
    }

    private static void ExtractMsg(string file, string temp, MailScanOptions options,
        List<AttachmentFile> output, out string subject)
    {
        // MSGReader 6.2.1 no resuelve referencias externas automáticamente.
        // Nunca llamar a ResolveExternalReference ni abrir rutas declaradas por el correo.
        using var message = new Storage.Message(file);
        subject = SafeLabel(message.Subject, 120);
        var index = 0;
        foreach (var item in message.Attachments)
        {
            if (++index > 100) { output.Add(new("—", "", "Se superó el límite de 100 adjuntos MSG.")); break; }
            if (item is not Storage.Attachment attachment)
            {
                output.Add(new("Adjunto incrustado", "", "Mensaje incrustado o tipo no soportado: no extraído."));
                continue;
            }
            var name = SafeLabel(attachment.FileName ?? "adjunto-" + index, 120);
            var data = attachment.Data;
            if (data is null)
            {
                output.Add(new(name, "", "Adjunto externo o sin datos: no examinado."));
                continue;
            }
            if (data.LongLength > options.MaxAttachmentMb * 1024L * 1024L)
            {
                output.Add(new(name, "", "Adjunto por encima del límite configurado."));
                continue;
            }
            var dest = Path.Combine(temp, Guid.NewGuid().ToString("N") + SafeExtension(name));
            File.WriteAllBytes(dest, data);
            output.Add(new(name, dest, "Preparado"));
        }
    }

    private static string SafeExtension(string name)
    {
        var ext = Path.GetExtension(name);
        return ext.Length is > 0 and <= 12 && ext.Skip(1).All(char.IsLetterOrDigit) ? ext : ".bin";
    }
    private static string SafeLabel(string? text, int maximum) =>
        new string((text ?? "").Where(c => !char.IsControl(c)).Take(maximum).ToArray());

    private sealed record AttachmentFile(string Name, string Path, string Status);
}

/// <summary>Advertencias heurísticas locales; nunca equivalen a una detección de phishing.</summary>
internal static class MailLinkInspector
{
    private static readonly Regex Urls = new("https?://[^\\s\\\"'<>]+", RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

    public static IReadOnlyList<string> Check(string file)
    {
        try
        {
            string text;
            if (Path.GetExtension(file).Equals(".eml", StringComparison.OrdinalIgnoreCase))
            {
                using var stream = File.OpenRead(file);
                var msg = MimeMessage.Load(stream);
                text = (msg.HtmlBody ?? "") + "\n" + (msg.TextBody ?? "");
            }
            else
            {
                using var msg = new Storage.Message(file);
                text = (msg.BodyHtml ?? "") + "\n" + (msg.BodyText ?? "");
            }
            if (text.Length > 750_000) text = text[..750_000];
            var alerts = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in Urls.Matches(text))
            {
                if (alerts.Count >= 5) break;
                if (!Uri.TryCreate(match.Value.TrimEnd('.', ',', ')', ';'), UriKind.Absolute, out var uri)) continue;
                if (IPAddress.TryParse(uri.DnsSafeHost, out _)) alerts.Add("Enlace con dirección IP en lugar de dominio (revisión manual). ");
                if (uri.DnsSafeHost.StartsWith("xn--", StringComparison.OrdinalIgnoreCase) ||
                    uri.DnsSafeHost.Contains(".xn--", StringComparison.OrdinalIgnoreCase))
                    alerts.Add("Enlace con dominio internacionalizado (punycode); revisar antes de abrir.");
                if (!string.IsNullOrEmpty(uri.UserInfo)) alerts.Add("Enlace que incluye credenciales en la URL.");
            }
            return alerts.ToList();
        }
        catch (Exception) { return Array.Empty<string>(); }
    }
}

internal sealed class AttachmentTooLargeException : IOException { }

internal sealed class BoundedWriteStream : Stream
{
    private readonly Stream _inner;
    private readonly long _limit;
    private long _written;
    public BoundedWriteStream(Stream inner, long limit) { _inner = inner; _limit = limit; }
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => _written;
    public override long Position { get => _written; set => throw new NotSupportedException(); }
    public override void Write(byte[] buffer, int offset, int count)
    {
        if (_written + count > _limit) throw new AttachmentTooLargeException();
        _inner.Write(buffer, offset, count);
        _written += count;
    }
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (_written + buffer.Length > _limit) throw new AttachmentTooLargeException();
        _inner.Write(buffer); _written += buffer.Length;
    }
    public override void Flush() => _inner.Flush();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing) { if (disposing) _inner.Dispose(); base.Dispose(disposing); }
}
