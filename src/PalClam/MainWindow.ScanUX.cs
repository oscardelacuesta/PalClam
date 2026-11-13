// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private ScanPauseGate? _scanPauseGate;
    private ScanBatchStatus? _scanBatch;
    private bool _scanPausedBetweenBatches;
    private long _scanGeneration;
    private TextBlock _headlineState = null!;
    private TextBlock _headlineFile = null!;
    private TextBlock _headlineStats = null!;
    private Button _topScanPauseButton = null!;
    private Button _dockPauseButton = null!;
    private TextBlock _dockPath = null!;
    private TextBlock _globalScanIndicator = null!;
    private Border _dashboardActiveScan = null!;
    private TextBlock _dashboardActiveTitle = null!;
    private TextBlock _dashboardActivePath = null!;
    private TextBlock _dashboardActiveDetails = null!;
    private ProgressBar _dashboardActiveProgress = null!;
    private Button _dashboardActivePause = null!;
    private Button _dashboardActiveStop = null!;
    private CheckBox _floatingScanOption = null!;
    private CheckBox _floatingTopmostOption = null!;
    private ComboBox _scanModeOption = null!;
    private ComboBox _floatingModeOption = null!;
    private FloatingScanWindow? _floatingScanWindow;
    private bool _floatingHiddenByUser;

    private Border BuildScanHeadline()
    {
        var bar = new Border { Background = BluePale, BorderBrush = Blue,
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14),
            Padding = new Thickness(14), Margin = new Thickness(0, 0, 0, 15) };
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var info = new StackPanel();
        _headlineState = T("LISTO PARA ANALIZAR", 17, Blue, FontWeights.Bold);
        info.Children.Add(_headlineState);
        _headlineFile = T("El archivo o lote en examen aparecerá aquí al iniciar.", 12, Ink,
            FontWeights.SemiBold, new Thickness(0, 7, 0, 3));
        _headlineFile.TextTrimming = TextTrimming.CharacterEllipsis;
        info.Children.Add(_headlineFile);
        _headlineStats = T("Sin análisis activo", 11, Muted);
        info.Children.Add(_headlineStats);
        row.Children.Add(info);
        var actions = new StackPanel { Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10,0,0,0) };
        _topScanPauseButton = ActionButton("Ⅱ  Pausar", false, (_, _) => ToggleScanPause());
        SetSemanticButton(_topScanPauseButton,"warn");
        _topScanPauseButton.MinWidth = 128;
        _topScanPauseButton.Visibility = Visibility.Collapsed;
        actions.Children.Add(_topScanPauseButton);
        _topScanStopButton = ActionButton("■  Detener", false, (_, _) => RequestStopCurrentOperation());
        SetSemanticButton(_topScanStopButton,"danger");
        _topScanStopButton.MinWidth = 128;
        _topScanStopButton.Visibility = Visibility.Collapsed;
        actions.Children.Add(_topScanStopButton);
        Grid.SetColumn(actions,1); row.Children.Add(actions);
        bar.Child = row;
        return bar;
    }

    private Border BuildDashboardActiveScan()
    {
        _dashboardActiveScan = new Border { Background = BluePale,
            BorderBrush = Blue, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14), Padding = new Thickness(17),
            Margin = new Thickness(0, 0, 0, 13), Visibility = Visibility.Collapsed };
        var stack = new StackPanel();
        _dashboardActiveTitle = T("ANÁLISIS EN CURSO", 21, Blue, FontWeights.Bold);
        stack.Children.Add(_dashboardActiveTitle);
        _dashboardActivePath = T("Esperando actividad…", 12, Ink,
            FontWeights.SemiBold, new Thickness(0,7,0,5));
        _dashboardActivePath.TextTrimming = TextTrimming.CharacterEllipsis;
        stack.Children.Add(_dashboardActivePath);
        _dashboardActiveProgress = new ProgressBar { Height = 9, Minimum=0, Maximum=100,
            IsIndeterminate = true, Foreground = Blue, Background = Outline,
            Margin = new Thickness(0,6,0,7) };
        stack.Children.Add(_dashboardActiveProgress);
        _dashboardActiveDetails = T("Preparando análisis…", 12, Muted);
        stack.Children.Add(_dashboardActiveDetails);
        var actions = new WrapPanel { Margin = new Thickness(0,10,0,0) };
        _dashboardActivePause = ActionButton("Ⅱ  Pausar", false, (_, _) => ToggleScanPause());
        SetSemanticButton(_dashboardActivePause,"warn");
        actions.Children.Add(_dashboardActivePause);
        _dashboardActiveStop = ActionButton("■  Detener", false, (_, _) => RequestStopCurrentOperation());
        SetSemanticButton(_dashboardActiveStop,"danger");
        actions.Children.Add(_dashboardActiveStop);
        actions.Children.Add(ActionButton("Ver análisis completo", false, (_, _) => Navigate("scan")));
        stack.Children.Add(actions);
        _dashboardActiveScan.Child = stack;
        return _dashboardActiveScan;
    }

    private void AddScanControlPreferences(StackPanel destination)
    {
        var card = Card(Surface, Outline);
        card.Margin = new Thickness(0, 13, 0, 0);
        var stack = new StackPanel();
        stack.Children.Add(T("Control y seguimiento del análisis", 16, Ink, FontWeights.Bold,
            new Thickness(0,0,0,9)));
        stack.Children.Add(T("Modo de pausa y trazabilidad", 12, Ink, FontWeights.SemiBold,
            new Thickness(0,0,0,5)));
        _scanModeOption = new ComboBox { MinWidth = 235, MaxWidth=290, Height = 34,
            ItemsSource = new[] { "Equilibrado", "Preciso" },
            SelectedItem = _settings.ScanControlMode };
        _scanModeOption.SelectionChanged += (_, _) =>
        {
            if (_scanModeOption.SelectedItem is not string mode) return;
            _settings.ScanControlMode = mode;
            SaveSettings();
        };
        stack.Children.Add(_scanModeOption);
        stack.Children.Add(T("Equilibrado: ClamAV analiza lotes de hasta 128 archivos; pausa al finalizar el lote actual. " +
            "Preciso: un archivo por lote, muestra el archivo en examen y permite pausar entre archivos, " +
            "pero puede ser mucho más lento porque recarga las firmas por cada archivo. " +
            "La reanudación es dentro de la misma sesión; detener descarta la cola restante.",
            11, Muted, FontWeights.Normal, new Thickness(0,7,0,13)));
        _floatingScanOption = OptionCheck("Mostrar barra flotante durante un análisis",
            _settings.ShowFloatingScanBar,
            "Puedes moverla, ocultarla y recuperar los detalles en PalClam. No envía información fuera del equipo.",
            SaveFloatingScanOptions);
        stack.Children.Add(_floatingScanOption);
        _floatingTopmostOption = OptionCheck("Mantener la barra flotante sobre otras ventanas",
            _settings.FloatingScanTopmost,
            "Opcional. La barra desaparece automáticamente al terminar o cancelar un análisis.",
            SaveFloatingScanOptions);
        stack.Children.Add(_floatingTopmostOption);
        var floatMode = new WrapPanel { Margin = new Thickness(0,0,0,11) };
        floatMode.Children.Add(T("Vista flotante", 12, Ink, FontWeights.SemiBold,
            new Thickness(0,8,10,0)));
        _floatingModeOption = new ComboBox { MinWidth = 160, Height = 34,
            ItemsSource = new[] { "Compacta", "Detallada" },
            SelectedItem = _settings.FloatingScanExpanded ? "Detallada" : "Compacta" };
        _floatingModeOption.SelectionChanged += (_, _) => SaveFloatingScanOptions();
        floatMode.Children.Add(_floatingModeOption);
        stack.Children.Add(floatMode);
        stack.Children.Add(ActionButton("Ver barra flotante ahora", false, (_, _) =>
        {
            if (!_scanRunning)
            {
                MessageBox.Show(this, "La barra flotante aparece durante un análisis. Inicia uno para probarla.",
                    "PalClam", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _floatingHiddenByUser = false;
            var floating = EnsureFloatingScanWindow();
            floating.ApplyAppearance(Surface, Ink, Purple, PurplePale, Outline, _settings.UiTextScale);
            floating.ShowForScan(_settings.FloatingScanTopmost);
            RefreshScanControlUx();
        }));
        card.Child = stack;
        destination.Children.Add(card);
    }

    private void SaveFloatingScanOptions()
    {
        if (_floatingScanOption is null || _floatingTopmostOption is null) return;
        _settings.ShowFloatingScanBar = _floatingScanOption.IsChecked == true;
        _settings.FloatingScanTopmost = _floatingTopmostOption.IsChecked == true;
        _settings.FloatingScanExpanded = _floatingModeOption?.SelectedItem as string == "Detallada";
        SaveSettings();
        if (!_settings.ShowFloatingScanBar)
        {
            _floatingHiddenByUser = true;
            _floatingScanWindow?.Hide();
        }
        else if (_scanRunning)
        {
            _floatingHiddenByUser = false;
            EnsureFloatingScanWindow().ShowForScan(_settings.FloatingScanTopmost);
        }
    }

    private FloatingScanWindow EnsureFloatingScanWindow() => _floatingScanWindow ??=
        new FloatingScanWindow(ToggleScanPause, RequestStopCurrentOperation,
            () => _floatingHiddenByUser = true, () =>
        {
            Show();
            WindowState = System.Windows.WindowState.Normal;
            Navigate("scan");
            Activate();
        });

    private void ToggleScanPause()
    {
        if (!_scanRunning || !_busy || _running is null || _running.IsCancellationRequested ||
            _scanPauseGate is null) return;
        if (_scanPauseGate.IsPauseRequested)
        {
            _scanPauseGate.Resume();
            _scanPausedBetweenBatches = false;
            if (_scanBatch is { WaitingForResume: true })
                _scanBatch = _scanBatch with { WaitingForResume = false };
            AppendLog("[CONTROL] Reanudar. Continúa el siguiente lote pendiente.");
        }
        else
        {
            _scanPauseGate.RequestPause();
            AppendLog("[CONTROL] Pausa solicitada. Se completará el lote actual antes de pausar.");
        }
        RefreshScanControlUx();
    }

    private void ReceiveScanBatch(long generation, ScanBatchStatus batch)
    {
        if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            // Descartar mensajes encolados de un análisis anterior o ya cerrado.
            if (!_scanRunning || generation != _scanGeneration) return;
            _scanPausedBetweenBatches = batch.WaitingForResume &&
                _scanPauseGate?.IsPauseRequested == true;
            _scanBatch = batch with { WaitingForResume = _scanPausedBetweenBatches };
            if (_scanPausedBetweenBatches)
            {
                Volatile.Write(ref _engineLiveSample, null);
                _displayFilesRate = 0;
                _displayLogicalRate = 0;
            }
            RefreshScanControlUx();
        }));
    }

    private string ScanControlStateText()
    {
        if (_running?.IsCancellationRequested == true && _scanRunning) return "DETENIENDO…";
        if (_scanPausedBetweenBatches && _scanPauseGate?.IsPauseRequested == true) return "PAUSADO";
        if (_scanPauseGate?.IsPauseRequested == true && _scanRunning) return "PAUSANDO TRAS LOTE…";
        return _scanRunning ? "ANÁLISIS EN CURSO" : "SIN ANÁLISIS ACTIVO";
    }

    private string ScanCurrentDisplay()
    {
        var batch = _scanBatch;
        if (batch is { WaitingForResume: true })
            return $"Próximo lote en espera: {System.IO.Path.GetFileName(batch.FirstPath)}";
        if (batch is not null && batch.FilesInBatch == 1)
            return "ARCHIVO EN EXAMEN: " + batch.FirstPath +
                " · " + (string.IsNullOrWhiteSpace(System.IO.Path.GetExtension(batch.FirstPath))
                    ? "sin extensión" : System.IO.Path.GetExtension(batch.FirstPath).ToUpperInvariant()) +
                (batch.FileLengthBytes.HasValue ? $" · ≈ {batch.FileLengthBytes.Value / 1048576.0:0.0} MiB" : "");
        if (batch is not null)
            return $"LOTE {batch.BatchNumber}: {batch.FilesInBatch} archivos enviados al motor; " +
                   $"primero: {System.IO.Path.GetFileName(batch.FirstPath)}";
        return "Preparando primer lote de archivos…";
    }

    private string ScanStatsDisplay()
    {
        var elapsed = _scanClock?.Elapsed ?? TimeSpan.Zero;
        var processed = Interlocked.Read(ref _filesReported);
        var errors = Interlocked.Read(ref _errorsReported);
        var threats = Interlocked.Read(ref _threatsReported);
        var elapsedText = elapsed.ToString(@"hh\:mm\:ss");
        return $"{processed:N0} resultados · {threats:N0} detecciones · {errors:N0} errores · " +
            $"{elapsedText} · {_displayFilesRate:0.0} resultados/s" +
            (string.IsNullOrWhiteSpace(_scanEtaMessage) ? "" : " · " + _scanEtaMessage);
    }

    private void RefreshScanControlUx()
    {
        if (_headlineState is null || _dockPath is null || _globalScanIndicator is null) return;
        var live = _scanRunning;
        var state = ScanControlStateText();
        var stopping = live && _running?.IsCancellationRequested == true;
        var paused = _scanPausedBetweenBatches && _scanPauseGate?.IsPauseRequested == true;
        var pauseRequested = live && _scanPauseGate?.IsPauseRequested == true;
        var canControl = live && _busy && _running is not null;
        var path = live ? ScanCurrentDisplay() :
            "Último archivo confirmado: " + (string.IsNullOrWhiteSpace(_lastReportedPath) ? "—" : _lastReportedPath);
        var stats = ScanStatsDisplay();
        _headlineState.Text = state;
        _headlineState.Foreground = paused || stopping ? Amber : live ? Blue : Muted;
        _headlineFile.Text = path;
        _headlineFile.ToolTip = path;
        _headlineStats.Text = stats;
        PalClam.UI.LocalizedUi.Bind(_globalScanIndicator, TextBlock.TextProperty,
            live ? (paused ? "Header.Paused" : stopping ? "Header.Stopping" :
            pauseRequested ? "Header.Pausing" : "Header.Scanning") : "Header.Local");
        _globalScanIndicator.Foreground = live ? (paused || stopping ? Amber : Blue) : Blue;
        _dockPath.Text = live ? path : "Consulta los últimos resultados e inicia un nuevo análisis.";
        _dockPath.ToolTip = path;
        // Mostrar controles solo cuando estén operativos; no confundir Pausar con Pausar Guard.
        foreach (var button in new[] { _topScanPauseButton, _dockPauseButton, _dashboardActivePause })
        {
            if (button is null) continue;
            button.Visibility = live ? Visibility.Visible : Visibility.Collapsed;
            button.IsEnabled = canControl && !stopping;
            button.Content = paused ? "▶  Reanudar" :
                pauseRequested ? "↶  Anular pausa" : "Ⅱ  Pausar";
            SetSemanticButton(button, pauseRequested ? "scan" : "warn");
        }
        if (_dashboardActiveScan is not null)
        {
            _dashboardActiveScan.Visibility = live ? Visibility.Visible : Visibility.Collapsed;
            _dashboardActiveTitle.Text = state;
            _dashboardActiveTitle.Foreground = paused || stopping ? Amber : Blue;
            _dashboardActivePath.Text = path;
            _dashboardActivePath.ToolTip = path;
            _dashboardActiveDetails.Text = stats + " · Último confirmado: " +
                (string.IsNullOrWhiteSpace(_lastReportedPath) ? "—" :
                    System.IO.Path.GetFileName(_lastReportedPath));
            _dashboardActiveStop.IsEnabled = canControl && !stopping;
            _dashboardActiveStop.Content = stopping ? "Deteniendo…" : "■  Detener";
            if (_scanProgressBar is not null)
            {
                _dashboardActiveProgress.IsIndeterminate = _scanProgressBar.IsIndeterminate && !paused && !stopping;
                if (!_dashboardActiveProgress.IsIndeterminate)
                    _dashboardActiveProgress.Value = _scanPercentVisible ?? 0;
            }
        }
        if (_floatingScanWindow is not null || (live && _settings.ShowFloatingScanBar))
        {
            var floating = EnsureFloatingScanWindow();
            floating.ApplyAppearance(Surface, Ink, Purple, PurplePale, Outline,
                _settings.UiTextScale);
            if (!live) floating.EndScan();
            else
            {
                if (_settings.ShowFloatingScanBar && !_floatingHiddenByUser)
                    floating.ShowForScan(_settings.FloatingScanTopmost);
                // Telemetría de ClamAV, no del sistema completo. Si no hay un
                // proceso activo en este instante, no mostrar muestras caducadas.
                var liveSample = paused || _scanPausedBetweenBatches ? null :
                    Volatile.Read(ref _engineLiveSample);
                var floatingStats = liveSample is null ? stats + " · CPU/RAM: sin muestra actual" :
                    stats + $" · ClamAV CPU {liveSample.MachineCpuPercent:0.0}%" +
                    $" · RAM {liveSample.WorkingSetBytes / 1048576.0:0} MiB";
                floating.UpdateScan(state, path, floatingStats,
                    string.IsNullOrWhiteSpace(_lastReportedPath) ? "—" : _lastReportedPath,
                    _scanPercentVisible, paused, pauseRequested && !paused, stopping,
                    canControl && !stopping,
                    _settings.FloatingScanExpanded);
            }
        }
    }

    private void CloseFloatingScanWindow()
    {
        _floatingScanWindow?.DisposeWindow();
        _floatingScanWindow = null;
    }
}
