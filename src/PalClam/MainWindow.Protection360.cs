// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private readonly ObservableCollection<HealthFinding> _healthFindings = new();
    private TextBlock? _healthSummary;
    private TextBlock? _healthDetails;
    private bool _healthRefreshing;

    private CheckBox? _usbWatchOption;
    private CheckBox? _usbAutoScanOption;
    private CheckBox? _downloadsWatchOption;
    private ComboBox? _usbDriveSelection;
    private TextBlock? _deviceWatchStatus;
    private readonly RemovableDriveMonitor _removableMonitor = new();
    private DispatcherTimer? _removableTimer;

    private void BuildHealth360Page(StackPanel page)
    {
        page.Children.Add(T("Security Health 360", 23, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 6)));
        var intro = T("Comprobación local de estado: no realiza un análisis de amenazas, " +
            "no bloquea ejecuciones y no sustituye Microsoft Defender.", 12, Muted);
        intro.TextWrapping = TextWrapping.Wrap;
        page.Children.Add(intro);
        var card = Card(Surface, Outline);
        card.Margin = new Thickness(0, 14, 0, 0);
        var stack = new StackPanel();
        _healthSummary = T("Pendiente de comprobación", 15, Purple, FontWeights.Bold,
            new Thickness(0, 0, 0, 10));
        _healthSummary.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(_healthSummary);
        var list = new ListView
        {
            ItemsSource = _healthFindings, MinHeight = 285, MaxHeight = 400,
            Background = Surface, BorderBrush = Outline, BorderThickness = new Thickness(1)
        };
        var columns = new GridView();
        columns.Columns.Add(new GridViewColumn { Header = "Componente", Width = 165,
            DisplayMemberBinding = new System.Windows.Data.Binding(nameof(HealthFinding.Component)) });
        columns.Columns.Add(new GridViewColumn { Header = "Estado", Width = 130,
            DisplayMemberBinding = new System.Windows.Data.Binding(nameof(HealthFinding.DisplayLevel)) });
        columns.Columns.Add(new GridViewColumn { Header = "Detalle real", Width = 485,
            DisplayMemberBinding = new System.Windows.Data.Binding(nameof(HealthFinding.Detail)) });
        list.View = columns;
        stack.Children.Add(list);
        _healthDetails = T("Los componentes opcionales desactivados no se presentan como amenazas.",
            11, Muted, FontWeights.Normal, new Thickness(0, 11, 0, 10));
        _healthDetails.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(_healthDetails);
        var actions = new WrapPanel();
        actions.Children.Add(ActionButton("↻  Comprobar sistema", true,
            async (_, _) => await RefreshProtectionHealthAsync()));
        actions.Children.Add(ActionButton("USB y Descargas", false, (_, _) => Navigate("devices")));
        actions.Children.Add(ActionButton("Motor y firmas", false, (_, _) => Navigate("signatures")));
        actions.Children.Add(ActionButton("Ver avisos", false, (_, _) => Navigate("alerts")));
        actions.Children.Add(ActionButton("✦  Explicar estados con IA", false,
            (_, _) => OpenAiAnalystWithPrompt(AiSecurityAnalyst.ForHealth(_healthFindings.ToArray()))));
        actions.Children.Add(ActionButton("Copiar diagnóstico", false, (_, _) =>
        {
            try
            {
                var report = new StringBuilder("PalClam · Security Health 360 · " +
                    DateTime.Now.ToString("dd/MM/yyyy HH:mm") + Environment.NewLine);
                report.AppendLine(_healthSummary?.Text ?? "Sin comprobar");
                foreach (var item in _healthFindings)
                    report.AppendLine(PalClam.Localization.DisplayText.Translate(item.Component) + " | " +
                        PalClam.Localization.DisplayText.Translate(item.DisplayLevel) + " | " +
                        PalClam.Localization.DisplayText.Translate(item.Detail));
                report.AppendLine("Diagnóstico parcial; no garantiza ausencia de malware.");
                Clipboard.SetText(PalClam.Localization.DisplayText.Translate(report.ToString()));
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Copiar", MessageBoxButton.OK,
                MessageBoxImage.Warning); }
        }));
        stack.Children.Add(actions);
        card.Child = stack;
        page.Children.Add(card);
    }

    private async Task RefreshProtectionHealthAsync()
    {
        if (_healthRefreshing || _healthSummary is null) return;
        _healthRefreshing = true;
        _healthSummary.Text = "Verificando componentes locales…";
        try
        {
            bool machineRegistered;
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\PalClamGuardSvc");
                machineRegistered = key is not null;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
            {
                machineRegistered = true; // No se declara ausente sin poder consultar.
            }
            var status = MachineServiceSnapshot.TryRead(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "PalClam", "MachineGuard", "estado.json"));
            var sigHealth = SignatureHealthService.Evaluate(SignatureStore.HasSignatures(),
                SignatureHealthService.DailyFileTimeUtc(SignatureStore.DatabaseDirectory),
                _settings.LastSignatureCheckUtc, _settings.LastSignatureAttemptUtc,
                _settings.LastSignatureUpdateError, DateTimeOffset.UtcNow);
            bool? scheduled = null;
            try
            {
                var result = await ScheduledTasksService.StatusAsync().WaitAsync(TimeSpan.FromSeconds(7));
                if (result.Success) scheduled = true;
                else if (result.Detail.Contains("no existe", StringComparison.OrdinalIgnoreCase) ||
                         result.Detail.Contains("cannot find", StringComparison.OrdinalIgnoreCase) ||
                         result.Detail.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    scheduled = false;
            }
            catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            { /* El estado de tareas es desconocido, no correcto. */ }

            var guard = _guardService;
            var watchingDownloads = guard?.IsRunning == true && guard.WatchedFolders.Any(folder =>
                DownloadGuardPolicy.IsWithin(DownloadGuardPolicy.DownloadsFolder, folder));
            var input = new HealthInputs(
                ClamAvLocator.IsValidScanner(_engineBox.Text), sigHealth.State,
                guard?.IsRunning == true, guard?.IsDegraded == true,
                _settings.DownloadGuardEnabled, watchingDownloads,
                _settings.UsbWatchEnabled, _removableTimer?.IsEnabled == true,
                machineRegistered, machineRegistered ? status : null,
                WindowsStartupRegistration.GetScope() != StartupScope.Disabled,
                scheduled, DateTimeOffset.UtcNow);
            var findings = ProtectionHealthPolicy.Evaluate(input);
            _healthFindings.Clear();
            foreach (var finding in findings) _healthFindings.Add(finding);
            _healthSummary.Text = ProtectionHealthPolicy.Summary(findings);
            _healthSummary.Foreground = findings.Any(f => f.Level == HealthLevel.Attention)
                ? Amber : findings.Any(f => f.Level == HealthLevel.Unknown) ? Purple : Green;
            if (_healthDetails is not null)
                _healthDetails.Text = "Firmas: " + sigHealth.Title + ". " +
                    sigHealth.Detail + " Los informes pueden contener rutas personales.";
        }
        catch (Exception ex)
        {
            _healthSummary.Text = "No se completó el diagnóstico: " + ex.Message;
            _healthSummary.Foreground = Amber;
        }
        finally { _healthRefreshing = false; }
    }

    private void BuildDeviceGuardPage(StackPanel page)
    {
        page.Children.Add(T("USB Watch + Download Guard", 23, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 6)));
        var introduction = T("Vigilancia reactiva opcional. USB Watch reconoce únicamente las unidades " +
            "clasificadas por Windows como extraíbles; algunos discos USB aparecen como fijos. " +
            "No intercepta lecturas, no bloquea ejecuciones ni garantiza una detección completa.",
            12, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 12));
        introduction.TextWrapping = TextWrapping.Wrap;
        page.Children.Add(introduction);
        var settings = Card(Surface, Outline);
        var body = new StackPanel();
        body.Children.Add(T("Preferencias de protección", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 10)));
        _usbWatchOption = OptionCheck("Detectar unidades extraíbles mientras PalClam esté abierto",
            _settings.UsbWatchEnabled,
            "Consulta Windows cada cinco segundos; las unidades conectadas antes del inicio no se autoanalizan.",
            SaveDeviceWatchOptions);
        body.Children.Add(_usbWatchOption);
        _usbAutoScanOption = OptionCheck("Analizar automáticamente al detectar una unidad nueva",
            _settings.UsbAutoScanEnabled && _settings.UsbWatchEnabled,
            "Desactivado por defecto. Puede tardar y consumir recursos; si hay un análisis en curso se solicita revisión manual.",
            SaveDeviceWatchOptions);
        _usbAutoScanOption.IsEnabled = _settings.UsbWatchEnabled;
        body.Children.Add(_usbAutoScanOption);
        _downloadsWatchOption = OptionCheck("Vigilar mi carpeta Descargas con Guard 2.0",
            _settings.DownloadGuardEnabled,
            "Se añade Descargas a la cola existente de Guard. No es un segundo motor. " +
            "Se aplica al siguiente inicio de Guard y únicamente a tu sesión.",
            SaveDeviceWatchOptions);
        body.Children.Add(_downloadsWatchOption);
        body.Children.Add(T("Carpeta prevista: " + DownloadGuardPolicy.DownloadsFolder,
            11, Muted, FontWeights.Normal, new Thickness(0, 4, 0, 6)));
        _deviceWatchStatus = T("Configuración voluntaria. Guard requiere motor y firmas locales.",
            12, Muted, FontWeights.Normal, new Thickness(0, 7, 0, 7));
        _deviceWatchStatus.TextWrapping = TextWrapping.Wrap;
        body.Children.Add(_deviceWatchStatus);
        settings.Child = body;
        page.Children.Add(settings);

        var devicesCard = Card(Surface, Outline);
        devicesCard.Margin = new Thickness(0, 13, 0, 0);
        var deviceBody = new StackPanel();
        deviceBody.Children.Add(T("Unidades extraíbles detectadas", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 8)));
        _usbDriveSelection = new ComboBox
        {
            MinWidth = 260, MaxWidth = 540,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            Padding = new Thickness(9, 6, 9, 6), BorderBrush = Outline
        };
        deviceBody.Children.Add(_usbDriveSelection);
        var actionRow = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
        actionRow.Children.Add(ActionButton("↻  Actualizar unidades", false, (_, _) => RefreshRemovableDrives()));
        actionRow.Children.Add(ActionButton("⌕  Analizar unidad seleccionada", true, (_, _) =>
        {
            if (_usbDriveSelection?.SelectedItem is not RemovableVolume selected) return;
            ScanRemovableDrive(selected.Root, automatic: false);
        }));
        actionRow.Children.Add(ActionButton("Centro de notificaciones", false,
            (_, _) => Navigate("alerts")));
        deviceBody.Children.Add(actionRow);
        var note = T("El resultado del análisis aparece en Análisis e Historial. Las conexiones USB se " +
            "anotan como información, nunca como detecciones. Desconecta la unidad con seguridad.",
            11, Muted, FontWeights.Normal, new Thickness(0, 10, 0, 0));
        note.TextWrapping = TextWrapping.Wrap;
        deviceBody.Children.Add(note);
        devicesCard.Child = deviceBody;
        page.Children.Add(devicesCard);
    }

    private void SaveDeviceWatchOptions()
    {
        if (_usbWatchOption is null || _usbAutoScanOption is null || _downloadsWatchOption is null) return;
        _settings.UsbWatchEnabled = _usbWatchOption.IsChecked == true;
        _settings.UsbAutoScanEnabled = _settings.UsbWatchEnabled && _usbAutoScanOption.IsChecked == true;
        _settings.DownloadGuardEnabled = _downloadsWatchOption.IsChecked == true;
        _usbAutoScanOption.IsEnabled = _settings.UsbWatchEnabled;
        SaveSettings();
        UpdateRemovableTimer();
        if (_deviceWatchStatus is not null)
            _deviceWatchStatus.Text = _guardService is null
                ? "Opciones guardadas. Descargas se vigilará al iniciar Guard si se ha activado."
                : "Opciones guardadas. Para aplicar cambios de Descargas, detén e inicia Guard.";
    }

    private static IReadOnlyList<RemovableVolume> EnumerateRemovableDrives()
    {
        var list = new List<RemovableVolume>();
        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.DriveType != DriveType.Removable || !drive.IsReady) continue;
                    if (RemovableDriveMonitor.IsDriveRoot(drive.RootDirectory.FullName))
                        list.Add(new RemovableVolume(drive.RootDirectory.FullName, drive.Name +
                            " · " + (string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Unidad extraíble" : drive.VolumeLabel)));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
                    System.Security.SecurityException) { /* Medio desconectado. */ }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            System.Security.SecurityException) { /* Estado no disponible. */ }
        return list;
    }

    private void StartRemovableMonitoring()
    {
        _removableTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _removableTimer.Tick += (_, _) => RefreshRemovableDrives();
        UpdateRemovableTimer();
        RefreshRemovableDrives();
        Closed += (_, _) => _removableTimer?.Stop();
    }

    private void UpdateRemovableTimer()
    {
        if (_removableTimer is null) return;
        if (_settings.UsbWatchEnabled)
        {
            _removableMonitor.Reset(EnumerateRemovableDrives());
            _removableTimer.Start();
        }
        else _removableTimer.Stop();
    }

    private void RefreshRemovableDrives()
    {
        var drives = EnumerateRemovableDrives();
        var selected = (_usbDriveSelection?.SelectedItem as RemovableVolume)?.Root;
        if (_usbDriveSelection is not null)
        {
            _usbDriveSelection.ItemsSource = drives;
            _usbDriveSelection.DisplayMemberPath = nameof(RemovableVolume.Label);
            _usbDriveSelection.SelectedItem = drives.FirstOrDefault(d =>
                string.Equals(d.Root, selected, StringComparison.OrdinalIgnoreCase)) ?? drives.FirstOrDefault();
        }
        if (!_settings.UsbWatchEnabled) return;
        foreach (var drive in _removableMonitor.Observe(drives))
        {
            try { DeviceWatchAudit.Append(new DeviceWatchRecord(DateTimeOffset.UtcNow,
                "Information", drive.Root, "Unidad extraíble detectada; no se ha ejecutado el contenido.")); }
            catch (Exception ex) { AppendLog("[USB] No se guardó el evento: " + ex.Message); }
            _desktopNotifications?.Show("PalClam USB Watch", "Unidad extraíble " + drive.Root +
                (_settings.UsbAutoScanEnabled ? " · Análisis automático solicitado." :
                " · Puedes analizarla desde USB y Descargas."), warning: false);
            if (_settings.UsbAutoScanEnabled) ScanRemovableDrive(drive.Root, automatic: true);
        }
    }

    private void ScanRemovableDrive(string root, bool automatic)
    {
        if (!RemovableDriveMonitor.IsDriveRoot(root) ||
            !EnumerateRemovableDrives().Any(x => x.Root.Equals(root, StringComparison.OrdinalIgnoreCase)))
        {
            if (_deviceWatchStatus is not null) _deviceWatchStatus.Text = "La unidad ya no está disponible.";
            return;
        }
        if (_busy || _checkingEngine || _installingAppUpdate ||
            !ClamAvLocator.IsValidScanner(_engineBox.Text) || !SignatureStore.HasSignatures())
        {
            var detail = "No se inició el análisis de " + root +
                ": motor ocupado, ausente o sin firmas. Examina la unidad manualmente.";
            if (_deviceWatchStatus is not null) _deviceWatchStatus.Text = detail;
            try { DeviceWatchAudit.Append(new DeviceWatchRecord(DateTimeOffset.UtcNow,
                "Warning", root, detail)); }
            catch (Exception ex) { AppendLog("[USB] Auditoría no disponible: " + ex.Message); }
            return;
        }
        try { DeviceWatchAudit.Append(new DeviceWatchRecord(DateTimeOffset.UtcNow,
            "Information", root, "Análisis de unidad solicitado" + (automatic ? " automáticamente." : " manualmente."))); }
        catch (Exception ex) { AppendLog("[USB] Auditoría no disponible: " + ex.Message); }
        if (_deviceWatchStatus is not null) _deviceWatchStatus.Text = "Solicitud de análisis: " + root;
        _ = StartScanAsync(root);
    }
}
