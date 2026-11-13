// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private ComboBox _mailSource = null!;
    private TextBox _mailPathBox = null!;
    private TextBlock _mailStatus = null!;
    private TextBlock _mailCounters = null!;
    private ProgressBar _mailProgress = null!;
    private Button _mailStartButton = null!;
    private Button _mailStopButton = null!;
    private CheckBox _mailSubfolders = null!;
    private CheckBox _mailAttachments = null!;
    private CheckBox _mailInline = null!;
    private CheckBox _mailPhishing = null!;
    private CheckBox _mailLinks = null!;
    private CheckBox _mailAutoReport = null!;
    private ComboBox _mailMaxSize = null!;
    private ComboBox _mailMaxAttach = null!;
    private ComboBox _mailMaxCount = null!;
    private CancellationTokenSource? _mailCancellation;
    private readonly ObservableCollection<MailFinding> _mailFindings = new();
    private MailScanSummary? _lastMailSummary;
    private ComboBox _mailReviewFilter = null!;
    private TextBlock _mailReviewSummary = null!;
    private ICollectionView? _mailItemsView;
    private int _mailResultsCount;
    private int _mailDetections;
    private int _mailErrors;
    private bool _mailActive;

    private void BuildMailPage(StackPanel page)
    {
        page.Children.Add(T("PalClam Mail", 24, Ink, FontWeights.Bold, new Thickness(0, 0, 0, 6)));
        page.Children.Add(T("Analiza mensajes exportados, buzones MBOX, Maildir y una carpeta elegida en Outlook clásico. Para el nuevo Outlook y Microsoft 365 utiliza la conexión Microsoft Graph.",
            12, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 13)));
        var warning = Card(PurplePale, PurplePale);
        var warningStack = new StackPanel();
        warningStack.Children.Add(T("Análisis local y acceso de solo lectura", 14, Ink, FontWeights.SemiBold, new Thickness(0, 0, 0, 5)));
        warningStack.Children.Add(T("Se analizan copias temporales sin modificar los correos originales ni abrir enlaces. Outlook clásico debe estar abierto; su proveedor puede descargar mensajes al copiarlos. Graph requiere autorización y conexión a Microsoft. No se analizan archivos PST/OST directamente: selecciona la carpeta montada en Outlook clásico.", 12, Muted));
        warning.Child = warningStack; page.Children.Add(warning);

        var select = Card(Surface, Outline);
        select.Margin = new Thickness(0, 13, 0, 0);
        var container = new StackPanel();
        container.Children.Add(T("Correo o carpeta a analizar", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 9)));
        container.Children.Add(T("Origen del correo", 12, Ink, FontWeights.SemiBold));
        _mailSource = new ComboBox { ItemsSource = new[] { "Mensajes exportados (EML / MSG / OFT)", "Buzón MBOX (Thunderbird y otros)", "Carpeta Maildir (cur / new)", "Outlook clásico: elegir carpeta abierta" },
            SelectedIndex = 0, Margin = new Thickness(0, 6, 0, 9) };
        container.Children.Add(_mailSource);
        _mailPathBox = Input("Ruta de archivo MBOX, mensaje exportado o carpeta Maildir");
        _mailPathBox.Text = _settings.MailLastPath;
        container.Children.Add(_mailPathBox);
        var chooser = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
        chooser.Children.Add(ActionButton("Elegir correo", false, (_, _) =>
        {
            var dlg = new OpenFileDialog { Title = "Seleccionar mensaje de correo",
                Filter = "Correo y buzones (*.eml;*.msg;*.oft;*.mbox;*.mbx)|*.eml;*.msg;*.oft;*.mbox;*.mbx|Todos los archivos, incluidos MBOX sin extensión (*.*)|*.*" };
            if (PalClam.UI.LocalizedDialogs.Show(dlg, this) == true)
            {
                _mailPathBox.Text = dlg.FileName;
                _mailSource.SelectedIndex = MailScanService.IsSupported(dlg.FileName) ? 0 : 1;
            }
        }));
        chooser.Children.Add(ActionButton("Elegir carpeta", false, (_, _) =>
        {
            var dlg = new OpenFolderDialog { Title = "Carpeta de correos exportados" };
            if (PalClam.UI.LocalizedDialogs.Show(dlg, this) == true) _mailPathBox.Text = dlg.FolderName;
        }));
        chooser.Children.Add(ActionButton("Localizar correo de Thunderbird", false, (_, _) =>
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Thunderbird", "Profiles");
            var dlg = new OpenFileDialog { Title = "Elige el buzón en Mail o ImapMail del perfil de Thunderbird",
                InitialDirectory = Directory.Exists(root) ? root : "", Filter = "Todos los archivos (*.*)|*.*" };
            if (PalClam.UI.LocalizedDialogs.Show(dlg, this) == true) { _mailPathBox.Text = dlg.FileName; _mailSource.SelectedIndex = 1; }
        }));
        chooser.Children.Add(ActionButton("Outlook / Microsoft 365 (Graph)", false, (_, _) => Navigate("outlook")));
        _mailStartButton = ActionButton("✉  Analizar correo", true, async (_, _) => await StartMailScanAsync());
        chooser.Children.Add(_mailStartButton);
        _mailStopButton = ActionButton("■  Detener", false, (_, _) => _mailCancellation?.Cancel());
        _mailStopButton.IsEnabled = false;
        _mailSource.SelectionChanged += (_, _) => _mailPathBox.IsEnabled = _mailSource.SelectedIndex != 3;
        chooser.Children.Add(_mailStopButton);
        container.Children.Add(chooser);
        _mailStatus = T("Selecciona un mensaje o una carpeta. El análisis requiere motor y firmas preparados.",
            12, Muted, FontWeights.Normal, new Thickness(0, 7, 0, 5));
        _mailStatus.TextWrapping = TextWrapping.Wrap;
        container.Children.Add(_mailStatus);
        _mailProgress = new ProgressBar { Minimum = 0, Maximum = 100, Height = 9,
            Value = 0, Foreground = Purple, Background = PurplePale, Margin = new Thickness(0, 8, 0, 9) };
        container.Children.Add(_mailProgress);
        _mailCounters = T("Mensajes: 0/0 · Amenazas: 0 · Errores: 0", 12, Muted,
            FontWeights.SemiBold);
        container.Children.Add(_mailCounters);
        select.Child = container;
        page.Children.Add(select);

        var optionsCard = Card(Surface, Outline);
        optionsCard.Margin = new Thickness(0, 13, 0, 0);
        var options = new StackPanel();
        options.Children.Add(T("Opciones de correo", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 9)));
        _mailSubfolders = OptionCheck("Buscar también en subcarpetas", _settings.MailIncludeSubdirectories,
            "Solo para análisis de una carpeta; nunca atraviesa enlaces simbólicos.", SaveMailOptions);
        _mailAttachments = OptionCheck("Extraer temporalmente y analizar adjuntos",
            _settings.MailExtractAttachments, "Además de analizar el mensaje original con ClamAV.", SaveMailOptions);
        _mailInline = OptionCheck("Incluir partes inline e imágenes incrustadas (.eml)",
            _settings.MailIncludeInline, "Puede aumentar la duración del análisis.", SaveMailOptions);
        _mailPhishing = OptionCheck("Habilitar las heurísticas de phishing de ClamAV",
            _settings.MailPhishingHeuristics, "Firmas y URLs locales de ClamAV: no visita los enlaces.", SaveMailOptions);
        _mailLinks = OptionCheck("Avisar de enlaces con IP, punycode o credenciales",
            _settings.MailCheckLinks, "Heurísticas orientativas; NO son prueba de phishing.", SaveMailOptions);
        _mailAutoReport = OptionCheck("Guardar automáticamente el informe de análisis de correo",
            _settings.MailSaveReport, "Puede contener nombres de archivos y asuntos; desactivado por privacidad.", SaveMailOptions);
        foreach (var chk in new[] { _mailSubfolders, _mailAttachments, _mailInline,
            _mailPhishing, _mailLinks, _mailAutoReport }) options.Children.Add(chk);

        var limits = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        limits.Children.Add(T("Máx. mensaje (MB)", 12, Ink, FontWeights.SemiBold,
            new Thickness(0, 7, 8, 0)));
        _mailMaxSize = MakeMailCombo(new[] { 10, 25, 50, 100 }, _settings.MailMaxMessageMb);
        limits.Children.Add(_mailMaxSize);
        limits.Children.Add(T("Máx. adjunto (MB)", 12, Ink, FontWeights.SemiBold,
            new Thickness(11, 7, 8, 0)));
        _mailMaxAttach = MakeMailCombo(new[] { 5, 10, 25, 50 }, _settings.MailMaxAttachmentMb);
        limits.Children.Add(_mailMaxAttach);
        limits.Children.Add(T("Máx. mensajes", 12, Ink, FontWeights.SemiBold,
            new Thickness(11, 7, 8, 0)));
        _mailMaxCount = MakeMailCombo(new[] { 50, 200, 500 }, _settings.MailMaxMessages);
        limits.Children.Add(_mailMaxCount);
        _mailMaxSize.SelectionChanged += (_, _) => SaveMailOptions();
        _mailMaxAttach.SelectionChanged += (_, _) => SaveMailOptions();
        _mailMaxCount.SelectionChanged += (_, _) => SaveMailOptions();
        options.Children.Add(limits);
        options.Children.Add(T("Los mensajes o adjuntos que excedan los límites se mostrarán como NO ANALIZADOS. " +
            "Los resultados positivos no se moverán automáticamente a cuarentena.", 11, Muted,
            FontWeights.Normal, new Thickness(0, 9, 0, 0)));
        options.Children.Add(T("MBOX: selecciona el archivo del buzón, también sin extensión; no un índice .msf. Maildir: selecciona su carpeta raíz; se leen cur/new y se omite tmp. Cierra el cliente durante la copia. Importación limitada a 512 MB temporales y 1 GB de lectura por MBOX. Outlook clásico permite cancelar entre mensajes, después de cerrar su selector de carpetas.", 11, Muted, FontWeights.Normal, new Thickness(0, 9, 0, 0)));
        optionsCard.Child = options;
        page.Children.Add(optionsCard);

        var resultsCard = Card(Surface, Outline);
        resultsCard.Margin = new Thickness(0, 13, 0, 0);
        var results = new StackPanel();
        results.Children.Add(T("Resultado por mensaje y adjunto", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 7)));
        results.Children.Add(T("DETECCIÓN = ClamAV · Aviso enlace = heurística, no malware confirmado · " +
            "Omitido/Error/Incompleto = hay contenido sin verificar.",
            11, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 8)));
        _mailReviewSummary = T("Sin resultados de correo todavía.", 12, Amber,
            FontWeights.SemiBold, new Thickness(0, 0, 0, 8));
        _mailReviewSummary.TextWrapping = TextWrapping.Wrap;
        results.Children.Add(_mailReviewSummary);
        var filterRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 9) };
        filterRow.Children.Add(T("Mostrar:", 12, Ink, FontWeights.SemiBold, new Thickness(0, 6, 8, 0)));
        _mailReviewFilter = new ComboBox { Width = 207,
            ItemsSource = new[] { "Todos", "Requieren atención", "Detecciones", "No analizados / errores", "Avisos de enlaces" },
            SelectedIndex = 0 };
        _mailReviewFilter.SelectionChanged += (_, _) => _mailItemsView?.Refresh();
        filterRow.Children.Add(_mailReviewFilter);
        results.Children.Add(filterRow);
        _mailFindings.CollectionChanged += (_, _) => RefreshMailReview();
        _mailItemsView = CollectionViewSource.GetDefaultView(_mailFindings);
        _mailItemsView.Filter = entry => entry is MailFinding f &&
            MailTriageService.Matches(f, _mailReviewFilter.SelectedItem as string);
        var list = new ListView { ItemsSource = _mailItemsView, Height = 315,
            Background = Surface, BorderBrush = Outline, FontSize = 12 };
        var columns = new GridView();
        columns.Columns.Add(new GridViewColumn { Header = "Estado", Width = 98,
            DisplayMemberBinding = new Binding(nameof(MailFinding.Status)) });
        columns.Columns.Add(new GridViewColumn { Header = "Mensaje", Width = 150,
            DisplayMemberBinding = new Binding(nameof(MailFinding.DisplayPath)) });
        columns.Columns.Add(new GridViewColumn { Header = "Asunto", Width = 130,
            DisplayMemberBinding = new Binding(nameof(MailFinding.DisplaySubject)) });
        columns.Columns.Add(new GridViewColumn { Header = "Adjunto", Width = 150,
            DisplayMemberBinding = new Binding(nameof(MailFinding.Attachment)) });
        columns.Columns.Add(new GridViewColumn { Header = "Detalle", Width = 380,
            DisplayMemberBinding = new Binding(nameof(MailFinding.Detail)) });
        list.View = columns;
        results.Children.Add(list);
        var tools = new WrapPanel { Margin = new Thickness(0, 11, 0, 0) };
        tools.Children.Add(ActionButton("Guardar informe TXT", false, (_, _) => SaveMailReport(manual: true)));
        tools.Children.Add(ActionButton("Limpiar resultados", false, (_, _) =>
        {
            if (!_mailActive) { _mailFindings.Clear(); _lastMailSummary = null; RefreshMailReview(); }
        }));
        tools.Children.Add(ActionButton("Abrir motor y firmas", false, (_, _) => Navigate("signatures")));
        results.Children.Add(tools);
        resultsCard.Child = results;
        page.Children.Add(resultsCard);
    }

    private static ComboBox MakeMailCombo(int[] choices, int selected)
    {
        return new ComboBox { ItemsSource = choices, SelectedItem = selected, Width = 77,
            Padding = new Thickness(6, 5, 6, 5), Margin = new Thickness(0, 0, 4, 0) };
    }

    private void SaveMailOptions()
    {
        if (_mailMaxSize is null || _mailMaxAttach is null || _mailMaxCount is null ||
            _mailSubfolders is null || _mailAttachments is null) return;
        _settings.MailIncludeSubdirectories = _mailSubfolders.IsChecked == true;
        _settings.MailExtractAttachments = _mailAttachments.IsChecked == true;
        _settings.MailIncludeInline = _mailInline.IsChecked == true;
        _settings.MailPhishingHeuristics = _mailPhishing.IsChecked == true;
        _settings.MailCheckLinks = _mailLinks.IsChecked == true;
        _settings.MailSaveReport = _mailAutoReport.IsChecked == true;
        _settings.MailMaxMessageMb = (int)(_mailMaxSize.SelectedItem ?? 50);
        _settings.MailMaxAttachmentMb = (int)(_mailMaxAttach.SelectedItem ?? 25);
        _settings.MailMaxMessages = (int)(_mailMaxCount.SelectedItem ?? 200);
        SaveSettings();
    }

    private async Task StartMailScanAsync()
    {
        if (_mailActive || _busy) return;
        var sourceKind = (MailSourceKind)Math.Max(0, _mailSource.SelectedIndex);
        var selectedPath = _mailPathBox.Text.Trim().Trim('"');
        if (sourceKind != MailSourceKind.OutlookClassic &&
            (string.IsNullOrWhiteSpace(selectedPath) || (!File.Exists(selectedPath) && !Directory.Exists(selectedPath))))
        { MessageBox.Show(this, "Selecciona un mensaje o un buzón existente.", "PalClam Mail", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (sourceKind == MailSourceKind.Exported && File.Exists(selectedPath) && !MailScanService.IsSupported(selectedPath))
        { MessageBox.Show(this, "Elige MBOX para un buzón local. PST/OST se abren desde la opción Outlook clásico con la carpeta montada.", "PalClam Mail", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        if (sourceKind == MailSourceKind.Mbox && !File.Exists(selectedPath) || sourceKind == MailSourceKind.Maildir && !Directory.Exists(selectedPath))
        { MessageBox.Show(this, "MBOX requiere un archivo y Maildir una carpeta.", "PalClam Mail", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (sourceKind == MailSourceKind.OutlookClassic && MessageBox.Show(this,
            "Se accederá a Outlook clásico que ya esté abierto para que elijas una carpeta. Se guardarán copias temporales de hasta el máximo configurado, sin cambiar los originales. Outlook puede solicitar permisos o descargar contenido. ¿Continuar?",
            "Autorizar lectura de Outlook clásico", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        var scanner = ClamAvLocator.FindScanner();
        if (!ClamAvLocator.IsValidScanner(scanner) || !SignatureStore.HasSignatures())
        {
            MessageBox.Show(this, "No está preparado ClamAV o faltan las firmas. " +
                "Visita Motor y firmas antes de analizar.", "PalClam Mail", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Navigate("signatures");
            return;
        }
        SaveMailOptions();
        _settings.MailLastPath = selectedPath;
        SaveSettings();
        var options = new MailScanOptions(_settings.MailIncludeSubdirectories,
            _settings.MailExtractAttachments, _settings.MailIncludeInline,
            _settings.MailPhishingHeuristics, _settings.MailCheckLinks,
            _settings.ScanArchives, _settings.DetectPua,
            _settings.MailMaxMessageMb, _settings.MailMaxAttachmentMb, _settings.MailMaxMessages);
        _mailFindings.Clear();
        _lastMailSummary = null;
        _mailResultsCount = 0;
        _mailDetections = 0;
        _mailErrors = 0;
        _mailActive = true;
        _mailCancellation = new CancellationTokenSource();
        _mailStartButton.IsEnabled = false;
        _mailSource.IsEnabled = false;
        _mailStopButton.IsEnabled = true;
        _mailStatus.Text = "Analizando mensajes y adjuntos...";
        _mailProgress.IsIndeterminate = true;
        _mailCounters.Text = "Preparando el análisis de correo...";
        var sw = Stopwatch.StartNew();
        SetBusy(true);
        try
        {
            var cancellation = _mailCancellation.Token;
            void Report(MailFinding finding) => Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_mailFindings.Count < 2000) _mailFindings.Add(finding);
                _mailResultsCount++;
                if (finding.Status == "DETECCIÓN") { _mailDetections++; MarkThreatForTray(); }
                if (finding.Status is "Error" or "Incompleto") _mailErrors++;
            }));
            void Progress(int done, int total) => Dispatcher.BeginInvoke(new Action(() =>
            {
                _mailProgress.IsIndeterminate = false;
                _mailProgress.Value = total == 0 ? 0 : (double)done / total * 100;
                _mailCounters.Text = $"Mensajes: {done}/{total} · Detecciones: {_mailDetections} · Errores: {_mailErrors}";
                _mailStatus.Text = $"Mensaje {done} de {total}. Tiempo: {sw.Elapsed.ToString(@"mm\:ss")}. " +
                    "El progreso es por mensajes terminados, no por bytes internos del motor.";
            }));
            MailImportSession? import = null;
            MailScanSummary summary;
            try
            {
                if (sourceKind != MailSourceKind.Exported)
                {
                    import = new MailImportSession();
                    _mailStatus.Text = "Copiando mensajes para el análisis local...";
                    if (sourceKind == MailSourceKind.OutlookClassic)
                        await OutlookClassicImport.ImportAsync(import, options, Report, cancellation);
                    else await Task.Run(() =>
                    {
                        if (sourceKind == MailSourceKind.Mbox) LocalMailboxImport.ImportMbox(selectedPath, import, options, Report, cancellation);
                        else LocalMailboxImport.ImportMaildir(selectedPath, import, options, Report, cancellation);
                    }, cancellation);
                }
                var scanPath = import?.DirectoryPath ?? selectedPath;
                summary = await Task.Run(() => MailScanService.ScanAsync(scanPath, scanner, SignatureStore.DatabaseDirectory,
                    options with { IncludeSubfolders = import is null && options.IncludeSubfolders },
                    finding => Report(import?.Map(finding) ?? finding), Progress, cancellation), cancellation);
                if (import is not null) import.Merge(summary);
            }
            finally
            {
                if (import is not null)
                {
                    try { import.Cleanup(); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        Report(new("Incompleto", selectedPath, "", "No se pudieron borrar las copias temporales: " + import.DirectoryPath, ""));
                        throw new IOException("Quedan copias temporales de correo sin eliminar: " + import.DirectoryPath, ex);
                    }
                }
            }
            // Esperar al vaciado de las actualizaciones de resultados de Dispatcher.
            await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            _lastMailSummary = summary;
            _mailStatus.Text = summary.Detections > 0
                ? $"Análisis terminado con {summary.Detections} detección(es). Revisa resultados."
                : summary.Errors > 0 || summary.Skipped > 0 || summary.Truncated
                    ? "Análisis incompleto o con elementos omitidos: NO debe considerarse limpio."
                    : summary.Discovered == 0 ? "No se encontraron mensajes compatibles en esa ubicación."
                    : "Análisis terminado sin detecciones notificadas; no es garantía de ausencia de amenazas.";
            _mailCounters.Text = $"Mensajes: {summary.Processed}/{summary.Discovered} · Adjuntos: {summary.AttachmentsAnalyzed} · " +
                $"Detecciones: {summary.Detections} · Errores: {summary.Errors} · Omitidos: {summary.Skipped}";
            if (summary.Detections > 0) MarkThreatForTray();
            if (_settings.MailSaveReport) SaveMailReport(manual: false);
            _desktopNotifications?.Show("PalClam Mail", _mailStatus.Text,
                summary.Detections > 0 || summary.Errors > 0 || summary.Skipped > 0 || summary.Truncated);
        }
        catch (OperationCanceledException)
        {
            _mailStatus.Text = "Análisis cancelado. Los correos pendientes NO se han examinado.";
            _mailFindings.Add(new MailFinding("Cancelado", selectedPath, "", "No concluyente", ""));
        }
        catch (Exception ex)
        {
            _mailStatus.Text = "Error del módulo de correo: " + ex.Message;
            _mailFindings.Add(new MailFinding("Error", selectedPath, "", ex.Message, ""));
        }
        finally
        {
            sw.Stop();
            _mailProgress.IsIndeterminate = false;
            _mailStartButton.IsEnabled = true;
            _mailSource.IsEnabled = true;
            _mailStopButton.IsEnabled = false;
            _mailCancellation?.Dispose();
            _mailCancellation = null;
            _mailActive = false;
            UpdateTrayState();
            SetBusy(false);
        }
    }

    private void RefreshMailReview()
    {
        if (_mailReviewSummary is null) return;
        _mailReviewSummary.Text = _mailFindings.Count == 0
            ? "Sin resultados de correo todavía."
            : MailTriageService.Describe(_mailFindings);
        _mailReviewSummary.Foreground = _mailFindings.Any(MailTriageService.IsDetection) ? Red
            : _mailFindings.Any(MailTriageService.NeedsAttention) ? Amber : Green;
    }

    private void SaveMailReport(bool manual)
    {
        try
        {
            string output;
            if (manual)
            {
                var dlg = new SaveFileDialog { Title = "Guardar informe PalClam Mail", DefaultExt = ".txt",
                    Filter = "Texto (*.txt)|*.txt", FileName = "PalClam-Mail-Informe.txt" };
                if (PalClam.UI.LocalizedDialogs.Show(dlg, this) != true) return;
                output = dlg.FileName;
            }
            else
            {
                var folder = Path.Combine(AppPaths.Root, "MailReports");
                Directory.CreateDirectory(folder);
                output = Path.Combine(folder, "PalClam-Mail-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
            }
            var lines = new List<string> { "PalClam Mail 1.2.2", "Fecha: " + DateTimeOffset.Now,
                "Este informe puede contener información privada; no compartir sin revisar.",
                "El análisis no sustituye un antivirus de acceso en tiempo real.", "" };
            if (_lastMailSummary is { } sum)
                lines.Add($"Mensajes: {sum.Processed}/{sum.Discovered}, Detecciones: {sum.Detections}, " +
                    $"Errores: {sum.Errors}, Omitidos: {sum.Skipped}, Advertencias enlaces: {sum.LinkWarnings}");
            lines.Add("RESUMEN DE REVISIÓN: " + MailTriageService.Describe(_mailFindings));
            lines.Add("DETECCIÓN = resultado de ClamAV; aviso de enlace = solo heurística local.");
            lines.Add("No analizado, error, cancelado o incompleto NO equivalen a limpio.");
            lines.Add("Ningún archivo o adjunto se elimina ni se aísla automáticamente.");
            foreach (var heading in new[] { "DETECCIONES CLAMAV", "ARCHIVOS SIN VERIFICAR E INCIDENCIAS",
                "AVISOS HEURÍSTICOS DE ENLACES", "RESTO DE RESULTADOS" })
            {
                lines.Add("");
                lines.Add("=== " + heading + " ===");
                foreach (var f in _mailFindings.Where(f => heading switch
                    {
                        "DETECCIONES CLAMAV" => MailTriageService.IsDetection(f),
                        "ARCHIVOS SIN VERIFICAR E INCIDENCIAS" => MailTriageService.IsUnexamined(f),
                        "AVISOS HEURÍSTICOS DE ENLACES" => MailTriageService.IsLinkNotice(f),
                        _ => !MailTriageService.NeedsAttention(f)
                    }))
                    lines.Add($"[{f.Status}] {f.Path} | {f.Subject} | {f.Attachment} | {f.Detail}");
            }
            File.WriteAllLines(output, lines, new UTF8Encoding(false));
            if (manual) MessageBox.Show(this, "Informe guardado en: " + output,
                "PalClam Mail", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo guardar el informe: " + ex.Message,
                "PalClam Mail", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
