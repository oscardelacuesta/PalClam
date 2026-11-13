// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MimeKit;
using PalClam.Services;

internal static class Program
{
    private static int _checks;
    private static void Check(bool valid, string label)
    { if (!valid) throw new Exception(label); _checks++; Console.WriteLine("OK " + label); }
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static MailScanOptions Options(int max = 50) => new(false, true, true, true, true, true, false, 1, 1, max);
    private static readonly string Eml = "From: sender@example.test\r\nTo: recipient@example.test\r\nSubject: ../../unsafe-subject\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nMessage body\r\n";
    private static string Envelope(int day) => $"From sender@example.test Thu Nov {day:00} 10:00:00 2026\n";
    private static async Task Main()
    {
        var now = DateTimeOffset.UtcNow; var options = new DiskLedOptions { MinFreeGb = 0 };
        var idle = new DiskActivitySnapshot(true, 0, 0, true, 90_000, 100_000, "C:\\", now, "");
        Check(DiskLedPolicy.Select(options, idle, false, false) == DiskLedState.Idle, "LED reposo");
        Check(DiskLedPolicy.Select(options, idle with { ReadBytesPerSecond = 20000 }, false, false) == DiskLedState.Reading, "LED lectura");
        Check(DiskLedPolicy.Select(options, idle with { WriteBytesPerSecond = 20000 }, false, false) == DiskLedState.Writing, "LED escritura");
        Check(DiskLedPolicy.Select(options, idle with { ReadBytesPerSecond = 20000, WriteBytesPerSecond = 20000 }, false, false) == DiskLedState.ReadWrite, "LED lectura/escritura simultáneas");
        Check(DiskLedPolicy.Select(options, idle, true, false) == DiskLedState.Scanning, "LED análisis");
        Check(DiskLedPolicy.Select(options, idle with { FreeBytes = 100 }, true, true) == DiskLedState.Threat, "LED detección prevalece sobre espacio y análisis");
        Check(DiskLedPolicy.Select(options, idle with { FreeBytes = 5000 }, true, false) == DiskLedState.LowSpace, "LED espacio prevalece sobre análisis, umbral inclusivo");
        Check(DiskLedPolicy.Select(options, idle with { SpaceAvailable = false, FreeBytes = 0 }, false, false) == DiskLedState.Idle, "LED no inventa disco lleno sin muestra");
        Check(DiskLedPolicy.Select(options, idle with { IoAvailable = false }, false, false) == DiskLedState.Unavailable, "LED sin contadores no equivale a reposo");
        options.ShowAntivirusStates = false;
        Check(DiskLedPolicy.Select(options, idle, true, true) == DiskLedState.Idle, "LED permite desactivar estados antivirus");
        options.ShowAntivirusStates = true; options.MinFreePercent = 0; options.MinFreeGb = 2;
        Check(DiskLedPolicy.Select(options, idle, false, false) == DiskLedState.LowSpace, "LED umbral absoluto GB");
        var invalid = new DiskLedOptions { IntervalMs = -1, ActivityThresholdKb = -7, DriveRoot = "\\\\host\\share", Colors = new() { ["Threat"] = "bad" } };
        var safe = DiskLedOptions.Normalize(invalid);
        Check(safe.IntervalMs == 500 && safe.ActivityThresholdKb == 0 && safe.DriveRoot == "auto" && safe.Colors.Count == 8 && safe.Colors["Threat"] == "#FF475A", "LED normaliza ajustes corruptos sin perder colores");
        Check(invalid.Colors["Threat"] == "bad", "normalización LED no modifica el objeto original");
        safe.Colors["Reading"] = "#abcdef"; safe.DriveRoot = "d:\\";
        safe = DiskLedOptions.Normalize(JsonSerializer.Deserialize<DiskLedOptions>(JsonSerializer.Serialize(safe)));
        Check(safe.Colors["Reading"] == "#ABCDEF" && safe.DriveRoot == "D:\\", "persistencia LED y normalización de unidad");
        using (var sampler = new DiskActivitySampler(safe))
        { if (!OperatingSystem.IsWindows()) Check(!sampler.Sample().IoAvailable, "PDH no se ejecuta en plataforma incompatible"); }
        var rw = new RansomwareOptions { MinFiles = 5, MinSignals = 2, WindowSeconds = 5, WarnMassActivity = false };
        var policy = new RansomwarePolicy(rw);
        var duplicateSafe = true;
        for (var i = 0; i < 100; i++) duplicateSafe &= policy.Observe(new(now, "one.txt", "", FileActivityKind.Changed, true)) is null;
        Check(duplicateSafe, "100 eventos duplicados no simulan muchos archivos");
        Check(policy.TrackedCount == 1, "contador de archivos distintos");
        RansomwareAlert? found = null;
        for (var i = 0; i < 5; i++) found ??= policy.Observe(new(now, "file" + i + ".txt.locked", "file" + i + ".txt", FileActivityKind.Renamed, false));
        Check(found?.Kind == "suspected", "correlación de renombrado compatible con cifrado");
        Check(policy.Observe(new(now.AddSeconds(1), "more.locked", "more.txt", FileActivityKind.Renamed, false)) is null, "enfriamiento evita notificaciones repetidas");
        policy = new(rw);
        for (var i = 0; i < 4; i++) policy.Observe(new(now, "old" + i, "", FileActivityKind.Changed, true));
        Check(policy.Observe(new(now.AddSeconds(6), "new", "", FileActivityKind.Changed, true)) is null && policy.TrackedCount == 1, "ventana temporal descarta eventos caducados");
        policy = new(rw);
        policy.Observe(new(now, "a.txt", "", FileActivityKind.Changed, false));
        policy.Observe(new(now, "a.txt.locked", "a.txt", FileActivityKind.Renamed, false));
        Check(policy.TrackedCount == 1, "renombrar conserva identidad y no duplica el archivo");
        policy = new(withMass());
        for (var i = 0; i < 5; i++) found = policy.Observe(new(now, "deleted" + i, "", FileActivityKind.Deleted, false)) ?? found;
        Check(found?.Kind == "deletion", "eliminación masiva se distingue de sospecha de cifrado");
        policy = new(rw);
        for (var i = 0; i < 5; i++) found = policy.Observe(new(now, "random" + i + ".txt", "", FileActivityKind.Changed, true)) ?? (i == 0 ? null : found);
        Check(found?.Kind == "suspected", "contenido aleatorio correlacionado en varios archivos");
        Check(RansomwarePolicy.Entropy(new byte[65536]) == 0, "entropía de datos repetidos");
        var uniform = Enumerable.Range(0, 65536).Select(n => (byte)n).ToArray();
        Check(Math.Abs(RansomwarePolicy.Entropy(uniform) - 8) < 0.00001, "entropía de distribución uniforme");
        Check(RansomwarePolicy.PlainTextExtension("report.csv") && !RansomwarePolicy.PlainTextExtension("archive.zip"), "ZIP y documentos comprimidos no se tratan como texto cifrado");
        Check(!RansomwarePolicy.SuspiciousName("report-new.txt", "report.txt") && RansomwarePolicy.SuspiciousName("report.txt.unknown", "report.txt"), "distingue renombrado ordinario y extensión añadida");
        Check(RansomwareMonitor.IsUnder(Path.Combine(Path.GetTempPath(), "data", "x"), Path.Combine(Path.GetTempPath(), "data")) &&
            !RansomwareMonitor.IsUnder(Path.Combine(Path.GetTempPath(), "data-other"), Path.Combine(Path.GetTempPath(), "data")), "exclusiones respetan límites de directorio");
        var normalized = RansomwareOptions.Normalize(new() { MinFiles = -1, MinSignals = 500, WindowSeconds = 0, Folders = null! });
        Check(normalized.MinFiles == 5 && normalized.MinSignals == 5 && normalized.WindowSeconds == 5 && normalized.Folders.Count == 0 && !normalized.StartWithApp, "ajustes ransomware corruptos y monitor desactivado por defecto");

        var notice = SecurityNoticeFeed.ParseRansomware(JsonSerializer.Serialize(new { When = now, Kind = "suspected", Detail = "test", ExamplePath = "private.txt" }));
        Check(notice?.Level == SecurityNoticeLevel.Warning && notice.Source == "ransomware", "sospecha de ransomware no se presenta como detección ClamAV");
        Check(SecurityNoticeFeed.ParseRansomware("{invalid") is null && SecurityNoticeFeed.ParseRansomware(JsonSerializer.Serialize(new { When = now, Kind = "unexpected" })) is null,
            "registros corruptos o con categoría desconocida se rechazan");
        Check(notice is not null && SecurityNoticeFeed.Filter(new[] { notice }, "Advertencia", "Anti-ransomware", "private.txt", false, DateTimeOffset.MinValue).Count == 1,
            "historial filtra avisos anti-ransomware y conserva rutas como datos");
        var root = Path.Combine(Path.GetTempPath(), "PalClam-FeatureTest-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            var mbox = Path.Combine(root, "Inbox");
            File.WriteAllText(mbox, Envelope(12) + Eml + "From this is body text\n>From sender@example.test Thu Nov 12 10:00:00 2026\n" + Envelope(13) + Eml, new UTF8Encoding(false));
            var hash = Hash(mbox); var notices = new List<MailFinding>();
            string temp;
            using (var import = new MailImportSession())
            {
                temp = import.DirectoryPath; LocalMailboxImport.ImportMbox(mbox, import, Options(), notices.Add, default);
                Check(import.Sources.Count == 2 && import.Seen == 2, "MBOX sin extensión importa dos mensajes");
                Check(File.ReadAllText(import.Sources.Keys.First()).Contains("From this is body text"), "From del cuerpo no se interpreta como separador");
                Check(File.ReadAllText(import.Sources.Keys.First()).Contains(">From sender"), "conserva bytes del cuerpo MBOX escapado");
                foreach (var file in import.Sources.Keys)
                {
                    using var msg = MimeMessage.Load(file);
                    Check(msg.Subject == "../../unsafe-subject", "asunto opaco no se convierte en ruta");
                    Check(Path.GetDirectoryName(file) == import.DirectoryPath, "copias confinadas a la sesión");
                }
                var mapped = import.Map(new("Mensaje OK", import.Sources.Keys.First(), "", "OK", ""));
                Check(mapped.Path.StartsWith(mbox) && mapped.Path.Contains("#1"), "resultados conservan origen y número del buzón");
            }
            Check(!Directory.Exists(temp), "limpieza del importador en éxito");
            Check(Hash(mbox) == hash, "MBOX original permanece byte a byte");
            using (var import = new MailImportSession())
            {
                LocalMailboxImport.ImportMbox(mbox, import, Options(1), notices.Add, default);
                Check(import.Sources.Count == 1 && import.Truncated, "máximo de mensajes MBOX produce resultado incompleto");
                var sum = import.Merge(new MailScanSummary { Discovered = 1, Processed = 1 });
                Check(sum.Skipped > 0 && sum.Truncated && sum.Discovered > sum.Processed, "las omisiones de importación llegan al resumen antivirus");
            }
            var large = Path.Combine(root, "large.mbox"); File.WriteAllText(large, Envelope(12) + Eml + new string('A', 1048577) + "\n" + Envelope(13) + Eml);
            using (var import = new MailImportSession())
            {
                LocalMailboxImport.ImportMbox(large, import, Options(), notices.Add, default);
                Check(import.Sources.Count == 1 && import.Skipped == 1, "línea MBOX grande sin asignación ilimitada; sigue al próximo mensaje");
            }
            using (var import = new MailImportSession())
            {
                LocalMailboxImport.ImportMbox(large, import, Options(), notices.Add, default, 1024);
                Check(import.Truncated && import.Sources.Count == 0, "límite de lectura descarta el mensaje parcial");
            }
            using (var import = new MailImportSession())
            {
                var bad = Path.Combine(root, "bad.mbox"); File.WriteAllText(bad, Eml);
                try { LocalMailboxImport.ImportMbox(bad, import, Options(), notices.Add, default); throw new Exception("MBOX inválido aceptado"); }
                catch (InvalidDataException) { Check(import.Sources.Count == 0, "formato desconocido no se declara analizado"); }
            }
            var md = Path.Combine(root, "maildir"); Directory.CreateDirectory(Path.Combine(md, "cur")); Directory.CreateDirectory(Path.Combine(md, "new")); Directory.CreateDirectory(Path.Combine(md, "tmp"));
            // ':' is a Maildir flag separator on Unix, but opens an NTFS alternate data stream on Windows.
            // The importer treats message names as opaque: use a regular Windows filename for its fixture.
            var currentName = "1730.M123!2,S";
            var current = Path.Combine(md, "cur", currentName); File.WriteAllText(current, Eml);
            var incoming = Path.Combine(md, "new", "1731.M234"); File.WriteAllText(incoming, Eml);
            File.WriteAllText(Path.Combine(md, "tmp", "temporary"), Eml);
            var nestedTemporary = Path.Combine(md, "tmp", "not-delivered", "cur"); Directory.CreateDirectory(nestedTemporary);
            File.WriteAllText(Path.Combine(nestedTemporary, "temporary-nested"), Eml);
            Check(Directory.EnumerateFiles(Path.Combine(md, "cur")).Contains(current, StringComparer.Ordinal) &&
                new FileInfo(current).Length == Encoding.UTF8.GetByteCount(Eml),
                "muestra Maildir es un archivo normal enumerado con contenido, no un flujo NTFS");
            hash = Hash(current);
            using (var import = new MailImportSession())
            {
                LocalMailboxImport.ImportMaildir(md, import, Options(), notices.Add, default);
                Check(import.Sources.Count == 2 && import.Sources.Values.All(p => !p.Contains("temporary")), "Maildir sin extensión: cur/new sí, tmp nunca");
                Check(import.Sources.Values.ToHashSet(StringComparer.Ordinal).SetEquals(new[] { current, incoming }) &&
                    import.Seen == 2 && import.Errors == 0 && import.Skipped == 0,
                    "Maildir importa exactamente los dos mensajes y excluye subcarpetas de tmp");
                Check(import.Sources.All(pair => Hash(pair.Key) == Hash(pair.Value)),
                    "Maildir copia íntegros los bytes de cur/new y conserva su origen");
            }
            Check(Hash(current) == hash, "Maildir conserva contenido y nombre del original");
            if (!OperatingSystem.IsWindows())
            {
                var unixCurrent = Path.Combine(md, "cur", "1730.M123:2,S");
                File.Move(current, unixCurrent);
                using (var unixImport = new MailImportSession())
                {
                    LocalMailboxImport.ImportMaildir(md, unixImport, Options(), notices.Add, default);
                    Check(unixImport.Sources.Count == 2 && unixImport.Sources.Values.Contains(unixCurrent) && Hash(unixCurrent) == hash,
                        "Maildir Unix conserva los dos puntos y flags donde son un nombre válido");
                }
                File.Move(unixCurrent, current);
                File.CreateSymbolicLink(Path.Combine(md, "new", "link"), current);
                using var import = new MailImportSession(); LocalMailboxImport.ImportMaildir(md, import, Options(), notices.Add, default);
                Check(import.Sources.Count == 2 && import.Errors > 0, "Maildir no sigue enlaces simbólicos");
            }
            string cancelTemp;
            using (var import = new MailImportSession())
            {
                cancelTemp = import.DirectoryPath; using var cancel = new CancellationTokenSource(); cancel.Cancel();
                try { LocalMailboxImport.ImportMbox(mbox, import, Options(), notices.Add, cancel.Token); throw new Exception("Cancelación ignorada"); }
                catch (OperationCanceledException) { Check(true, "cancelación detiene la importación"); }
            }
            Check(!Directory.Exists(cancelTemp), "cancelación permite eliminar los temporales");
            var watched = Path.Combine(root, "watch"); Directory.CreateDirectory(watched);
            for (var i = 0; i < 6; i++) File.WriteAllText(Path.Combine(watched, i + ".txt"), "synthetic test data");
            var notification = new TaskCompletionSource<RansomwareAlert>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var monitor = new RansomwareMonitor(new() { Folders = new() { watched }, MinFiles = 5, MinSignals = 2, SampleEntropy = false, WarnMassActivity = false },
                a => notification.TrySetResult(a), _ => { }))
            {
                monitor.Start(); Check(monitor.IsRunning, "monitor de carpetas arranca");
                for (var i = 0; i < 6; i++) File.Move(Path.Combine(watched, i + ".txt"), Path.Combine(watched, i + ".txt.locked"));
                var alert = await notification.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Check(alert.Kind == "suspected" && alert.Files >= 5, "eventos reales del sistema de archivos llegan al detector sin ransomware");
            }
            var entropyFolder = Path.Combine(root, "entropy"); Directory.CreateDirectory(entropyFolder);
            for (var i = 0; i < 6; i++) File.WriteAllText(Path.Combine(entropyFolder, i + ".txt"), "synthetic plain text");
            var entropyNotice = new TaskCompletionSource<RansomwareAlert>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var monitor = new RansomwareMonitor(new() { Folders = new() { entropyFolder }, MinFiles = 5, MinSignals = 2, SampleEntropy = true, WarnMassActivity = false },
                a => entropyNotice.TrySetResult(a), _ => { }))
            {
                monitor.Start();
                for (var i = 0; i < 6; i++) File.WriteAllBytes(Path.Combine(entropyFolder, i + ".txt"), uniform);
                var alert = await entropyNotice.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Check(alert.Kind == "suspected" && alert.HighEntropy >= 2, "muestreo real de archivos sintéticos correlaciona alta entropía sin cambiar extensiones");
            }
        }
        finally { Directory.Delete(root, true); }
        Console.WriteLine($"PASS {_checks} verificaciones. No ejecuta Outlook, WPF, PDH ni malware real.");
        RansomwareOptions withMass() => new() { MinFiles = 5, MinSignals = 2, WindowSeconds = 5, WarnMassActivity = true };
    }
}
