// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Controls;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using PalClam.Localization;

namespace PalClam;

public sealed partial class MainWindow
{
    private Forms.NotifyIcon? _trayIcon;
    private readonly Dictionary<string, Icon> _trayIcons = new(StringComparer.OrdinalIgnoreCase);
    private DispatcherTimer? _trayTimer;
    private Forms.ToolStripMenuItem? _pauseGuardMenu;
    private Forms.ToolStripMenuItem? _clearAlertMenu;
    private bool _trayExitRequested;
    private bool _guardPaused;
    private bool _trayAlert;
    private bool _trayHasInitialized;
    private bool _trayHasShownCloseHint;
    private readonly Dictionary<Forms.ToolStripItem, string> _trayLabels = new();
    private CheckBox? _keepTrayOption;

    private void InitializeTray()
    {
        foreach (var name in new[] { "Idle", "Background", "Guard", "Scanning", "Paused", "Alert", "Updating", "Error" })
        {
            var resource = "PalClam.Tray." + name + ".ico";
            using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException("No existe el icono: " + resource);
            _trayIcons[name] = new Icon(resourceStream);
        }
        var menu = new Forms.ContextMenuStrip();
        void Add(string label, EventHandler action)
        {
            var item = menu.Items.Add(DisplayText.Translate(label), null, action);
            _trayLabels[item] = label;
        }
        menu.Opening += (_, _) =>
        {
            foreach (var pair in _trayLabels) pair.Key.Text = DisplayText.Translate(pair.Value);
            UpdateTrayState();
        };
        Add("Abrir PalClam", (_, _) => Dispatcher.BeginInvoke(new Action(ShowFromTray)));
        Add("Analizar un archivo...", (_, _) => Dispatcher.BeginInvoke(new Action(() =>
        {
            ShowFromTray(); Navigate("scan");
        })));
        Add("Centro de notificaciones", (_, _) => Dispatcher.BeginInvoke(new Action(() =>
        {
            ShowFromTray(); Navigate("alerts");
        })));
        _pauseGuardMenu = new Forms.ToolStripMenuItem("Pausar vigilancia") { Enabled = false };
        _pauseGuardMenu.Click += (_, _) => Dispatcher.BeginInvoke(new Action(async () => await ToggleGuardPauseAsync()));
        menu.Items.Add(_pauseGuardMenu);
        _clearAlertMenu = new Forms.ToolStripMenuItem("Reconocer alerta") { Enabled = false };
        _trayLabels[_clearAlertMenu] = "Reconocer alerta";
        _clearAlertMenu.Click += (_, _) => Dispatcher.BeginInvoke(new Action(() => { _trayAlert = false; _ransomwareAlert = false; UpdateTrayState(); }));
        menu.Items.Add(_clearAlertMenu);
        menu.Items.Add(new Forms.ToolStripSeparator());
        Add("Salir de PalClam y detener Guard", (_, _) => Dispatcher.BeginInvoke(new Action(ExitFromTray)));

        _trayIcon = new Forms.NotifyIcon
        {
            Icon = _trayIcons["Idle"],
            Text = "PalClam · Inactivo (no garantiza protección)",
            Visible = true,
            ContextMenuStrip = menu
        };
        _trayIcon.DoubleClick += (_, _) => Dispatcher.BeginInvoke(new Action(ShowFromTray));
        _trayHasInitialized = true;
        _trayTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _trayTimer.Tick += (_, _) => { UpdateTrayState(); RefreshGuardTelemetry(); RefreshRansomwareTelemetry(); };
        _trayTimer.Start();
        UpdateTrayState();
        ConfigureDiskLed();
    }

    private void ShowFromTray()
    {
        ShowActivated = true;
        Show(); ShowInTaskbar = true;
        WindowState = WindowState.Normal;
        Activate();
        UpdateTrayState();
    }

    private void ExitFromTray()
    {
        _trayExitRequested = true;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_trayExitRequested && _settings.MinimizeToTrayOnClose && _trayHasInitialized)
        {
            e.Cancel = true;
            Hide();
            RefreshHeaderAuroraMotion();
            ShowInTaskbar = false;
            UpdateTrayState();
            if (_trayIcon is not null && !_trayHasShownCloseHint)
            {
                _trayHasShownCloseHint = true;
                _trayIcon.BalloonTipTitle = DisplayText.Translate("PalClam sigue abierto");
                _trayIcon.BalloonTipText = DisplayText.Translate("Se ha minimizado a la bandeja. Guard solo vigila si está activado; usa «Salir» para cerrar completamente.");
                _trayIcon.ShowBalloonTip(4500);
            }
            return;
        }
        base.OnClosing(e);
        // Importante: después de OnClosing y solo si no se canceló el cierre.
        if (!e.Cancel) StopSelfDefense();
    }

    private Task ToggleGuardPauseAsync()
    {
        var guard = _guardService;
        if (guard is null)
        {
            _guardPaused = false;
            StartGuard(manual: false);
        }
        else if (guard.IsPaused)
        {
            guard.ResumeProcessing();
            _guardPaused = false;
            _guardStatus.Text = "Vigilancia y procesamiento reanudados.";
            _guardStatus.Foreground = Green;
        }
        else
        {
            guard.PauseProcessing();
            _guardPaused = guard.IsPaused;
            _guardStatus.Text = "Procesamiento pausado: los cambios siguen entrando en la cola " +
                "y puede haber pérdida de eventos si se llena. El análisis activo terminará.";
            _guardStatus.Foreground = Amber;
        }
        if (_guardPauseButton is not null)
        {
            _guardPauseButton.IsEnabled = _guardPaused || _guardService?.IsRunning == true;
            _guardPauseButton.Content = _guardPaused ? "▶  Reanudar vigilancia" : "Ⅱ  Pausar vigilancia";
        }
        RefreshGuardTelemetry();
        UpdateTrayState();
        return Task.CompletedTask;
    }

    private void MarkThreatForTray()
    {
        _trayAlert = true;
        UpdateTrayState();
        RenderDiskLed();
    }

    private void UpdateTrayState()
    {
        if (_trayIcon is null || !_trayHasInitialized) return;
        var guardActive = _guardService?.IsRunning == true;
        var scanning = _scanRunning || _mailActive || _outlookScanning;
        var updating = !scanning && (_checkingEngine || _installingAppUpdate);
        string mode;
        if (_trayAlert) mode = "Alert";
        else if (_scanRunning && _scanPausedBetweenBatches &&
            _scanPauseGate?.IsPauseRequested == true) mode = "ScanPaused";
        else if (scanning) mode = "Scanning";
        else if (updating) mode = "Updating";
        else if (_guardPaused || _guardService?.IsPaused == true) mode = "Paused";
        else if (_guardService?.IsDegraded == true ||
            !PalClam.Services.ClamAvLocator.IsValidScanner(PalClam.Services.ClamAvLocator.FindScanner()) ||
            !PalClam.Services.SignatureStore.HasSignatures()) mode = "Error";
        else if (guardActive) mode = "Guard";
        else if (!IsVisible) mode = "Background";
        else mode = "Idle";
        _trayIcon.Icon = _trayIcons[mode == "ScanPaused" ? "Paused" : mode];
        var description = mode switch
        {
            "Alert" => "Alerta pendiente: abre PalClam para comprobarla",
            "Scanning" => "Analizando archivos o correo",
            "Updating" => "Comprobando o actualizando componentes",
            "Paused" => "Guard pausado: cambios pendientes en cola",
            "ScanPaused" => "Análisis pausado entre lotes",
            "Guard" => "Guard vigilando cambios (NO bloquea archivos)",
            "Error" => "Motor, firmas o vigilancia requieren atención",
            "Background" => "En segundo plano: Guard no está activo",
            _ => "Inactivo: no hay vigilancia activa"
        };
        var localizedDescription = "PalClam · " + DisplayText.Translate(description);
        _trayIcon.Text = localizedDescription[..Math.Min(63, localizedDescription.Length)];
        if (_pauseGuardMenu is not null)
        {
            _pauseGuardMenu.Enabled = (guardActive || _guardPaused || _settings.GuardFolders.Count > 0) && !_busy;
            _pauseGuardMenu.Text = DisplayText.Translate(_guardPaused ? "Reanudar vigilancia" :
                guardActive ? "Pausar vigilancia" : "Iniciar vigilancia");
        }
        if (_clearAlertMenu is not null) _clearAlertMenu.Enabled = _trayAlert;
        TaskbarItemInfo ??= new TaskbarItemInfo();
        TaskbarItemInfo.ProgressState = mode switch
        {
            "Alert" => TaskbarItemProgressState.Error,
            "Error" => TaskbarItemProgressState.Error,
            "Scanning" => TaskbarItemProgressState.Indeterminate,
            "Paused" => TaskbarItemProgressState.Paused,
            "ScanPaused" => TaskbarItemProgressState.Paused,
            _ => TaskbarItemProgressState.None
        };
    }

    private void DisposeTray()
    {
        DisposeDiskLed();
        _trayTimer?.Stop();
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.ContextMenuStrip?.Dispose();
            _trayIcon.Dispose();
            _trayIcon = null;
        }
        foreach (var icon in _trayIcons.Values) icon.Dispose();
        _trayIcons.Clear();
        _trayLabels.Clear();
    }
}
