// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Collections.ObjectModel;
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
    private GuardService? _guardService;
    private readonly GuardNoticeGate _guardNoticeGate = new();
    private readonly ObservableCollection<GuardFinding> _guardFindings = new();
    private TextBox _guardFoldersBox = null!;
    private CheckBox _guardAutoStartOption = null!;
    private CheckBox _guardNotifyOption = null!;
    private ComboBox _guardMaxSizeOption = null!;
    private ComboBox _guardProcessingModeOption = null!;
    private TextBlock _guardQueueStatus = null!;
    private TextBlock _guardStatus = null!;
    private TextBlock _guardStatistics = null!;
    private TextBlock _guardLastFile = null!;
    private ListView _guardEventsView = null!;
    private Button _guardStartButton = null!;
    private Button _guardStopButton = null!;
    private Button _guardPauseButton = null!;

    private void BuildGuardPage(StackPanel page)
    {
        page.Children.Add(T("PalClam Guard 2.0", 23, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 6)));
        page.Children.Add(T("Cola inteligente · vigilancia reactiva experimental (Windows)", 12, Purple,
            FontWeights.SemiBold, new Thickness(0, 0, 0, 10)));

        var disclaimer = Card(PurplePale, PurplePale);
        disclaimer.Child = T("Guard detecta nuevos archivos o modificaciones y los envía a ClamAV. " +
            "Solo funciona mientras PalClam está abierto (también minimizado). " +
            "NO bloquea la ejecución ni garantiza detectar todos los accesos. " +
            "Microsoft Defender continúa siendo la protección preventiva principal.",
            12, Ink, FontWeights.Normal);
        page.Children.Add(disclaimer);

        var configuration = Card(Surface, Outline);
        configuration.Margin = new Thickness(0, 13, 0, 0);
        var configStack = new StackPanel();
        configStack.Children.Add(T("Carpetas vigiladas", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 6)));
        configStack.Children.Add(T("Una carpeta por línea; máximo cinco. No se admiten raíces de unidades ni enlaces. " +
            "No se analizan los archivos anteriores: usa Análisis de archivos para ellos.",
            11, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 10)));
        var defaults = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        }.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase);
        _guardFoldersBox = new TextBox
        {
            Text = string.Join(Environment.NewLine, _settings.GuardFolders),
            MinHeight = 82, MaxHeight = 115, AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"), FontSize = 12, Foreground = Ink,
            BorderBrush = Outline, Padding = new Thickness(9)
        };
        configStack.Children.Add(_guardFoldersBox);
        var addRow = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
        addRow.Children.Add(ActionButton("+  Añadir carpeta", false, (_, _) =>
        {
            var dlg = new OpenFolderDialog { Title = "Elegir carpeta para PalClam Guard" };
            if (PalClam.UI.LocalizedDialogs.Show(dlg, this) != true) return;
            if (!string.IsNullOrWhiteSpace(_guardFoldersBox.Text))
                _guardFoldersBox.AppendText(Environment.NewLine);
            _guardFoldersBox.AppendText(dlg.FolderName);
        }));
        addRow.Children.Add(ActionButton("Guardar preferencias", false, (_, _) => SaveGuardOptions()));
        configStack.Children.Add(addRow);
        _guardAutoStartOption = OptionCheck("Activar Guard al abrir PalClam",
            _settings.GuardStartWithApp,
            "Desactivado por defecto; cuando PalClam se cierre, también se detendrá la vigilancia.",
            SaveGuardAutoOptions);
        _guardAutoStartOption.Margin = new Thickness(0, 15, 0, 8);
        configStack.Children.Add(_guardAutoStartOption);
        _guardNotifyOption = OptionCheck("Avisar de amenazas, errores y archivos omitidos (con límite)",
            _settings.GuardNotifyThreats,
            "Evita avisos duplicados; el historial conserva todos los eventos. Requiere PalClam abierto.", SaveGuardAutoOptions);
        configStack.Children.Add(_guardNotifyOption);
        configStack.Children.Add(T("Tamaño máximo de archivo por Guard:", 12, Ink,
            FontWeights.SemiBold, new Thickness(0, 6, 0, 4)));
        _guardMaxSizeOption = new ComboBox
        {
            Width = 160, HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            ItemsSource = new[] { "50 MB", "100 MB", "250 MB" },
            SelectedItem = _settings.GuardMaxFileMb + " MB",
            Padding = new Thickness(8, 5, 8, 5), BorderBrush = Outline
        };
        configStack.Children.Add(_guardMaxSizeOption);
        configStack.Children.Add(T("Perfil de rendimiento de Guard:", 12, Ink,
            FontWeights.SemiBold, new Thickness(0, 12, 0, 4)));
        _guardProcessingModeOption = new ComboBox
        {
            Width = 180, HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            ItemsSource = new[] { "Eficiente", "Equilibrado", "Rápido" },
            SelectedItem = GuardTuning.NormalizeMode(_settings.GuardProcessingMode),
            Padding = new Thickness(8, 5, 8, 5), BorderBrush = Outline
        };
        configStack.Children.Add(_guardProcessingModeOption);
        configStack.Children.Add(T("Eficiente reduce la frecuencia de comprobaciones; Rápido responde antes, " +
            "pero puede consumir más recursos. Los tres perfiles usan un solo análisis simultáneo. " +
            "Los cambios se aplican tras reiniciar Guard.", 11, Muted,
            FontWeights.Normal, new Thickness(0, 7, 0, 2)));
        configStack.Children.Add(T("Guard atiende una petición cada vez para limitar el consumo. " +
            "Los archivos demasiado grandes, inaccesibles o inestables se notifican como no analizados.",
            11, Muted, FontWeights.Normal, new Thickness(0, 8, 0, 0)));
        configuration.Child = configStack;
        page.Children.Add(configuration);

        var runningCard = Card(Surface, Outline);
        runningCard.Margin = new Thickness(0, 13, 0, 0);
        var runningStack = new StackPanel();
        runningStack.Children.Add(T("Estado de vigilancia", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 9)));
        _guardStatus = T("Desactivado · No hay protección de acceso en tiempo real", 13, Muted,
            FontWeights.SemiBold, new Thickness(0, 0, 0, 7));
        runningStack.Children.Add(_guardStatus);
        _guardStatistics = T("Analizados: 0 · Detecciones: 0 · No analizados: 0 · Errores: 0", 12, Ink,
            FontWeights.Normal, new Thickness(0, 0, 0, 7));
        runningStack.Children.Add(_guardStatistics);
        _guardQueueStatus = T("Cola: 0 · Eventos agrupados: 0 · Perdidos: 0", 11, Muted,
            FontWeights.Normal, new Thickness(0, 0, 0, 8));
        _guardQueueStatus.TextWrapping = TextWrapping.Wrap;
        runningStack.Children.Add(_guardQueueStatus);
        _guardLastFile = T("Último archivo: —", 11, Muted, FontWeights.Normal,
            new Thickness(0, 0, 0, 12));
        _guardLastFile.TextWrapping = TextWrapping.Wrap;
        runningStack.Children.Add(_guardLastFile);
        var actionRow = new WrapPanel();
        _guardStartButton = ActionButton("▶  Iniciar vigilancia", true,
            (_, _) => StartGuard(manual: true));
        _guardStopButton = ActionButton("■  Detener vigilancia", false,
            async (_, _) => await StopGuardAsync());
        SetSemanticButton(_guardStopButton, "danger");
        _guardStopButton.IsEnabled = false;
        actionRow.Children.Add(_guardStartButton);
        actionRow.Children.Add(_guardStopButton);
        _guardPauseButton = ActionButton("Ⅱ  Pausar vigilancia", false,
            async (_, _) => await ToggleGuardPauseAsync());
        SetSemanticButton(_guardPauseButton, "warn");
        _guardPauseButton.IsEnabled = false;
        actionRow.Children.Add(_guardPauseButton);
        runningStack.Children.Add(actionRow);
        runningCard.Child = runningStack;
        page.Children.Add(runningCard);

        var eventsCard = Card(Surface, Outline);
        eventsCard.Margin = new Thickness(0, 13, 0, 0);
        var eventsStack = new StackPanel();
        eventsStack.Children.Add(T("Actividad reciente de Guard", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 8)));
        _guardEventsView = new ListView
        {
            ItemsSource = _guardFindings, MinHeight = 190, MaxHeight = 300,
            Background = Surface, BorderBrush = Outline, BorderThickness = new Thickness(1)
        };
        var columns = new GridView();
        columns.Columns.Add(new GridViewColumn { Header = "Fecha", Width = 120,
            DisplayMemberBinding = new Binding(nameof(GuardFinding.DisplayTime)) });
        columns.Columns.Add(new GridViewColumn { Header = "Estado", Width = 110,
            DisplayMemberBinding = new Binding(nameof(GuardFinding.DisplayOutcome)) });
        columns.Columns.Add(new GridViewColumn { Header = "Archivo", Width = 300,
            DisplayMemberBinding = new Binding(nameof(GuardFinding.FilePath)) });
        columns.Columns.Add(new GridViewColumn { Header = "Detalle", Width = 285,
            DisplayMemberBinding = new Binding(nameof(GuardFinding.Detail)) });
        _guardEventsView.View = columns;
        eventsStack.Children.Add(_guardEventsView);
        var tools = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
        tools.Children.Add(ActionButton("Examinar de nuevo selección", false,
            (_, _) =>
            {
                if (_guardEventsView.SelectedItem is GuardFinding entry && File.Exists(entry.FilePath))
                    _ = StartScanAsync(entry.FilePath);
                else MessageBox.Show(this, "Selecciona un archivo existente.", "PalClam Guard");
            }));
        tools.Children.Add(ActionButton("Aislar detección seleccionada", false,
            async (_, _) => await IsolateGuardFindingAsync()));
        tools.Children.Add(ActionButton("Guardar informe TXT", false, (_, _) => ExportGuardReport()));
        tools.Children.Add(ActionButton("Ver cuarentena", false, (_, _) => Navigate("quarantine")));
        eventsStack.Children.Add(tools);
        eventsStack.Children.Add(T("La pausa deja los observadores activos: los eventos pendientes se acumulan " +
            "hasta el límite de la cola. Una pausa larga, el desbordamiento o el cierre pueden " +
            "dejar archivos sin comprobar; realiza un análisis manual para recuperar cobertura. " +
            "No se mueve ni elimina ningún archivo sin confirmación.",
            11, Muted, FontWeights.Normal, new Thickness(0, 10, 0, 0)));
        eventsCard.Child = eventsStack;
        page.Children.Add(eventsCard);
        BuildMachineServiceSection(page);
    }

    private void SaveGuardAutoOptions()
    {
        if (_guardAutoStartOption is null || _guardNotifyOption is null) return;
        _settings.GuardStartWithApp = _guardAutoStartOption.IsChecked == true;
        _settings.GuardNotifyThreats = _guardNotifyOption.IsChecked == true;
        SaveSettings();
    }

    private bool SaveGuardOptions()
    {
        try
        {
            var lines = _guardFoldersBox.Text.Split(
                new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var folders = lines.Length == 0 && _settings.DownloadGuardEnabled
                ? new List<string>() : GuardService.NormalizeFolders(lines);
            DownloadGuardPolicy.Compose(folders, _settings.DownloadGuardEnabled,
                DownloadGuardPolicy.DownloadsFolder);
            var selected = _guardMaxSizeOption.SelectedItem?.ToString()?.Split(' ')[0];
            if (!int.TryParse(selected, out var limit) || limit is not (50 or 100 or 250))
                throw new ArgumentException("Selecciona un límite válido.");
            _settings.GuardFolders = folders;
            _settings.GuardMaxFileMb = limit;
            var selectedMode = _guardProcessingModeOption.SelectedItem?.ToString();
            if (selectedMode is not ("Eficiente" or "Equilibrado" or "Rápido"))
                throw new ArgumentException("Selecciona un perfil de Guard válido.");
            _settings.GuardProcessingMode = selectedMode;
            SaveGuardAutoOptions();
            _guardFoldersBox.Text = string.Join(Environment.NewLine, folders);
            _guardStatus.Text = _guardService is null
                ? "Preferencias guardadas. Inicia Guard para vigilar las carpetas."
                : "Preferencias guardadas. Detén y vuelve a iniciar Guard para aplicarlas.";
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            MessageBox.Show(this, "No se guardaron las carpetas: " + ex.Message,
                "PalClam Guard", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    private Task StartGuardOnLaunchAsync()
    {
        if (_settings.GuardStartWithApp || _settings.DownloadGuardEnabled)
            StartGuard(manual: false);
        return Task.CompletedTask;
    }

    private void StartGuard(bool manual)
    {
        if (_guardService is not null) return;
        if (_busy || _checkingEngine || _installingAppUpdate)
        {
            _guardStatus.Text = "Espera a que termine la operación del motor antes de iniciar Guard.";
            return;
        }
        if (manual && !SaveGuardOptions()) return;
        try
        {
            // Download Guard reutiliza el mismo proceso/cola de Guard 2.0.
            // Si solo se habilitó Descargas, no activar silenciosamente otras carpetas guardadas.
            var personal = !manual && !_settings.GuardStartWithApp && _settings.DownloadGuardEnabled
                ? Array.Empty<string>() : _settings.GuardFolders.ToArray();
            var folders = GuardService.NormalizeFolders(DownloadGuardPolicy.Compose(
                personal, _settings.DownloadGuardEnabled, DownloadGuardPolicy.DownloadsFolder));
            var scanner = ClamAvLocator.FindScanner();
            var config = new GuardConfiguration(
                folders, scanner, SignatureStore.DatabaseDirectory,
                _settings.ScanArchives, _settings.DetectPua,
                ScanExclusions.Normalize(_settings.ExcludedPaths), _settings.GuardMaxFileMb,
                GuardTuning.NormalizeMode(_settings.GuardProcessingMode));
            var service = new GuardService(config, HandleGuardFinding,
                () => _busy || _checkingEngine || _installingAppUpdate);
            try { service.Start(); }
            catch { service.Dispose(); throw; }
            _guardService = service;
            _guardPaused = false;
            UpdateTrayState();
            _guardStatus.Text = $"Guard 2.0: vigilando {folders.Count} carpeta(s) " +
                $"({service.ProcessingMode}). No hay bloqueo preventivo.";
            _guardStatus.Foreground = Green;
            _guardPauseButton.IsEnabled = true;
            _guardPauseButton.Content = "Ⅱ  Pausar vigilancia";
            _guardStartButton.IsEnabled = false;
            _guardStopButton.IsEnabled = true;
            RefreshGuardTelemetry();
            AppendLog("[GUARD] Vigilancia iniciada de " + string.Join("; ", folders));
        }
        catch (Exception ex)
        {
            _guardStatus.Text = "No se pudo iniciar Guard: " + ex.Message;
            _guardStatus.Foreground = B("#A23E3E");
            if (manual) MessageBox.Show(this, ex.Message, "PalClam Guard", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            AppendLog("[GUARD] No se inició: " + ex.Message);
        }
    }

    private async Task StopGuardAsync()
    {
        var guard = _guardService;
        if (guard is null) return;
        _guardStopButton.IsEnabled = false;
        _guardPauseButton.IsEnabled = false;
        _guardStartButton.IsEnabled = false;
        _guardStatus.Text = "Deteniendo vigilancia y análisis en curso...";
        guard.Stop();
        try { await guard.StopAsync(); }
        finally
        {
            guard.Dispose();
            if (ReferenceEquals(_guardService, guard)) _guardService = null;
            _guardPaused = false;
            UpdateTrayState();
            _guardStartButton.IsEnabled = true;
            _guardStatus.Text = "Desactivado: no se vigilan archivos.";
            _guardStatus.Foreground = Muted;
            RefreshGuardTelemetry();
            AppendLog("[GUARD] Vigilancia detenida.");
        }
    }

    private void StopGuardOnClose()
    {
        try { _guardService?.Stop(); } catch (Exception) { }
        _guardService = null;
    }

    private void RefreshGuardTelemetry()
    {
        if (_guardStatistics is null || _guardQueueStatus is null) return;
        var guard = _guardService;
        if (guard is null)
        {
            _guardQueueStatus.Text = "Cola: 0 · Eventos agrupados: 0 · Perdidos: 0";
            return;
        }
        _guardStatistics.Text = $"Analizados: {guard.Scanned:N0} · Detecciones: {guard.Detections:N0} · " +
            $"No analizados: {guard.Skipped:N0} · Errores: {guard.Failures:N0}";
        _guardQueueStatus.Text = $"Cola: {guard.QueuedCount:N0}/{guard.QueueCapacity:N0} " +
            $"· Procesando: {(guard.PendingCount > guard.QueuedCount ? "sí" : "no")} " +
            $"· Agrupados: {guard.CoalescedEvents:N0} · Perdidos: {guard.LostEvents:N0}";
    }

    private void HandleGuardFinding(GuardFinding finding)
    {
        if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_guardEventsView is null) return;
            _guardFindings.Insert(0, finding);
            if (_guardFindings.Count > 500) _guardFindings.RemoveAt(_guardFindings.Count - 1);
            _guardLastFile.Text = "Último evento: " + finding.FilePath;
            var guard = _guardService;
            if (guard is not null)
            {
                RefreshGuardTelemetry();
                if (guard.IsDegraded)
                {
                    _guardStatus.Foreground = B("#A23E3E");
                    _guardStatus.Text = "ATENCIÓN: vigilancia incompleta (eventos perdidos). " +
                        "Detén/reinicia Guard y ejecuta un análisis manual.";
                }
            }
            if (finding.Outcome == GuardOutcome.Threat) MarkThreatForTray();
            if (finding.Outcome is GuardOutcome.Threat or GuardOutcome.Warning or GuardOutcome.Error)
            {
                AppendLog("[GUARD] " + finding.DisplayOutcome + ": " + finding.FilePath + " · " + finding.Detail);
            }
            // Todas las incidencias se conservan en historial y auditoría. Solo
            // se limita la frecuencia de avisos visuales para evitar inundar Windows.
            if (_settings.GuardNotifyThreats &&
                finding.Outcome is GuardOutcome.Threat or GuardOutcome.Warning or GuardOutcome.Error or GuardOutcome.Skipped)
            {
                var noticeKind = finding.Outcome switch
                {
                    GuardOutcome.Threat => GuardNoticeKind.Threat,
                    GuardOutcome.Error => GuardNoticeKind.Error,
                    _ => GuardNoticeKind.Warning
                };
                if (_guardNoticeGate.ShouldShow(noticeKind, finding.FilePath, DateTimeOffset.UtcNow))
                {
                    var filename = Path.GetFileName(finding.FilePath);
                    _desktopNotifications?.Show("PalClam Guard: " + finding.DisplayOutcome,
                        (string.IsNullOrWhiteSpace(filename) ? "Archivo sin nombre" : filename) +
                        " · Consulta el historial de Guard para ver el detalle.", warning: true);
                }
            }
            try { GuardAuditLog.Append(finding); }
            catch (Exception ex) { AppendLog("[GUARD] No se pudo guardar una incidencia: " + ex.Message); }
        }));
    }

    private async Task IsolateGuardFindingAsync()
    {
        if (_guardEventsView.SelectedItem is not GuardFinding { Outcome: GuardOutcome.Threat } finding)
        {
            MessageBox.Show(this, "Selecciona primero una amenaza detectada por Guard.",
                "Cuarentena", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_busy || _guardService is not null)
        {
            MessageBox.Show(this, "Detén Guard y los análisis en curso antes de aislar un archivo.",
                "Cuarentena", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var info = new FileInfo(finding.FilePath);
            info.Refresh();
            if (!info.Exists || info.Length != finding.FileLength ||
                info.LastWriteTimeUtc != finding.LastWriteUtc)
                throw new IOException("El archivo ha cambiado desde la detección. Analízalo de nuevo antes de aislarlo.");
            if (MessageBox.Show(this,
                "¿Aislar MANUALMENTE este archivo detectado? No se eliminará.\n\n" +
                finding.FilePath + "\n\n" + finding.Detail,
                "Confirmar cuarentena", MessageBoxButton.YesNo, MessageBoxImage.Warning) !=
                MessageBoxResult.Yes) return;
            await QuarantineStore.IsolateAsync(finding.FilePath, finding.Detail);
            Navigate("quarantine");
            AppendLog("[GUARD] Archivo aislado manualmente: " + finding.FilePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo aislar: " + ex.Message,
                "Cuarentena", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ExportGuardReport()
    {
        var dialog = new SaveFileDialog { Title = "Guardar actividad de PalClam Guard",
            Filter = "Texto (*.txt)|*.txt", DefaultExt = ".txt",
            FileName = "PalClam-Guard-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".txt" };
        if (PalClam.UI.LocalizedDialogs.Show(dialog, this) != true) return;
        try
        {
            var report = new StringBuilder("PalClam Guard 1.0.0 - actividad de sesión" + Environment.NewLine +
                "Esta vigilancia no bloquea archivos ni garantiza la inspección de todos los eventos." +
                Environment.NewLine + Environment.NewLine);
            foreach (var finding in _guardFindings)
                report.AppendLine($"{finding.DisplayTime} | {finding.DisplayOutcome} | " +
                    $"{finding.FilePath} | {finding.Detail}");
            File.WriteAllText(dialog.FileName, report.ToString());
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Guardar informe", MessageBoxButton.OK,
            MessageBoxImage.Warning); }
    }
}
