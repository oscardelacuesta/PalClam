// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Threading;
using System.Net.Http;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using PalClam.Infrastructure;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Dictionary<string, StackPanel> _pages = new();
    private readonly Dictionary<string, Button> _navButtons = new();
    private readonly Dictionary<string, StackPanel> _navGroupPanels = new();
    private readonly Dictionary<string, TextBlock> _navGroupIndicators = new();
    private readonly Dictionary<string, string> _navPageGroups = new();
    private Border _dashboardMoodBorder = null!;
    private TextBlock _dashboardMoodText = null!;
    private TextBlock _dashboardMoodDetail = null!;
    private TextBlock _dashboardEngineState = null!;
    private TextBlock _dashboardSignaturesState = null!;
    private TextBlock _dashboardGuardState = null!;
    private DispatcherTimer? _dashboardTimer;

    private ContentControl _pageHost = null!;
    private ScrollViewer _pageScroll = null!;
    // Barra de comandos situada fuera del ScrollViewer: visible durante todo el análisis.
    private Border _scanActionDock = null!;
    private TextBlock _signatureMetadata = null!;
    private TextBlock _signatureHealthText = null!;
    private TextBlock _explorerIntegrationText = null!;
    private bool _readingSignatures;
    private TextBlock _scanStage = null!;
    private TextBlock _scanCurrentFile = null!;
    private TextBlock _scanEstimate = null!;
    private TextBlock _scanProgressText = null!;
    private TextBlock _scanProgressContext = null!;
    private Border _scanFinalCard = null!;
    private TextBlock _scanFinalTitle = null!;
    private TextBlock _scanFinalDetails = null!;
    private readonly ScanResultRateTracker _scanResultRate = new();
    private double? _scanPercentVisible;
    private string _scanEtaMessage = "";
    private TextBlock _scanProcessedText = null!;
    private TextBlock _scanThreatsText = null!;
    private TextBlock _scanErrorsText = null!;
    private TextBlock _scanElapsedText = null!;
    private ProgressBar _scanProgressBar = null!;
    // Telemetría de RC7. No se confunden bytes lógicos con lectura del disco.
    private TextBlock _scanFilesRateText = null!;
    private TextBlock _scanLogicalRateText = null!;
    private TextBlock _scanCpuText = null!;
    private TextBlock _scanRamText = null!;
    private TextBlock _scanEngineActivityText = null!;
    private TextBlock _scanPerformanceNote = null!;
    private CheckBox _performanceOption = null!;
    private ComboBox _scanPriorityOption = null!;
    private EngineProcessSample? _engineLiveSample;
    private bool _activeScanMetrics;
    private string _activeScanPriority = "Equilibrado";
    private long _scanLogicalBytes;
    private long _scanLastResultElapsedTicks;
    private long _scanPeakMemory;
    private double _scanCpuSum;
    private double _scanCpuPeak;
    private int _scanCpuSamples;
    private long _sampledFiles;
    private long _sampledBytes;
    private TimeSpan _sampledAt;
    private double _displayFilesRate;
    private double _displayLogicalRate;
    private double _longestSilence;
    private readonly ScanPerformanceSeries _livePerformance = new();
    private long _lastChartSampleTicks;
    private Polyline _liveCpuPolyline = null!;
    private Polyline _liveRamPolyline = null!;
    private Polyline _liveRatePolyline = null!;
    private string _scanEngineVersion = "";
    private string _scanSignatureSnapshot = "";
    private string _scanConfigurationId = "";
    private string _activeScanControlMode = "Equilibrado";

    private ComboBox _resultFilter = null!;
    private ListView _scanResultsList = null!;
    private ListView _historyList = null!;
    private readonly List<ScanFileEvent> _recentResults = new();
    private readonly ConcurrentQueue<ScanFileEvent> _pendingResults = new();
    private readonly ConcurrentQueue<string> _pendingMessages = new();
    private DispatcherTimer? _scanUiTimer;
    private Task<FileEstimate>? _estimateTask;
    private CancellationTokenSource? _estimateCancellation;
    private Stopwatch? _scanClock;
    private long _filesReported;
    private long _threatsReported;
    private long _errorsReported;
    private long _engineSummaryScanned = -1;
    private long _engineSummaryInfected = -1;
    private long _estimateCount = -1;
    private long _inaccessibleCount;
    private bool _estimateTruncated;
    private bool _scanRunning;
    private bool _scanFinishedNormally;
    private int _pendingResultCount;
    private string _scanLogPath = "";
    private string _lastReportedPath = "";
    private Button _openScanReport = null!;
    private TextBox _targetBox = null!;
    private TextBox _engineBox = null!;
    private TextBox _console = null!;
    private TextBlock _status = null!;
    private TextBlock _detections = null!;
    private TextBlock _lastAnalysis = null!;
    private TextBlock _engineStatus = null!;
    private Button _quickButton = null!;
    private Button _profileButton = null!;
    private Button _selectedButton = null!;
    private Button _updateButton = null!;
    private Button _engineUpdateButton = null!;
    private CheckBox _autoEngine = null!;
    private ProgressBar _engineDownloadProgress = null!;
    private TextBlock _engineUpdateStatus = null!;
    private bool _checkingEngine;
    private Button _cancelButton = null!;
    private Button _retryScanButton = null!;
    private TextBlock _scanActionHint = null!;
    private bool _scanEverStarted;
    private Button _historyButton = null!;
    private Button _checkAppUpdatesButton = null!;
    private Button _installAppUpdateButton = null!;
    private CheckBox _checkUpdatesOnStartup = null!;
    private CheckBox _updateSignaturesAutomatically = null!;
    private CheckBox _installUpdatesAutomatically = null!;
    private TextBlock _appUpdateStatus = null!;
    private ProgressBar _appDownloadProgress = null!;
    private ReleaseManifest? _availableRelease;
    private CancellationTokenSource? _appUpdateCancellation;
    private bool _checkingAppUpdates;
    private bool _installingAppUpdate;
    private bool _pendingAutomaticInstall;
    private CancellationTokenSource? _running;
    private bool _busy;
    private CheckBox _scanArchivesOption = null!;
    private CheckBox _detectPuaOption = null!;
    private CheckBox _showCleanOption = null!;
    private CheckBox _consoleExpandedOption = null!;
    private ComboBox _checkIntervalOption = null!;
    private TextBox _excludedPathsBox = null!;
    private TextBlock _optionsStatus = null!;
    private Expander _technicalConsoleExpander = null!;
    private CheckBox _explorerOption = null!;
    private bool _settingExplorerOption;
    private TextBox _diagnosticBox = null!;
    private ListView _quarantineList = null!;
    private TextBlock _quarantineStatus = null!;
    private ComboBox _scheduleFrequency = null!;
    private ComboBox _scheduleTarget = null!;
    private TextBox _scheduleTime = null!;
    private TextBlock _scheduleStatus = null!;
    private CheckBox _notificationsOption = null!;
    private DesktopNotifications? _desktopNotifications;

    // Cache compartida de paletas: no congelar pinceles porque cambian de color en vivo.
    private static readonly Dictionary<string, SolidColorBrush> UiBrushes =
        new(StringComparer.OrdinalIgnoreCase);
    private static string ActiveTheme = AppearanceCatalog.DefaultId;
    private LinearGradientBrush? _heroGradient;
    private ComboBox _themeOption = null!;
    private ComboBox _textScaleOption = null!;
    private Button _topScanStopButton = null!;
    private TextBlock _footerVersionAndDate = null!;

    private static readonly Brush Ink = B("#172034");
    private static readonly Brush Muted = B("#68738C");
    private static readonly Brush Purple = B("#6940B3");
    private static readonly Brush PurplePale = B("#F0EAFB");
    private static readonly Brush Outline = B("#E5E9F2");
    private static readonly Brush Surface = B("#FFFFFF");
    private static readonly Brush BackgroundColor = B("#F4F6FC");
    private static readonly Brush Blue = B("#2167D3");
    private static readonly Brush BluePale = B("#EAF2FF");
    private static readonly Brush Green = B("#16815C");
    private static readonly Brush GreenPale = B("#E5F6EE");
    private static readonly Brush Amber = B("#9F650A");
    private static readonly Brush AmberPale = B("#FFF3D9");
    private static readonly Brush Red = B("#C82B42");
    private static readonly Brush RedPale = B("#FDE9ED");
    private static readonly Brush SidebarColor = B("#FBFCFF");
    private static readonly Brush Slate = B("#293552");

    public MainWindow(string? launchTarget = null, bool startHidden = false)
    {
        _settings = SettingsStore.Load();
        PalClam.Localization.UiText.Current.SetLanguage(PalClam.Localization.TextCatalog.ResolvePreference(_settings.UiLanguage));
        ApplyAppearancePalette(_settings.UiTheme);
        Title = "PalClam 1.2.2 · Guard 2.0";
        Width = 1220;
        Height = 830;
        MinWidth = 960;
        MinHeight = 660;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        if (startHidden)
        {
            // Proceso WPF vivo, NotifyIcon visible; evitar ventana y foco en el inicio de sesión.
            ShowInTaskbar = false;
            ShowActivated = false;
            WindowState = System.Windows.WindowState.Minimized;
            Loaded += (_, _) =>
            {
                Hide();
                RefreshHeaderAuroraMotion();
                UpdateTrayState();
            };
        }
        IsVisibleChanged += (_, _) => RefreshHeaderAuroraMotion();
        StateChanged += (_, _) => RefreshHeaderAuroraMotion();
        Background = BackgroundColor;
        Foreground = Ink;
        FontFamily = new FontFamily("Segoe UI");
        FontSize = 13;
        Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/PalClam-256.png"));
        AllowDrop = true;
        DragEnter += OnFilesDragEnter;
        Drop += OnFilesDrop;
        PreviewKeyDown += OnMainKeyDown;
        Content = BuildLayout();
        ApplyAppearanceTextScale();
        _desktopNotifications = new DesktopNotifications(this);
        Closed += (_, _) =>
        {
            _inspectorReadCancellation?.Cancel();
            try { _running?.Cancel(); } catch (ObjectDisposedException) { }
            _scanPauseGate?.Resume();
            CloseFloatingScanWindow();
            _desktopNotifications?.Dispose();
        };
        InitializeTray();
        InitializeSelfDefense();
        if (_settings.Ransomware.StartWithApp) StartRansomware();
        StartRemovableMonitoring();
        _dashboardTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _dashboardTimer.Tick += (_, _) =>
        {
            RefreshDashboardUi();
            _ = RefreshDashboardInsightsAsync();
            if (_footerVersionAndDate is not null)
                _footerVersionAndDate.Text = FooterVersionAndDate();
        };
        _dashboardTimer.Start();
        Closed += (_, _) => _dashboardTimer?.Stop();
        StartMachineStatusMonitoring();
        Closed += (_, _) => _machineStatusTimer?.Stop();

        // Un motor incluido y verificado tiene prioridad sobre rutas guardadas de versiones antiguas.
        _engineBox.Text = ClamAvLocator.FindScanner() is { Length: > 0 } discovered
            ? discovered : ClamAvLocator.IsValidScanner(_settings.ClamScanPath)
                ? _settings.ClamScanPath : "";
        _targetBox.Text = _settings.LastTarget;
        RefreshEngineStatus();
        LoadLastScan();
        AppendLog("PalClam 1.2.2 iniciado. Guard es una vigilancia opcional de cambios, no bloqueo preventivo; no sustituye Defender.");
        AppendLog("Motor ClamAV incluido para usar sin Internet; se actualiza desde PalClam.");
        try
        {
            var seeded = SignatureStore.SeedFromInstaller();
            if (seeded > 0) AppendLog($"[OFFLINE] {seeded} bases de firmas iniciales incorporadas al perfil del usuario.");
            if (SignatureStore.HasSignatures())
            {
                var date = SignatureStore.OfflineDailyDateUtc();
                AppendLog("[OFFLINE] Hay firmas locales. Su fecha puede ser anterior a las últimas amenazas; actualiza al conectar Internet." +
                    (date is null ? "" : $" Base incluida el {date.Value:dd/MM/yyyy}."));
            }
            else AppendLog("[AVISO] No existen firmas completas. El antivirus NO puede analizar hasta obtenerlas.");
        }
        catch (Exception ex) { AppendLog("[AVISO] No se pudieron preparar las firmas offline: " + ex.Message); }
        AppendLog("Firmas locales: " + SignatureStore.DatabaseDirectory);
        _ = RefreshSignatureMetadataAsync();
        Loaded += async (_, _) =>
        {
            // Mantener el arranque opcional de Guard también para solicitudes del Explorador.
            await StartGuardOnLaunchAsync();
            // Prioridad absoluta al usuario que pidió analizar desde el Explorador.
            // No retrasar el examen esperando consultas a Internet o instalación de motor.
            if (!string.IsNullOrWhiteSpace(launchTarget))
            {
                Navigate("scan");
                AppendLog("[EXPLORADOR] Solicitud de análisis: " + launchTarget);
                await StartScanAsync(launchTarget);
                return;
            }
            // El orden evita iniciar FreshClam mientras instalamos el motor.
            await CheckAndInstallEngineAsync(manual: false);
            if (_settings.AutoUpdateSignatures && !_busy && !_installingAppUpdate &&
                ClamAvLocator.IsValidScanner(_engineBox.Text) &&
                SignatureHealthService.AutoCheckDue(_settings.LastSignatureCheckUtc,
                    _settings.LastSignatureAttemptUtc, _settings.UpdateIntervalHours, DateTimeOffset.UtcNow))
                await UpdateSignaturesAsync();
            if (_settings.CheckAppUpdatesOnStartup)
                _ = CheckAppUpdatesAsync(manual: false);
            var maintenanceTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromHours(1) };
            maintenanceTimer.Tick += async (_, _) =>
            {
                if (_busy || _installingAppUpdate || _checkingEngine) return;
                if (_settings.AutoUpdateEngine && DateTimeOffset.UtcNow - _settings.LastEngineCheckUtc >= TimeSpan.FromHours(_settings.UpdateIntervalHours))
                    await CheckAndInstallEngineAsync(manual: false);
                if (_settings.AutoUpdateSignatures && !_busy &&
                    ClamAvLocator.IsValidScanner(_engineBox.Text) &&
                    SignatureHealthService.AutoCheckDue(_settings.LastSignatureCheckUtc,
                        _settings.LastSignatureAttemptUtc, _settings.UpdateIntervalHours, DateTimeOffset.UtcNow))
                    await UpdateSignaturesAsync();
            };
            maintenanceTimer.Start();
        };
    }

    // Cada color de referencia se resuelve en una única instancia mutable.
    // Cuando el usuario cambia de tema también cambian tarjetas, menús y páginas ocultas.
    private static Brush B(string html)
    {
        if (!UiBrushes.TryGetValue(html, out var brush))
        {
            brush = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(
                AppearanceCatalog.MapColor(ActiveTheme, html))!);
            UiBrushes.Add(html, brush);
        }
        return brush;
    }

    private UIElement BuildLayout()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(72) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(37) });

        var header = new Border
        {
            Background = Surface, BorderBrush = Outline,
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
        // Aurora limitada a esta franja, detrás del escudo, nombre y Preview.
        var headerLayers = new Grid { ClipToBounds = true };
        headerLayers.Children.Add(CreateHeaderAurora());
        var headerRow = new Grid { Margin = new Thickness(28, 0, 30, 0) };
        headerLayers.Children.Add(headerRow);
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var brand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        brand.Children.Add(new System.Windows.Controls.Image
        {
            Source = new BitmapImage(new Uri("pack://application:,,,/Assets/PalClam-256.png")),
            Width = 43, Height = 43, Stretch = Stretch.Uniform, Margin = new Thickness(0, 0, 13, 0)
        });
        var brandTitles = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        brandTitles.Children.Add(T("PalClam", 23, Ink, FontWeights.Bold));
        brandTitles.Children.Add(LT("Header.Tagline", 9, Muted, FontWeights.SemiBold));
        brand.Children.Add(brandTitles);
        brand.Children.Add(new Border
        {
            Background = PurplePale, CornerRadius = new CornerRadius(12),
            Margin = new Thickness(16, 0, 0, 0), Padding = new Thickness(11, 5, 11, 5),
            VerticalAlignment = VerticalAlignment.Center,
            Child = T("v1.2.2", 10, Purple, FontWeights.Bold)
        });
        // La marca abre el panel principal, pero ya no se presenta como botón
        // para evitar un hover artificial en el título al pasar el ratón.
        var brandHome = new Border
        {
            Child = brand,
            Background = Brushes.Transparent,
            Padding = new Thickness(0),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Focusable = false
        };
        PalClam.UI.LocalizedUi.Bind(brandHome, FrameworkElement.ToolTipProperty, "Header.HomeTip");
        PalClam.UI.LocalizedUi.Bind(brandHome, System.Windows.Automation.AutomationProperties.NameProperty, "Header.HomeTip");
        brandHome.MouseLeftButtonUp += (_, _) => Navigate("dashboard");
        headerRow.Children.Add(brandHome);
        var headRight = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        _globalScanIndicator = LT("Header.Local", 10, Blue, FontWeights.Bold);
        headRight.Children.Add(new Border
        {
            Background = BluePale, CornerRadius = new CornerRadius(10),
            Padding = new Thickness(11, 6, 11, 6), Margin = new Thickness(0, 0, 14, 0),
            Child = _globalScanIndicator
        });
        // Acceso directo permanente a la ayuda, al lado de Ajustes.
        // Conserva el acceso del menú Herramientas y Mayús+F1.
        var helpHeaderButton = LocalizedAction("Header.Help", false, (_, _) => Navigate("help"));
        helpHeaderButton.Margin = new Thickness(0, 0, 9, 0);
        helpHeaderButton.MinWidth = 80;
        PalClam.UI.LocalizedUi.Bind(helpHeaderButton, FrameworkElement.ToolTipProperty, "Header.HelpTip");
        PalClam.UI.LocalizedUi.Bind(helpHeaderButton, System.Windows.Automation.AutomationProperties.NameProperty, "Header.HelpName");
        var settingsHeaderButton = LocalizedAction("Header.Settings", false, (_, _) => Navigate("options"));
        settingsHeaderButton.Margin = new Thickness(0, 0, 12, 0);
        settingsHeaderButton.MinWidth = 100;
        PalClam.UI.LocalizedUi.Bind(settingsHeaderButton, FrameworkElement.ToolTipProperty, "Header.SettingsTip");
        PalClam.UI.LocalizedUi.Bind(settingsHeaderButton, System.Windows.Automation.AutomationProperties.NameProperty, "Header.SettingsTip");
        // La web sigue en el pie. La cabecera dedica ese espacio al selector rápido de tema.
        headRight.Children.Add(CreateQuickThemeButton());
        headRight.Children.Add(helpHeaderButton);
        headRight.Children.Add(settingsHeaderButton);
        Grid.SetColumn(headRight, 1);
        headerRow.Children.Add(headRight);
        header.Child = headerLayers;
        root.Children.Add(header);

        var body = new Grid();
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(246) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        var navigation = new Border
        {
            Background = SidebarColor, BorderBrush = Outline,
            BorderThickness = new Thickness(0, 0, 1, 0)
        };
        var sidebarLayout = new Grid();
        sidebarLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        sidebarLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        sidebarLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var navContent = new StackPanel { Margin = new Thickness(14, 20, 14, 8) };
        navContent.Children.Add(T("CENTRO DE SEGURIDAD", 10, Muted, FontWeights.Bold,
            new Thickness(12, 0, 0, 13)));
        navContent.Children.Add(NavGroup("INICIO Y ANÁLISIS", "base", Blue,
            ("⌂", "Panel principal", "dashboard"),
            ("♥", "Security Health 360", "health"),
            ("⌕", "Análisis de archivos", "scan"),
            ("▤", "Inspector de archivos", "inspector"),
            ("◈", "Motor y firmas", "signatures"),
            ("↻", "Actualizaciones", "updates")));
        navContent.Children.Add(NavGroup("PROTECCIÓN", "protect", Green,
            ("◉", "PalClam Guard", "guard"),
            ("◇", "Anti-ransomware", "ransomware"),
            ("▣", "USB y Descargas", "devices"),
            ("!", "Centro de notificaciones", "alerts"),
            ("♧", "Self-Defense · Monitor", "selfdefense"),
            ("▣", "Remediation Center", "quarantine"),
            ("◷", "Análisis programados", "schedule")));
        navContent.Children.Add(NavGroup("CORREO", "mailgroup", Purple,
            ("✉", "PalClam Mail", "mail"),
            ("▤", "Outlook en línea", "outlook")));
        navContent.Children.Add(NavGroup("INTELIGENCIA ARTIFICIAL", "aigroup", Purple,
            ("✦", "PalClam AI · Analista y proveedores", "ai")));
        navContent.Children.Add(NavGroup("HERRAMIENTAS", "tools", Slate,
            ("▥", "Historial local", "history"),
            ("✚", "Diagnóstico", "diagnostics"),
            ("✧", "Consejos y estado", "advice"),
            ("?", "Ayuda · Funciones y ventajas", "help"),
            ("ⓘ", "Acerca de PalClam", "about")));
        // Tarjeta compacta: el personaje del autor ofrece consejos, sin adornos ni falsas alertas.
        navContent.Children.Add(BuildCharacterAdviceCard());
        var navScroll = new ScrollViewer
        {
            // No mostrar la barra vertical gris: los grupos permiten plegar el menú.
            // Si la ventana es muy pequeña, la rueda y el teclado permiten desplazarse.
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = true,
            Content = navContent
        };
        Grid.SetRow(navScroll, 0);
        sidebarLayout.Children.Add(navScroll);
        var note = new Border
        {
            Background = B("#F0F3FA"), CornerRadius = new CornerRadius(12),
            Margin = new Thickness(14, 6, 14, 16), Padding = new Thickness(13, 11, 13, 11)
        };
        var noteStack = new StackPanel();
        noteStack.Children.Add(T("●  CONTROL EN TU EQUIPO", 10, Green, FontWeights.Bold));
        noteStack.Children.Add(T("Guard opcional · No sustituye Defender", 10, Muted,
            FontWeights.Normal, new Thickness(0, 5, 0, 0)));
        note.Child = noteStack;
        // Configuración como sección independiente; acceso permanente y sin scroll.
        var settingsShortcut = Nav("⚙", "Opciones y personalización", "options");
        settingsShortcut.Margin = new Thickness(14, 7, 14, 2);
        settingsShortcut.Padding = new Thickness(12, 13, 12, 13);
        settingsShortcut.Background = PurplePale;
        settingsShortcut.BorderBrush = Outline;
        settingsShortcut.BorderThickness = new Thickness(1);
        settingsShortcut.ToolTip = "Abrir ajustes: temas, análisis, barra flotante, actualizaciones";
        Grid.SetRow(settingsShortcut, 1);
        sidebarLayout.Children.Add(settingsShortcut);
        Grid.SetRow(note, 2);
        sidebarLayout.Children.Add(note);
        navigation.Child = sidebarLayout;
        body.Children.Add(navigation);

        // El contenido de cada página puede desplazarse, pero los mandos de análisis no.
        var contentPane = new Grid();
        contentPane.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        contentPane.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetColumn(contentPane, 1);
        _pageScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Grid.SetRow(_pageScroll, 0);
        _pageHost = new ContentControl();
        _pageScroll.Content = _pageHost;
        contentPane.Children.Add(_pageScroll);
        _scanActionDock = BuildScanActionDock();
        Grid.SetRow(_scanActionDock, 1);
        contentPane.Children.Add(_scanActionDock);
        body.Children.Add(contentPane);

        StackPanel Page() => new() { Margin = new Thickness(27, 22, 27, 26) };
        var content = Page();
        var scanPage = Page();
        var inspectorPage = Page();
        var signaturePage = Page();
        var updatesPage = Page();
        var historyPage = Page();
        var optionsPage = Page();
        var diagnosticPage = Page();
        var quarantinePage = Page();
        var schedulePage = Page();
        var guardPage = Page();
        var securityPage = Page();
        var healthPage = Page();
        var devicePage = Page();
        var mailPage = Page();
        var outlookPage = Page();
        var aboutPage = Page();
        var helpPage = Page();
        var aiPage = Page();
        var advicePage = Page();
        var selfDefensePage = Page();
        var ransomwarePage = Page();
        _pages.Add("dashboard", content);
        _pages.Add("scan", scanPage);
        _pages.Add("inspector", inspectorPage);
        _pages.Add("signatures", signaturePage);
        _pages.Add("updates", updatesPage);
        _pages.Add("history", historyPage);
        _pages.Add("options", optionsPage);
        _pages.Add("diagnostics", diagnosticPage);
        _pages.Add("quarantine", quarantinePage);
        _pages.Add("schedule", schedulePage);
        _pages.Add("guard", guardPage);
        _pages.Add("alerts", securityPage);
        _pages.Add("health", healthPage);
        _pages.Add("devices", devicePage);
        _pages.Add("mail", mailPage);
        _pages.Add("outlook", outlookPage);
        _pages.Add("about", aboutPage);
        _pages.Add("help", helpPage);
        _pages.Add("ai", aiPage);
        _pages.Add("advice", advicePage);
        _pages.Add("selfdefense", selfDefensePage);
        _pages.Add("ransomware", ransomwarePage);

        // Dashboard compacto: jerarquía de seguridad primero, acciones después.
        BuildProfessionalDashboard(content);
        RefreshDashboardUi();

        scanPage.Children.Add(BuildScanHeadline());
        scanPage.Children.Add(BuildSmartScanPanel());

        var selectionCard = Card(Surface, Outline);
        var selection = new StackPanel();
        selection.Children.Add(T("Archivo o carpeta", 14, Ink, FontWeights.SemiBold, new Thickness(0, 0, 0, 7)));
        var targetRow = new Grid();
        targetRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        targetRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        targetRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _targetBox = Input("Selecciona o escribe una ruta local...");
        targetRow.Children.Add(_targetBox);
        var pickFile = ActionButton("Archivo", false, (_, _) => PickFile());
        pickFile.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(pickFile, 1);
        targetRow.Children.Add(pickFile);
        var pickFolder = ActionButton("Carpeta", false, (_, _) => PickFolder());
        pickFolder.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(pickFolder, 2);
        targetRow.Children.Add(pickFolder);
        selection.Children.Add(targetRow);
        selection.Children.Add(T("También puedes arrastrar aquí un archivo o carpeta desde el Explorador. " +
            "Atajos: Ctrl+O (archivo) · Ctrl+Mayús+O (carpeta).", 11, Muted,
            FontWeights.Normal, new Thickness(0, 8, 0, 0)));
        selectionCard.Child = selection;
        scanPage.Children.Add(selectionCard);
        scanPage.Children.Add(BuildScanProgressPanel());
        scanPage.Children.Add(BuildScanResultsPanel());

        var engineCard = Card(Surface, Outline);
        engineCard.Margin = new Thickness(0, 13, 0, 0);
        var engineStack = new StackPanel();
        engineStack.Children.Add(T("Motor de análisis", 14, Ink, FontWeights.SemiBold, new Thickness(0, 0, 0, 7)));
        var engineRow = new Grid();
        engineRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        engineRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _engineBox = Input("El motor se descarga automáticamente; ruta de clamscan.exe...");
        _engineBox.TextChanged += (_, _) => RefreshEngineStatus();
        engineRow.Children.Add(_engineBox);
        var pickEngine = ActionButton("Examinar", false, (_, _) => PickEngine());
        pickEngine.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(pickEngine, 1);
        engineRow.Children.Add(pickEngine);
        engineStack.Children.Add(engineRow);
        _engineStatus = T("Motor pendiente de configurar", 11, Muted, FontWeights.Normal, new Thickness(0, 7, 0, 0));
        engineStack.Children.Add(_engineStatus);
        _engineUpdateStatus = T("Motor offline incorporado. Comprueba aquí las versiones más nuevas cuando tengas Internet.",
            11, Muted, FontWeights.Normal, new Thickness(0, 5, 0, 0));
        engineStack.Children.Add(_engineUpdateStatus);
        _engineDownloadProgress = new ProgressBar
        {
            Height = 6, Minimum = 0, Maximum = 100, Visibility = Visibility.Collapsed,
            Foreground = Purple, Background = Outline, Margin = new Thickness(0, 9, 0, 7)
        };
        engineStack.Children.Add(_engineDownloadProgress);
        var signaturesInfo = T("Firmas propias: " + SignatureStore.DatabaseDirectory,
            11, Muted, FontWeights.Normal, new Thickness(0, 5, 0, 0));
        engineStack.Children.Add(signaturesInfo);
        _signatureMetadata = T("Firmas: comprobando las bases locales...", 11, Ink, FontWeights.Normal,
            new Thickness(0, 8, 0, 5));
        engineStack.Children.Add(_signatureMetadata);
        _signatureHealthText = T("Comprobando antigüedad estimada y últimas actualizaciones...", 12,
            Amber, FontWeights.SemiBold, new Thickness(0, 9, 0, 7));
        _signatureHealthText.TextWrapping = TextWrapping.Wrap;
        engineStack.Children.Add(_signatureHealthText);
        var engineActions = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
        _updateButton = ActionButton("↻  Actualizar firmas (FreshClam)", false, (_, _) => _ = UpdateSignaturesAsync());
        engineActions.Children.Add(_updateButton);
        _engineUpdateButton = ActionButton("↓  Comprobar / instalar motor ClamAV", false,
            (_, _) => _ = CheckAndInstallEngineAsync(manual: true));
        engineActions.Children.Add(_engineUpdateButton);
        engineStack.Children.Add(engineActions);
        engineStack.Children.Add(T("Las comprobaciones automáticas se configuran en Opciones.",
            11, Muted, FontWeights.Normal, new Thickness(0, 11, 0, 0)));
        engineStack.Children.Add(ActionButton("⚙  Configurar actualizaciones", false, (_, _) => OpenOptionsTab(3)));
        engineCard.Child = engineStack;
        signaturePage.Children.Add(engineCard);
        BuildSignatureCenterPanel(signaturePage);

        var appUpdateCard = Card(Surface, Outline);
        appUpdateCard.Margin = new Thickness(0, 13, 0, 0);
        var appUpdateStack = new StackPanel();
        appUpdateStack.Children.Add(T("Actualizaciones de PalClam y motor ClamAV", 14, Ink, FontWeights.SemiBold,
            new Thickness(0, 0, 0, 7)));
        appUpdateStack.Children.Add(T("PalClam se actualiza con su instalador; ClamAV y las firmas se actualizan por separado desde esta interfaz.",
            12, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 9)));
        appUpdateStack.Children.Add(T("Personaliza las comprobaciones y la instalación automática en Opciones.",
            11, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 8)));
        appUpdateStack.Children.Add(ActionButton("⚙  Preferencias", false, (_, _) => OpenOptionsTab(3)));
        _appUpdateStatus = T("No se ha comprobado la disponibilidad de nuevas versiones.", 11, Muted);
        appUpdateStack.Children.Add(_appUpdateStatus);
        _appDownloadProgress = new ProgressBar
        {
            Height = 5, Minimum = 0, Maximum = 100, Value = 0,
            Visibility = Visibility.Collapsed, Background = Outline, Foreground = Purple,
            Margin = new Thickness(0, 8, 0, 6)
        };
        appUpdateStack.Children.Add(_appDownloadProgress);
        var updateControls = new WrapPanel { Margin = new Thickness(0, 9, 0, 0) };
        _checkAppUpdatesButton = ActionButton("↻  Buscar versión nueva", false,
            (_, _) => _ = CheckAppUpdatesAsync(manual: true));
        _installAppUpdateButton = ActionButton("↓  Descargar e instalar versión nueva", true,
            (_, _) => _ = InstallAvailableUpdateAsync());
        _installAppUpdateButton.IsEnabled = false;
        _installAppUpdateButton.ToolTip = "Solo se activa cuando se publica una versión más reciente y verificada.";
        updateControls.Children.Add(_checkAppUpdatesButton);
        updateControls.Children.Add(_installAppUpdateButton);
        appUpdateStack.Children.Add(updateControls);
        appUpdateCard.Child = appUpdateStack;
        updatesPage.Children.Add(appUpdateCard);

        var logHeader = new Grid { Margin = new Thickness(0, 19, 0, 8) };
        logHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        logHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        logHeader.Children.Add(T("Actividad y resultados", 16, Ink, FontWeights.Bold));
        var clearButton = ActionButton("Limpiar consola", false, (_, _) => _console.Clear());
        clearButton.Padding = new Thickness(9, 6, 9, 6);
        Grid.SetColumn(clearButton, 1);
        logHeader.Children.Add(clearButton);
        scanPage.Children.Add(logHeader);

        var logCard = Card(B("#182238"), B("#182238"));
        logCard.Padding = new Thickness(12);
        _console = new TextBox
        {
            Background = Brushes.Transparent, Foreground = B("#E7EDF9"), BorderThickness = new Thickness(0),
            FontFamily = new FontFamily("Consolas"), FontSize = 11.5, AcceptsReturn = true,
            IsReadOnly = true, TextWrapping = TextWrapping.Wrap,
            Height = 148, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        logCard.Child = _console;
        _technicalConsoleExpander = new Expander
        {
            Header = "Mostrar consola técnica (últimos mensajes)", IsExpanded = _settings.ShowTechnicalConsole,
            Margin = new Thickness(0, 0, 0, 3), Content = logCard,
            Foreground = Ink, FontWeight = FontWeights.SemiBold
        };
        scanPage.Children.Add(_technicalConsoleExpander);
        var historyRow = new WrapPanel { Margin = new Thickness(0, 9, 0, 0) };
        _historyButton = ActionButton("▤  Ver historial / exportar informe", false,
            (_, _) => new HistoryWindow(this).ShowDialog());
        historyRow.Children.Add(_historyButton);
        historyPage.Children.Add(T("Historial local", 23, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 8)));
        historyPage.Children.Add(T("Análisis anteriores guardados únicamente en este equipo. " +
            "Selecciona una fila y consulta el informe detallado o exporta el historial.",
            12, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 15)));
        historyPage.Children.Add(historyRow);
        var reloadHistory = ActionButton("↻  Actualizar historial", false, (_, _) => RefreshHistory());
        historyRow.Children.Add(reloadHistory);
        historyRow.Children.Add(ActionButton("◫  Rendimiento histórico", false,
            (_, _) => new PerformanceWindow(this).ShowDialog()));
        _historyList = CreateHistoryList();
        historyPage.Children.Add(_historyList);
        var showScanLog = ActionButton("Abrir informe detallado seleccionado", false, (_, _) =>
        {
            if (_historyList.SelectedItem is ScanRecord record && !string.IsNullOrWhiteSpace(record.LogPath) && File.Exists(record.LogPath))
                OpenLocalReport(record.LogPath);
            else MessageBox.Show(this, "El análisis seleccionado no tiene un informe detallado disponible.",
                "Historial", MessageBoxButton.OK, MessageBoxImage.Information);
        });
        historyPage.Children.Add(showScanLog);

        BuildOptionsPage(optionsPage);
        BuildDiagnosticsPage(diagnosticPage);
        BuildInspectorPage(inspectorPage);
        BuildQuarantinePage(quarantinePage);
        BuildSchedulingPage(schedulePage);
        BuildGuardPage(guardPage);
        BuildSecurityCenterPage(securityPage);
        BuildHealth360Page(healthPage);
        BuildDeviceGuardPage(devicePage);
        BuildMailPage(mailPage);
        BuildOutlookPage(outlookPage);
        BuildAboutPage(aboutPage);
        BuildHelpPage(helpPage);
        BuildAiPage(aiPage);
        BuildAdvicePage(advicePage);
        BuildSelfDefensePage(selfDefensePage);
        BuildRansomwarePage(ransomwarePage);
        Navigate("dashboard");

        var footer = new Border
        {
            Background = Surface, BorderBrush = Outline,
            BorderThickness = new Thickness(0, 1, 0, 0)
        };
        var footerRow = new Grid { Margin = new Thickness(21, 0, 21, 0) };
        footerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var footerLeft = T("●  Análisis local  ·  PalClam no sustituye la protección de Microsoft Defender",
            10, Muted);
        footerLeft.VerticalAlignment = VerticalAlignment.Center;
        footerRow.Children.Add(footerLeft);
        _footerVersionAndDate = T(FooterVersionAndDate(), 10, Muted);
        var footerRight = _footerVersionAndDate;
        footerRight.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(footerRight, 1);
        footerRow.Children.Add(footerRight);
        footer.Child = footerRow;
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);
        return root;
    }

    private static string FooterVersionAndDate() =>
        "1.2.2  ·  " + DateTime.Now.ToString("dd/MM/yyyy") + "  ·  GPLv2  ·  palentino.es";

    private void BuildQuarantinePage(StackPanel page)
    {
        page.Children.Add(T("Remediation Center PRO · Cuarentena", 23, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 8)));
        page.Children.Add(T("El aislamiento es siempre voluntario y solo se ofrece para archivos " +
            "detectados en el análisis actual. Los archivos se guardan con nombre opaco en tu perfil. " +
            "Puedes reanalizar el archivo aislado sin restaurarlo. Un resultado sin detecciones " +
            "NO confirma que sea seguro. Restaurar o eliminar requiere confirmación individual.",
            12, B("#966023"), FontWeights.Normal, new Thickness(0, 0, 0, 12)));
        var card = Card(Surface, Outline);
        var stack = new StackPanel();
        _quarantineStatus = T("Sin consultar", 12, Muted);
        stack.Children.Add(_quarantineStatus);
        _quarantineList = new ListView { Height = 330, Margin = new Thickness(0, 10, 0, 12),
            SelectionMode = System.Windows.Controls.SelectionMode.Extended,
            BorderBrush = Outline, BorderThickness = new Thickness(1), Background = Surface };
        var columns = new GridView();
        columns.Columns.Add(new GridViewColumn { Header = "Estado", Width = 90,
            DisplayMemberBinding = new Binding(nameof(QuarantineEntry.State)) });
        columns.Columns.Add(new GridViewColumn { Header = "Fecha", Width = 145,
            DisplayMemberBinding = new Binding(nameof(QuarantineEntry.QuarantinedAt))
            { StringFormat = "dd/MM/yyyy HH:mm" } });
        columns.Columns.Add(new GridViewColumn { Header = "Archivo original", Width = 400,
            DisplayMemberBinding = new Binding(nameof(QuarantineEntry.OriginalPath)) });
        columns.Columns.Add(new GridViewColumn { Header = "Firma ClamAV", Width = 235,
            DisplayMemberBinding = new Binding(nameof(QuarantineEntry.Detection)) });
        columns.Columns.Add(new GridViewColumn { Header = "Última revisión", Width = 255,
            DisplayMemberBinding = new Binding(nameof(QuarantineEntry.RecheckLabel)) });
        columns.Columns.Add(new GridViewColumn { Header = "Revisión manual", Width = 140,
            DisplayMemberBinding = new Binding(nameof(QuarantineEntry.ReviewLabel)) });
        _quarantineList.View = columns;
        _quarantineList.SelectionChanged += (_, _) => UpdateRemediationSelection();
        stack.Children.Add(_quarantineList);
        _quarantineReviewInfo = T("Selecciona un archivo aislado para consultar su integridad y revisiones.",
            11, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 9));
        _quarantineReviewInfo.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(_quarantineReviewInfo);
        var buttons = new WrapPanel();
        buttons.Children.Add(ActionButton("↻  Actualizar lista", false, (_, _) => RefreshQuarantine()));
        buttons.Children.Add(ActionButton("⌕  Reanalizar aislados (máx. 20)", false,
            async (_, _) => await RecheckQuarantineSelectedAsync()));
        buttons.Children.Add(ActionButton("⚑  Revisar posible falso positivo", false,
            async (_, _) => await FlagQuarantineForReviewAsync()));
        buttons.Children.Add(ActionButton("▤  Exportar informe local", false,
            (_, _) => ExportRemediationReport()));
        buttons.Children.Add(ActionButton("✦  Explicar cuarentena con IA", false,
            (_, _) => OpenAiAnalystWithPrompt(AiSecurityAnalyst.ForQuarantine(QuarantineStore.List()))));
        buttons.Children.Add(ActionButton("↶  Restaurar archivo seleccionado", false,
            async (_, _) => await RestoreQuarantineAsync()));
        var deleteButton = ActionButton("✕  Eliminar definitivamente seleccionado", false,
            async (_, _) => await DeleteQuarantineAsync());
        deleteButton.Background = B("#FDE8E8");
        deleteButton.Foreground = B("#AE1C30");
        deleteButton.BorderBrush = B("#F4B8C1");
        buttons.Children.Add(deleteButton);
        stack.Children.Add(buttons);
        stack.Children.Add(T("Nunca se aísla ni elimina automáticamente. La eliminación requiere confirmación, " +
            "afecta solo al archivo ya aislado y deja un registro. No garantiza borrado forense. " +
            "La restauración no sobrescribe archivos y comprueba SHA-256. Los reanálisis " +
            "usan firmas locales; no cambian el veredicto original ni restauran automáticamente.", 11, Muted,
            FontWeights.Normal, new Thickness(0, 12, 0, 0)));
        card.Child = stack;
        page.Children.Add(card);
    }

    private void RefreshQuarantine()
    {
        try
        {
            var entries = QuarantineStore.List();
            _quarantineList.ItemsSource = entries;
            _quarantineStatus.Text = $"{entries.Count(e => e.RestoredAt is null && e.DeletedAt is null)} aislados · " +
                $"{entries.Count(e => e.RestoredAt is not null)} restaurados · " +
                $"{entries.Count(e => e.DeletedAt is not null)} eliminados (histórico).";
        }
        catch (Exception ex) { _quarantineStatus.Text = "No se pudo consultar la cuarentena: " + ex.Message; }
    }

    private async Task IsolateDetectionAsync()
    {
        if (_busy || _remediationRunning) { MessageBox.Show(this, "Espera a que finalice la operación actual antes de mover archivos."); return; }
        if (_scanResultsList.SelectedItem is not ScanFileEvent { Status: ScanFileStatus.Detected } selected)
        {
            MessageBox.Show(this, "Selecciona una detección en la tabla de resultados del análisis.",
                "Cuarentena", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!File.Exists(selected.FilePath))
        {
            MessageBox.Show(this, "No se encuentra un archivo físico que pueda aislarse. " +
                "Los elementos contenidos dentro de un ZIP no pueden separarse de este modo.",
                "Cuarentena", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show(this, $"¿Aislar este archivo en tu perfil local?\n\n{selected.FilePath}\n\n" +
            "No se eliminará. Se moverá a una carpeta privada con nombre opaco.",
            "Confirmar cuarentena", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            var moved = await QuarantineStore.IsolateAsync(selected.FilePath, selected.Detail);
            AppendLog("[CUARENTENA] Archivo aislado: " + moved.OriginalPath);
            Navigate("quarantine");
        }
        catch (Exception ex)
        {
            // Si el fallo ocurrió después del traslado, el archivo puede estar
            // aislado pero marcado para revisión de integridad.
            RefreshQuarantine();
            MessageBox.Show(this, "El aislamiento no se pudo completar con garantías: " + ex.Message +
                "\n\nRevisa la sección Cuarentena antes de intentar otra acción.",
                "Cuarentena", MessageBoxButton.OK, MessageBoxImage.Error);
            Navigate("quarantine");
        }
    }

    private async Task RestoreQuarantineAsync()
    {
        if (_busy || _remediationRunning) { MessageBox.Show(this, "Finaliza la operación actual antes de restaurar archivos."); return; }
        if (_quarantineList.SelectedItems.Count != 1 ||
            _quarantineList.SelectedItem is not QuarantineEntry selected || selected.RestoredAt is not null || selected.DeletedAt is not null)
        {
            MessageBox.Show(this, "Selecciona un elemento que siga aislado.", "Cuarentena"); return;
        }
        if (MessageBox.Show(this, "ADVERTENCIA: este archivo fue señalado por ClamAV. " +
            "Restaurarlo puede volver a exponer una amenaza.\n\n" + selected.OriginalPath +
            "\n\n¿Confirmas su restauración?", "Restaurar archivo sospechoso",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            await QuarantineStore.RestoreAsync(selected);
            RefreshQuarantine();
            MessageBox.Show(this, "Archivo restaurado. Revisa por qué fue detectado antes de utilizarlo.",
                "Cuarentena", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show(this, "No se restauró: " + ex.Message,
            "Cuarentena", MessageBoxButton.OK, MessageBoxImage.Error); RefreshQuarantine(); }
    }

    private bool _remediationRunning;

    private async Task DeleteQuarantineAsync()
    {
        if (_busy || _remediationRunning)
        {
            MessageBox.Show(this, "Espera a que termine la operación actual.", "PalClam Remediation");
            return;
        }
        if (_quarantineList.SelectedItems.Count != 1 ||
            _quarantineList.SelectedItem is not QuarantineEntry selected ||
            selected.RestoredAt is not null || selected.DeletedAt is not null)
        {
            MessageBox.Show(this, "Selecciona un elemento que continúe aislado.",
                "PalClam Remediation", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show(this,
            "ELIMINACIÓN DEFINITIVA DEL ARCHIVO AISLADO\n\n" + selected.OriginalPath +
            "\n\nFirma detectada: " + selected.Detection +
            "\n\nNo podrás recuperarlo desde PalClam. Esta acción NO elimina otros componentes " +
            "de una posible infección ni garantiza borrado forense del disco.\n\n¿Confirmas la eliminación?",
            "Confirmar eliminación definitiva", MessageBoxButton.YesNo,
            MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
        {
            AppendLog("[REMEDIACION] Eliminación cancelada por el usuario.");
            return;
        }
        _remediationRunning = true;
        try
        {
            await QuarantineStore.DeleteIsolatedAsync(selected);
            AppendLog("[REMEDIACION] Eliminación confirmada desde cuarentena: " + selected.OriginalPath);
            RefreshQuarantine();
            MessageBox.Show(this, "Archivo aislado eliminado. Se conserva el registro de la operación.",
                "PalClam Remediation", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppendLog("[REMEDIACION] Error al eliminar: " + ex.Message);
            RefreshQuarantine();
            MessageBox.Show(this, "No se ha podido confirmar la eliminación: " + ex.Message,
                "PalClam Remediation", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { _remediationRunning = false; }
    }

    private async Task IsolateAndDeleteDetectionAsync()
    {
        if (_busy || _remediationRunning)
        {
            MessageBox.Show(this, "Termina el análisis antes de gestionar la detección.");
            return;
        }
        if (_scanResultsList.SelectedItem is not ScanFileEvent { Status: ScanFileStatus.Detected } found ||
            !File.Exists(found.FilePath))
        {
            MessageBox.Show(this, "Selecciona un archivo físico detectado por ClamAV.",
                "PalClam Remediation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show(this,
            "PRIMERA CONFIRMACIÓN: AISLAMIENTO\n\n" + found.FilePath +
            "\n\nPalClam moverá el archivo a cuarentena. Aún no lo eliminará. ¿Continuar?",
            "Confirmar cuarentena", MessageBoxButton.YesNo,
            MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        _remediationRunning = true;
        try
        {
            var entry = await QuarantineStore.IsolateAsync(found.FilePath, found.Detail);
            AppendLog("[REMEDIACION] Archivo aislado con permiso: " + entry.OriginalPath);
            Navigate("quarantine");
            if (MessageBox.Show(this,
                "SEGUNDA CONFIRMACIÓN: ELIMINACIÓN\n\n" + entry.OriginalPath +
                "\n\nYa está en cuarentena. Si eliges NO, seguirá aislado y podrás restaurarlo. " +
                "Si eliges SÍ, se eliminará el archivo aislado y quedará un registro.\n\n¿Eliminar ahora?",
                "Confirmar eliminación definitiva", MessageBoxButton.YesNo,
                MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                AppendLog("[REMEDIACION] Conservado en cuarentena por decisión del usuario.");
                return;
            }
            await QuarantineStore.DeleteIsolatedAsync(entry);
            AppendLog("[REMEDIACION] Archivo eliminado tras doble confirmación: " + entry.OriginalPath);
            RefreshQuarantine();
        }
        catch (Exception ex)
        {
            RefreshQuarantine();
            MessageBox.Show(this, "La operación no se completó con garantías: " + ex.Message +
                "\n\nComprueba el estado de la cuarentena.",
                "PalClam Remediation", MessageBoxButton.OK, MessageBoxImage.Error);
            Navigate("quarantine");
        }
        finally { _remediationRunning = false; }
    }

    private void BuildSchedulingPage(StackPanel page)
    {
        page.Children.Add(T("Análisis programados", 23, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 7)));
        page.Children.Add(T("Crea una tarea de Windows que analiza sin abrir ventanas. " +
            "Funciona cuando este usuario haya iniciado sesión; si el ordenador está apagado o sin sesión, " +
            "la ejecución puede no realizarse. Nunca se eliminan archivos automáticamente.",
            12, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 15)));
        var card = Card(Surface, Outline);
        var stack = new StackPanel();
        stack.Children.Add(T("Frecuencia", 13, Ink, FontWeights.SemiBold));
        _scheduleFrequency = new ComboBox { Height = 33, Width = 240,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left, Margin = new Thickness(0, 7, 0, 12),
            ItemsSource = new[] { "Diaria", "Semanal (lunes)" }, SelectedIndex = 0 };
        stack.Children.Add(_scheduleFrequency);
        stack.Children.Add(T("Objetivo", 13, Ink, FontWeights.SemiBold));
        _scheduleTarget = new ComboBox { Height = 33, Width = 240,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left, Margin = new Thickness(0, 7, 0, 12),
            ItemsSource = new[] { "Descargas", "Perfil de usuario" },
            SelectedIndex = _settings.ScheduledTarget == "Perfil de usuario" ? 1 : 0 };
        stack.Children.Add(_scheduleTarget);
        stack.Children.Add(T("Hora (HH:mm)", 13, Ink, FontWeights.SemiBold));
        _scheduleTime = Input("Por ejemplo: 19:00");
        _scheduleTime.Width = 140;
        _scheduleTime.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
        _scheduleTime.Margin = new Thickness(0, 7, 0, 14);
        _scheduleTime.Text = "19:00";
        stack.Children.Add(_scheduleTime);
        var actions = new WrapPanel();
        actions.Children.Add(ActionButton("Guardar programación", true,
            async (_, _) => await ConfigureScheduleAsync()));
        actions.Children.Add(ActionButton("Eliminar programación", false,
            async (_, _) => await DeleteScheduleAsync()));
        actions.Children.Add(ActionButton("Comprobar estado", false,
            async (_, _) => await RefreshScheduleAsync()));
        stack.Children.Add(actions);
        _scheduleStatus = T("Comprobando tarea de Windows...", 12, Muted,
            FontWeights.Normal, new Thickness(0, 12, 0, 0));
        stack.Children.Add(_scheduleStatus);
        stack.Children.Add(T("Los análisis automáticos se registran en Historial local, identificados " +
            "como [PROGRAMADO]. Si faltan firmas o el motor falla, se registrará como incompleto. " +
            "No hay monitorización en tiempo real.", 11, Muted,
            FontWeights.Normal, new Thickness(0, 13, 0, 0)));
        card.Child = stack;
        page.Children.Add(card);
    }

    private async Task ConfigureScheduleAsync()
    {
        _settings.ScheduledTarget = _scheduleTarget.SelectedIndex == 1 ? "Perfil de usuario" : "Descargas";
        SaveSettings();
        try
        {
            var (success, detail) = await ScheduledTasksService.CreateAsync(
                _scheduleFrequency.SelectedIndex == 1, _scheduleTime.Text.Trim());
            _scheduleStatus.Text = success ? "Tarea creada: " + detail : "No se pudo programar: " + detail;
        }
        catch (Exception ex) { _scheduleStatus.Text = "Error de programación: " + ex.Message; }
    }

    private async Task DeleteScheduleAsync()
    {
        if (MessageBox.Show(this, "¿Eliminar la programación de análisis de este usuario?",
            "Análisis programados", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            var (success, detail) = await ScheduledTasksService.DeleteAsync();
            _scheduleStatus.Text = success ? "Tarea eliminada." : "No se eliminó: " + detail;
        }
        catch (Exception ex) { _scheduleStatus.Text = "Error: " + ex.Message; }
    }

    private async Task RefreshScheduleAsync()
    {
        if (_scheduleStatus is null) return;
        try
        {
            var (success, detail) = await ScheduledTasksService.StatusAsync();
            _scheduleStatus.Text = success ? "Programación detectada en Windows. " + detail :
                "No hay tarea activa de PalClam para esta cuenta (o no se puede consultar).";
        }
        catch (Exception ex) { _scheduleStatus.Text = "No se pudo consultar: " + ex.Message; }
    }

    private void BuildOptionsPage(StackPanel page)
    {
        page.Children.Add(LT("Options.Title", 24, Ink, FontWeights.Bold, new Thickness(0, 0, 0, 6)));
        page.Children.Add(LT("Options.Introduction", 12, Muted, FontWeights.Normal,
            new Thickness(0, 0, 0, 16)));

        // Selector de temas reales y accesibles. Cambia en vivo y persiste en settings.json.
        var visual = Card(Surface, Outline);
        var visualStack = new StackPanel();
        visualStack.Children.Add(LT("Options.AppearanceTitle", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 7)));
        visualStack.Children.Add(BuildLanguageSelector());
        visualStack.Children.Add(T("Elige entre siete temas de alto contraste, incluido Oscuro violeta. El rojo de las amenazas " +
            "y el ámbar de advertencia mantienen su significado. Los cambios se aplican al instante.",
            11, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 12)));
        var themeLine = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        themeLine.Children.Add(T("Tema de colores", 12, Ink, FontWeights.SemiBold,
            new Thickness(0, 7, 14, 0)));
        _themeOption = new ComboBox { Width = 218, MinHeight = 34,
            ItemsSource = AppearanceCatalog.Themes, DisplayMemberPath = "Name",
            SelectedItem = AppearanceCatalog.Find(_settings.UiTheme) };
        _themeOption.SelectionChanged += (_, _) => OnAppearanceChanged();
        themeLine.Children.Add(_themeOption);
        visualStack.Children.Add(themeLine);
        var paletteSwatches = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        foreach (var palette in AppearanceCatalog.Themes)
        {
            var swatch = new Button
            {
                Width = 29, Height = 29, Margin = new Thickness(0, 0, 8, 0),
                Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(palette.Accent)!),
                BorderBrush = Outline, BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand, ToolTip = palette.Name
            };
            RoundButton(swatch, 8);
            swatch.Click += (_, _) => _themeOption.SelectedItem = palette;
            paletteSwatches.Children.Add(swatch);
        }
        visualStack.Children.Add(paletteSwatches);
        var textLine = new WrapPanel { Margin = new Thickness(0, 0, 0, 9) };
        textLine.Children.Add(T("Tamaño del texto", 12, Ink, FontWeights.SemiBold,
            new Thickness(0, 7, 14, 0)));
        _textScaleOption = new ComboBox { Width = 194, MinHeight = 34,
            ItemsSource = AppearanceCatalog.SupportedScales,
            SelectedItem = AppearanceCatalog.NormalizeScaleValue(_settings.UiTextScale) };
        // ItemsSource usa escala numérica; aquí se muestran porcentajes.
        _textScaleOption.ItemStringFormat = "{0:P0}";
        _textScaleOption.SelectionChanged += (_, _) => OnAppearanceChanged();
        textLine.Children.Add(_textScaleOption);
        visualStack.Children.Add(textLine);
        visualStack.Children.Add(T("90 %, 100 %, 110 %, 120 % y 130 %. El texto crece sin " +
            "cambiar el tamaño de los iconos; los paneles siguen siendo desplazables.",
            11, Muted, FontWeights.Normal));
        _auroraOption = OptionCheck("Aurora animada y suave detrás del escudo y el título",
            _settings.AnimateHeaderAurora,
            "Las ondas siguen el color del tema. Se detienen al minimizar u ocultar PalClam.",
            () =>
            {
                if (_auroraOption is null) return;
                _settings.AnimateHeaderAurora = _auroraOption.IsChecked == true;
                RefreshHeaderAuroraMotion();
                SaveSettings();
            });
        visualStack.Children.Add(_auroraOption);
        var auroraSpeedRow = new WrapPanel { Margin = new Thickness(0, 8, 0, 7) };
        auroraSpeedRow.Children.Add(T("Velocidad de la aurora", 12, Ink, FontWeights.SemiBold,
            new Thickness(0, 8, 14, 0)));
        _auroraSpeedOption = new ComboBox
        {
            Width = 258, MinHeight = 34,
            ItemsSource = AuroraMotionProfiles.All,
            DisplayMemberPath = nameof(AuroraMotionProfile.Name),
            SelectedItem = AuroraMotionProfiles.Find(_settings.HeaderAuroraSpeed),
            ToolTip = "Ajusta las ondas de luz de la cabecera. No cambia la velocidad del antivirus."
        };
        _auroraSpeedOption.SelectionChanged += (_, _) =>
        {
            if (_auroraSpeedOption?.SelectedItem is not AuroraMotionProfile selected) return;
            if (_settings.HeaderAuroraSpeed == selected.Id) return;
            _settings.HeaderAuroraSpeed = selected.Id;
            RefreshHeaderAuroraMotion(forceRestart: true);
            SaveSettings();
            if (_optionsStatus is not null)
                _optionsStatus.Text = "Aurora: velocidad " + selected.Name + ". Guardada.";
        };
        auroraSpeedRow.Children.Add(_auroraSpeedOption);
        visualStack.Children.Add(auroraSpeedRow);
        visualStack.Children.Add(T("La opción Equilibrada es más rápida que la animación anterior. " +
            "Puedes reducir la velocidad, aumentarla o desactivar por completo la aurora. " +
            "La animación se detiene al minimizar y en el alto contraste de Windows.",
            11, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 3)));
        visual.Child = visualStack;
        page.Children.Add(visual);

        var scan = Card(Surface, Outline);
        scan.Margin = new Thickness(0, 13, 0, 0);
        var scanStack = new StackPanel();
        scanStack.Children.Add(T("Análisis antivirus", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 10)));
        _scanArchivesOption = OptionCheck("Examinar el contenido de ZIP y otros archivos comprimidos",
            _settings.ScanArchives, "Habilita el análisis interno de los formatos que soporte ClamAV.", SaveGeneralOptions);
        scanStack.Children.Add(_scanArchivesOption);
        _detectPuaOption = OptionCheck("Detectar aplicaciones potencialmente no deseadas (PUA)",
            _settings.DetectPua, "Puede producir avisos adicionales y falsos positivos; desactivado por defecto.", SaveGeneralOptions);
        scanStack.Children.Add(_detectPuaOption);
        scanStack.Children.Add(T("Los límites internos de tamaño, profundidad y tiempo se mantienen " +
            "en los valores seguros de ClamAV.", 11, Muted, FontWeights.Normal,
            new Thickness(0, 2, 0, 2)));
        scan.Child = scanStack;
        page.Children.Add(scan);

        var performanceOptions = Card(Surface, Outline);
        performanceOptions.Margin = new Thickness(0, 13, 0, 0);
        var performanceStack = new StackPanel();
        performanceStack.Children.Add(T("Scan Engine PRO · rendimiento", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 9)));
        _performanceOption = OptionCheck("Mostrar CPU, RAM, ritmo de archivos y actividad del motor",
            _settings.ShowScanPerformance,
            "Telemetría local del proceso clamscan. No mide la eficacia de detección ni el tráfico físico de disco.",
            SavePerformanceOptions);
        performanceStack.Children.Add(_performanceOption);
        var priorityRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        priorityRow.Children.Add(T("Prioridad del análisis", 12, Ink, FontWeights.SemiBold,
            new Thickness(0, 7, 14, 0)));
        _scanPriorityOption = new ComboBox { Width = 180, Height = 32,
            ItemsSource = new[] { "Económico", "Equilibrado", "Prioritario" },
            SelectedItem = _settings.ScanPriority };
        _scanPriorityOption.SelectionChanged += (_, _) => SavePerformanceOptions();
        priorityRow.Children.Add(_scanPriorityOption);
        performanceStack.Children.Add(priorityRow);
        performanceStack.Children.Add(T("Económico: menor prioridad de CPU. Equilibrado: normal (recomendado). " +
            "Prioritario: superior a la normal; puede afectar otras aplicaciones. " +
            "No inicia múltiples copias de ClamAV: la concurrencia real se evaluará con clamd.",
            11, Muted, FontWeights.Normal));
        performanceOptions.Child = performanceStack;
        page.Children.Add(performanceOptions);
        AddScanControlPreferences(page);

        var updates = Card(Surface, Outline);
        updates.Margin = new Thickness(0, 13, 0, 0);
        var updateStack = new StackPanel();
        updateStack.Children.Add(T("Actualizaciones", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 10)));
        _autoEngine = OptionCheck("Comprobar y mantener el motor ClamAV actualizado",
            _settings.AutoUpdateEngine, "El motor offline sigue disponible como respaldo.", SaveUpdatePreferences);
        updateStack.Children.Add(_autoEngine);
        _updateSignaturesAutomatically = OptionCheck("Actualizar las firmas automáticamente",
            _settings.AutoUpdateSignatures, "FreshClam descargará las bases de detección cuando haya conexión.", SaveUpdatePreferences);
        updateStack.Children.Add(_updateSignaturesAutomatically);
        _checkUpdatesOnStartup = OptionCheck("Buscar versiones nuevas de PalClam al iniciar",
            _settings.CheckAppUpdatesOnStartup, "Consulta tu canal de publicación firmado.", SaveUpdatePreferences);
        updateStack.Children.Add(_checkUpdatesOnStartup);
        _installUpdatesAutomatically = OptionCheck("Instalar versiones nuevas de PalClam sin intervención",
            _settings.AutoInstallAppUpdates, "Puede cerrar y reiniciar la aplicación. Por defecto pide intervención.", SaveUpdatePreferences);
        updateStack.Children.Add(_installUpdatesAutomatically);
        var intervalRow = new WrapPanel { Margin = new Thickness(0, 9, 0, 0) };
        intervalRow.Children.Add(T("Intervalo de comprobación de motor y firmas", 12, Ink,
            FontWeights.SemiBold, new Thickness(0, 6, 14, 0)));
        _checkIntervalOption = new ComboBox { Width = 132, Height = 31,
            ItemsSource = new[] { "6 horas", "12 horas", "24 horas" },
            SelectedIndex = _settings.UpdateIntervalHours == 6 ? 0 :
                _settings.UpdateIntervalHours == 24 ? 2 : 1 };
        _checkIntervalOption.SelectionChanged += (_, _) => SaveUpdatePreferences();
        intervalRow.Children.Add(_checkIntervalOption);
        updateStack.Children.Add(intervalRow);
        updates.Child = updateStack;
        page.Children.Add(updates);

        var display = Card(Surface, Outline);
        display.Margin = new Thickness(0, 13, 0, 0);
        var displayStack = new StackPanel();
        displayStack.Children.Add(T("Resultados y visualización", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 10)));
        _showCleanOption = OptionCheck("Mostrar también los archivos sin detecciones en la tabla",
            _settings.ShowCleanResults, "El contador seguirá incluyendo todos los archivos notificados por ClamAV.", SaveGeneralOptions);
        displayStack.Children.Add(_showCleanOption);
        _consoleExpandedOption = OptionCheck("Mantener la consola técnica desplegada",
            _settings.ShowTechnicalConsole, "Los informes completos se guardan en tu equipo en cualquier caso.", SaveGeneralOptions);
        displayStack.Children.Add(_consoleExpandedOption);
        _notificationsOption = OptionCheck("Avisar en Windows al terminar un análisis abierto en PalClam",
            _settings.NotifyOnScanEnd,
            "Se muestra un aviso de bandeja de Windows; depende de que las notificaciones del sistema estén habilitadas.",
            SaveGeneralOptions);
        displayStack.Children.Add(_notificationsOption);
        _keepTrayOption = OptionCheck("Mantener PalClam en la bandeja al cerrar la ventana",
            _settings.MinimizeToTrayOnClose,
            "El programa y Guard continuarán mientras PalClam siga abierto en segundo plano. Usa Salir en el menú del escudo para detenerlo.",
            () => { _settings.MinimizeToTrayOnClose = _keepTrayOption?.IsChecked == true; SaveSettings(); });
        displayStack.Children.Add(_keepTrayOption);
        displayStack.Children.Add(T("Inicio automático de PalClam", 13, Ink, FontWeights.SemiBold,
            new Thickness(0, 8, 0, 7)));
        _startWithWindowsOption = new ComboBox
        {
            ItemsSource = new[]
            {
                "Desactivado",
                "Solo mi usuario · bandeja",
                "Todos los usuarios · requiere administrador"
            },
            SelectedIndex = (int)WindowsStartupRegistration.GetScope(),
            MinWidth = 280, MaxWidth = 440, Height = 34,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 7)
        };
        _startWithWindowsOption.SelectionChanged += (_, _) => SaveWindowsStartupPreference();
        displayStack.Children.Add(_startWithWindowsOption);
        var startupExplanation = T("Cada sesión abre PalClam oculto en su propia bandeja. " +
            "Para todos los usuarios se necesita instalar la aplicación en Program Files y autorizar UAC. " +
            "Guard y la configuración siguen siendo independientes en cada cuenta; no es un servicio del sistema.",
            11, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 7));
        startupExplanation.TextWrapping = TextWrapping.Wrap;
        displayStack.Children.Add(startupExplanation);
        _windowsStartupStatus = T(WindowsStartupRegistration.Status(), 11, Muted,
            FontWeights.Normal, new Thickness(0, 1, 0, 10));
        _windowsStartupStatus.TextWrapping = TextWrapping.Wrap;
        displayStack.Children.Add(_windowsStartupStatus);
        display.Child = displayStack;
        page.Children.Add(display);

        var exclusions = Card(Surface, Outline);
        exclusions.Margin = new Thickness(0, 13, 0, 0);
        var exclusionsStack = new StackPanel();
        exclusionsStack.Children.Add(T("Exclusiones de análisis", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 7)));
        exclusionsStack.Children.Add(T("Una ruta absoluta de archivo o carpeta por línea. Los elementos " +
            "excluidos NO serán analizados, aunque estén infectados. Úsalo solo si es imprescindible.",
            12, B("#966023"), FontWeights.Normal, new Thickness(0, 0, 0, 10)));
        _excludedPathsBox = new TextBox
        {
            AcceptsReturn = true, TextWrapping = TextWrapping.NoWrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            MinHeight = 95, MaxHeight = 150, FontSize = 12,
            FontFamily = new FontFamily("Consolas"), Foreground = Ink,
            BorderBrush = Outline, BorderThickness = new Thickness(1), Padding = new Thickness(9),
            Text = string.Join(Environment.NewLine, _settings.ExcludedPaths)
        };
        exclusionsStack.Children.Add(_excludedPathsBox);
        var exclusionButtons = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
        exclusionButtons.Children.Add(ActionButton("+  Añadir carpeta", false, (_, _) =>
        {
            var picker = new OpenFolderDialog { Title = "Carpeta que NO se analizará" };
            if (PalClam.UI.LocalizedDialogs.Show(picker, this) == true) AddExcludedPath(picker.FolderName);
        }));
        exclusionButtons.Children.Add(ActionButton("+  Añadir archivo", false, (_, _) =>
        {
            var picker = new OpenFileDialog { Title = "Archivo que NO se analizará" };
            if (PalClam.UI.LocalizedDialogs.Show(picker, this) == true) AddExcludedPath(picker.FileName);
        }));
        exclusionButtons.Children.Add(ActionButton("Guardar exclusiones", true, (_, _) => SaveExcludedPaths()));
        exclusionsStack.Children.Add(exclusionButtons);
        exclusions.Child = exclusionsStack;
        page.Children.Add(exclusions);

        var guardSettingsLink = Card(PurplePale, PurplePale);
        guardSettingsLink.Margin = new Thickness(0, 13, 0, 0);
        var guardSettingsStack = new StackPanel();
        guardSettingsStack.Children.Add(T("Vigilancia opcional de archivos", 15, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 7)));
        guardSettingsStack.Children.Add(T("Configura las carpetas de PalClam Guard, el arranque automático, " +
            "las notificaciones y el tamaño máximo de archivo. Solo funciona con PalClam abierto.",
            12, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 8)));
        guardSettingsStack.Children.Add(ActionButton("◉  Abrir PalClam Guard", false, (_, _) => Navigate("guard")));
        guardSettingsStack.Children.Add(ActionButton("♥  Security Health 360", false,
            (_, _) => Navigate("health")));
        guardSettingsStack.Children.Add(ActionButton("▣  USB Watch y Descargas", false,
            (_, _) => Navigate("devices")));
        guardSettingsLink.Child = guardSettingsStack;
        page.Children.Add(guardSettingsLink);

        var mailOptions = Card(PurplePale, PurplePale);
        mailOptions.Margin = new Thickness(0, 13, 0, 0);
        var mailOptionsContent = new StackPanel();
        mailOptionsContent.Children.Add(T("Opciones de análisis de correo", 15, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 7)));
        mailOptionsContent.Children.Add(T("Configura adjuntos, límites, subcarpetas y avisos de enlaces sospechosos. " +
            "No accede a los buzones de Outlook ni a cuentas online.", 12, Muted, FontWeights.Normal,
            new Thickness(0, 0, 0, 9)));
        mailOptionsContent.Children.Add(ActionButton("✉  Abrir PalClam Mail y sus opciones", false, (_, _) => Navigate("mail")));
        mailOptions.Child = mailOptionsContent;
        page.Children.Add(mailOptions);

        var advanced = Card(Surface, Outline);
        advanced.Margin = new Thickness(0, 13, 0, 0);
        var advancedStack = new StackPanel();
        advancedStack.Children.Add(T("Privacidad y seguridad", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 9)));
        advancedStack.Children.Add(T("• Ningún archivo se elimina, mueve ni pone en cuarentena automáticamente.\n" +
            "• Las rutas, registros y preferencias permanecen en este equipo.\n" +
            "• PalClam no desactiva Microsoft Defender ni proporciona protección residente.",
            12, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 11)));
        var localFolderButton = ActionButton("Abrir carpeta de datos", false, (_, _) =>
        {
            var folder = System.IO.Path.Combine(PalClam.Services.AppPaths.Root);
            try { Directory.CreateDirectory(folder); Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Abrir carpeta", MessageBoxButton.OK, MessageBoxImage.Warning); }
        });
        advancedStack.Children.Add(localFolderButton);
        advancedStack.Children.Add(T("Integración con el Explorador de Windows", 15, Ink,
            FontWeights.Bold, new Thickness(0, 16, 0, 8)));
        _explorerOption = OptionCheck("Mostrar 'Analizar con PalClam' al hacer clic derecho",
            ExplorerIntegration.IsEnabled(),
            "Se registra solo para este usuario. No requiere administrador; en Windows 11 puede aparecer bajo 'Mostrar más opciones'.",
            SaveExplorerIntegration);
        advancedStack.Children.Add(_explorerOption);
        _explorerIntegrationText = T(ExplorerIntegration.Status(), 11, Muted, FontWeights.Normal,
            new Thickness(0, 7, 0, 0));
        _explorerIntegrationText.TextWrapping = TextWrapping.Wrap;
        advancedStack.Children.Add(_explorerIntegrationText);
        advancedStack.Children.Add(ActionButton("Verificar menú contextual", false, (_, _) =>
        {
            _explorerIntegrationText.Text = ExplorerIntegration.Status();
            _settingExplorerOption = true;
            try { _explorerOption.IsChecked = ExplorerIntegration.IsEnabled(); }
            finally { _settingExplorerOption = false; }
        }));
        advancedStack.Children.Add(ActionButton("Restablecer preferencias", false, (_, _) => RestoreDefaultOptions()));
        advanced.Child = advancedStack;
        page.Children.Add(advanced);
        _optionsStatus = T("Preferencias cargadas.", 11, Muted, FontWeights.Normal,
            new Thickness(0, 10, 0, 0));
        page.Children.Add(_optionsStatus);
        OrganizeOptionsIntoTabs(page);
    }

    private static CheckBox OptionCheck(string title, bool initial, string explanation, Action onChange)
    {
        // Evitar recortes en Opciones con texto al 130 % o ventana compacta.
        var titleText = T(title, 12.5, Ink, FontWeights.SemiBold);
        titleText.TextWrapping = TextWrapping.Wrap;
        var explanationText = T(explanation, 11, Muted, FontWeights.Normal,
            new Thickness(0, 2, 0, 0));
        explanationText.TextWrapping = TextWrapping.Wrap;
        var checkbox = new CheckBox
        {
            Content = new StackPanel { Children = { titleText, explanationText } },
            IsChecked = initial, Foreground = Ink, Margin = new Thickness(0, 1, 0, 11),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        checkbox.Checked += (_, _) => onChange();
        checkbox.Unchecked += (_, _) => onChange();
        return checkbox;
    }

    private void SavePerformanceOptions()
    {
        if (_performanceOption is null || _scanPriorityOption is null) return;
        _settings.ShowScanPerformance = _performanceOption.IsChecked == true;
        _settings.ScanPriority = _scanPriorityOption.SelectedItem as string ?? "Equilibrado";
        if (_smartScanPriority is not null && !Equals(_smartScanPriority.SelectedItem, _settings.ScanPriority))
            _smartScanPriority.SelectedItem = _settings.ScanPriority;
        SaveSettings();
        if (_optionsStatus is not null)
            _optionsStatus.Text = "Preferencias de rendimiento guardadas para el próximo análisis.";
    }

    private void SaveGeneralOptions()
    {
        if (_scanArchivesOption is null || _showCleanOption is null || _consoleExpandedOption is null ||
            _detectPuaOption is null) return;
        _settings.ScanArchives = _scanArchivesOption.IsChecked == true;
        _settings.DetectPua = _detectPuaOption.IsChecked == true;
        _settings.ShowCleanResults = _showCleanOption.IsChecked == true;
        _settings.ShowTechnicalConsole = _consoleExpandedOption.IsChecked == true;
        _settings.NotifyOnScanEnd = _notificationsOption?.IsChecked == true;
        if (_technicalConsoleExpander is not null)
            _technicalConsoleExpander.IsExpanded = _settings.ShowTechnicalConsole;
        RefreshResultsView();
        SaveSettings();
        if (_optionsStatus is not null) _optionsStatus.Text = "Preferencias de análisis guardadas.";
    }

    private void AddExcludedPath(string path)
    {
        if (_excludedPathsBox is null) return;
        if (!string.IsNullOrWhiteSpace(_excludedPathsBox.Text) &&
            !_excludedPathsBox.Text.EndsWith(Environment.NewLine, StringComparison.Ordinal))
            _excludedPathsBox.AppendText(Environment.NewLine);
        _excludedPathsBox.AppendText(path);
    }

    private void SaveExcludedPaths()
    {
        if (_busy)
        {
            MessageBox.Show(this, "Espera a que finalice el análisis para cambiar las exclusiones.",
                "Opciones", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var lines = _excludedPathsBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (!ScanExclusions.TryNormalize(lines, out var paths, out var error))
        {
            MessageBox.Show(this, error, "Ruta de exclusión no válida", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _settings.ExcludedPaths = paths;
        _excludedPathsBox.Text = string.Join(Environment.NewLine, paths);
        SaveSettings();
        _optionsStatus.Text = paths.Count == 0 ? "Exclusiones eliminadas." :
            $"Guardadas {paths.Count} exclusiones. Estas rutas no se analizarán.";
    }

    private void RestoreDefaultOptions()
    {
        if (_busy) { MessageBox.Show(this, "No se pueden restablecer opciones durante el análisis.",
            "Opciones", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (MessageBox.Show(this, "¿Restablecer preferencias de análisis, actualizaciones, interfaz y exclusiones? " +
            "No se borrarán el historial ni las firmas.", "Restablecer preferencias",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        var defaults = new AppSettings();
        var priorTheme = ActiveTheme;
        var priorScale = _settings.UiTextScale;
        // Cambiar los dos selectores de forma atómica. Sus SelectionChanged no deben
        // ejecutar dos repintados ni guardar ajustes intermedios por separado.
        _applyingAppearanceChange = true;
        try
        {
            _settings.UiTheme = defaults.UiTheme;
            _settings.UiTextScale = defaults.UiTextScale;
            if (_themeOption is not null)
                _themeOption.SelectedItem = AppearanceCatalog.Find(defaults.UiTheme);
            if (_textScaleOption is not null)
                _textScaleOption.SelectedItem = defaults.UiTextScale;
            ApplyAppearancePalette(defaults.UiTheme);
            ApplyAppearanceTextScale();
        }
        catch (Exception ex)
        {
            CrashDiagnostics.Record("restablecer-apariencia", ex);
            _settings.UiTheme = priorTheme;
            _settings.UiTextScale = priorScale;
            try
            {
                if (ActiveTheme != priorTheme) ApplyAppearancePalette(priorTheme);
                if (_themeOption is not null)
                    _themeOption.SelectedItem = AppearanceCatalog.Find(priorTheme);
                if (_textScaleOption is not null)
                    _textScaleOption.SelectedItem = priorScale;
                ApplyAppearanceTextScale();
            }
            catch (Exception rollbackError)
            {
                CrashDiagnostics.Record("reversion-restablecer-apariencia", rollbackError);
            }
            MessageBox.Show(this,
                "No se han podido restablecer los ajustes de apariencia. Consulta " + CrashDiagnostics.LogPath,
                "PalClam · Apariencia", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        finally { _applyingAppearanceChange = false; }
        _settings.ScanArchives = defaults.ScanArchives;
        _settings.ShowScanPerformance = defaults.ShowScanPerformance;
        _settings.ScanPriority = defaults.ScanPriority;
        _settings.ScanControlMode = defaults.ScanControlMode;
        _settings.ShowFloatingScanBar = defaults.ShowFloatingScanBar;
        _settings.FloatingScanTopmost = defaults.FloatingScanTopmost;
        _settings.FloatingScanExpanded = defaults.FloatingScanExpanded;
        _floatingScanWindow?.Hide();
        if (_floatingModeOption is not null) _floatingModeOption.SelectedItem =
            defaults.FloatingScanExpanded ? "Detallada" : "Compacta";
        if (_scanModeOption is not null) _scanModeOption.SelectedItem = defaults.ScanControlMode;
        if (_floatingScanOption is not null) _floatingScanOption.IsChecked = defaults.ShowFloatingScanBar;
        if (_floatingTopmostOption is not null) _floatingTopmostOption.IsChecked = defaults.FloatingScanTopmost;
        if (_performanceOption is not null) _performanceOption.IsChecked = defaults.ShowScanPerformance;
        if (_scanPriorityOption is not null) _scanPriorityOption.SelectedItem = defaults.ScanPriority;
        _settings.DetectPua = defaults.DetectPua;
        _settings.ShowCleanResults = defaults.ShowCleanResults;
        _settings.ShowTechnicalConsole = defaults.ShowTechnicalConsole;
        _settings.NotifyOnScanEnd = defaults.NotifyOnScanEnd;
        _settings.MinimizeToTrayOnClose = defaults.MinimizeToTrayOnClose;
        _settings.AnimateHeaderAurora = defaults.AnimateHeaderAurora;
        _settings.HeaderAuroraSpeed = defaults.HeaderAuroraSpeed;
        if (_auroraOption is not null) _auroraOption.IsChecked = defaults.AnimateHeaderAurora;
        if (_auroraSpeedOption is not null)
            _auroraSpeedOption.SelectedItem = AuroraMotionProfiles.Find(defaults.HeaderAuroraSpeed);
        RefreshHeaderAuroraMotion(forceRestart: true);
        if (!AppPaths.IsPortable)
        {
        // El restablecimiento puede necesitar UAC si estaba habilitado en HKLM.
        // No ocultar un fallo administrativo: conservar el estado real.
        if (WindowsStartupRegistration.TrySet(StartupScope.Disabled, out var startupResetError))
        {
            _settings.LaunchAtWindowsStartup = false;
        }
        else
            AppendLog("[AVISO] No se pudo retirar el inicio automático: " + startupResetError);
        }
        RefreshWindowsStartupPreference();
        if (_keepTrayOption is not null) _keepTrayOption.IsChecked = defaults.MinimizeToTrayOnClose;
        _settings.GuardStartWithApp = defaults.GuardStartWithApp;
        _settings.GuardNotifyThreats = defaults.GuardNotifyThreats;
        _settings.GuardMaxFileMb = defaults.GuardMaxFileMb;
        _settings.GuardProcessingMode = defaults.GuardProcessingMode;
        _settings.GuardFolders = new();
        _settings.DownloadGuardEnabled = defaults.DownloadGuardEnabled;
        _settings.UsbWatchEnabled = defaults.UsbWatchEnabled;
        _settings.UsbAutoScanEnabled = defaults.UsbAutoScanEnabled;
        if (_downloadsWatchOption is not null) _downloadsWatchOption.IsChecked = false;
        if (_usbWatchOption is not null) _usbWatchOption.IsChecked = false;
        if (_usbAutoScanOption is not null) _usbAutoScanOption.IsChecked = false;
        UpdateRemovableTimer();
        if (_guardAutoStartOption is not null) _guardAutoStartOption.IsChecked = false;
        if (_guardNotifyOption is not null) _guardNotifyOption.IsChecked = true;
        if (_guardMaxSizeOption is not null) _guardMaxSizeOption.SelectedItem = "100 MB";
        if (_guardProcessingModeOption is not null)
            _guardProcessingModeOption.SelectedItem = "Equilibrado";
        if (_guardFoldersBox is not null) _guardFoldersBox.Clear();
        _ = StopGuardAsync();
        if (_notificationsOption is not null) _notificationsOption.IsChecked = defaults.NotifyOnScanEnd;
        _settings.AutoUpdateEngine = defaults.AutoUpdateEngine;
        _settings.AutoUpdateSignatures = defaults.AutoUpdateSignatures;
        _settings.CheckAppUpdatesOnStartup = defaults.CheckAppUpdatesOnStartup;
        _settings.AutoInstallAppUpdates = defaults.AutoInstallAppUpdates;
        _settings.UpdateIntervalHours = defaults.UpdateIntervalHours;
        _settings.ExcludedPaths = new();
        // Las tareas existentes se administran desde la página Análisis programados.
        // Restablecer las preferencias no borra tareas de Windows.
        _scanArchivesOption.IsChecked = defaults.ScanArchives;
        _detectPuaOption.IsChecked = defaults.DetectPua;
        _showCleanOption.IsChecked = defaults.ShowCleanResults;
        _consoleExpandedOption.IsChecked = defaults.ShowTechnicalConsole;
        _autoEngine.IsChecked = defaults.AutoUpdateEngine;
        _updateSignaturesAutomatically.IsChecked = defaults.AutoUpdateSignatures;
        _checkUpdatesOnStartup.IsChecked = defaults.CheckAppUpdatesOnStartup;
        _installUpdatesAutomatically.IsChecked = defaults.AutoInstallAppUpdates;
        _checkIntervalOption.SelectedIndex = 1;
        _excludedPathsBox.Clear();
        SaveSettings();
        if (_explorerOption is not null && !AppPaths.IsPortable)
        {
            try { ExplorerIntegration.SetEnabled(false); _explorerOption.IsChecked = false; }
            catch (Exception ex) { AppendLog("No se pudo eliminar el menú contextual: " + ex.Message); }
        }
        _optionsStatus.Text = "Preferencias de fábrica restauradas.";
    }

    private Border BuildScanProgressPanel()
    {
        var card = Card(Surface, Outline);
        card.Margin = new Thickness(0, 14, 0, 0);
        var stack = new StackPanel();
        stack.Children.Add(T("Seguimiento del análisis", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 8)));
        _scanStage = T("Sin análisis activo", 13, Blue, FontWeights.SemiBold,
            new Thickness(0, 0, 0, 9));
        stack.Children.Add(_scanStage);
        var progressGrid = new Grid();
        progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _scanEstimate = T("Esperando análisis", 11, Muted);
        _scanProgressText = T("—", 12, Blue, FontWeights.Bold);
        Grid.SetColumn(_scanProgressText, 1);
        progressGrid.Children.Add(_scanEstimate);
        progressGrid.Children.Add(_scanProgressText);
        stack.Children.Add(progressGrid);
        _scanProgressBar = new ProgressBar { Height = 9, Minimum = 0, Maximum = 100,
            Value = 0, IsIndeterminate = false, Foreground = Blue, Background = Outline,
            Margin = new Thickness(0, 9, 0, 12) };
        stack.Children.Add(_scanProgressBar);
        _scanProgressContext = T("El porcentaje, si aparece, es orientativo y no confirma cobertura.",
            11, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 11));
        stack.Children.Add(_scanProgressContext);
        _scanFinalCard = new Border
        {
            Background = GreenPale, BorderBrush = Outline,
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(13), Margin = new Thickness(0, 0, 0, 13),
            Visibility = Visibility.Collapsed
        };
        var finalStack = new StackPanel();
        _scanFinalTitle = T("Resultado del análisis", 15, Ink, FontWeights.Bold);
        _scanFinalDetails = T("", 11, Ink, FontWeights.Normal,
            new Thickness(0, 6, 0, 0));
        finalStack.Children.Add(_scanFinalTitle);
        finalStack.Children.Add(_scanFinalDetails);
        _scanFinalCard.Child = finalStack;
        stack.Children.Add(_scanFinalCard);
        stack.Children.Add(T("ÚLTIMO ARCHIVO INFORMADO POR CLAMAV", 10, Muted, FontWeights.Bold,
            new Thickness(0, 0, 0, 5)));
        _scanCurrentFile = T("Todavía no hay resultados de archivos", 12, Ink, FontWeights.Normal,
            new Thickness(0, 0, 0, 12));
        _scanCurrentFile.TextTrimming = TextTrimming.CharacterEllipsis;
        _scanCurrentFile.ToolTip = "La ruta aparece al terminar el análisis de cada archivo, no al comenzar.";
        stack.Children.Add(_scanCurrentFile);

        var stats = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        for (var i = 0; i < 4; i++) stats.ColumnDefinitions.Add(new ColumnDefinition
        { Width = new GridLength(1, GridUnitType.Star) });
        _scanProcessedText = T("0", 19, Ink, FontWeights.Bold);
        _scanThreatsText = T("0", 19, Ink, FontWeights.Bold);
        _scanErrorsText = T("0", 19, Ink, FontWeights.Bold);
        _scanElapsedText = T("00:00", 19, Ink, FontWeights.Bold);
        AddCounter(stats, 0, "INFORMADOS", _scanProcessedText);
        AddCounter(stats, 1, "DETECCIONES", _scanThreatsText);
        AddCounter(stats, 2, "ERRORES", _scanErrorsText);
        AddCounter(stats, 3, "TIEMPO", _scanElapsedText);
        stack.Children.Add(stats);
        var perfPanel = Card(BluePale, Outline);
        perfPanel.Margin = new Thickness(0, 3, 0, 11);
        var perfStack = new StackPanel();
        perfStack.Children.Add(T("SCAN ENGINE PRO · RENDIMIENTO Y ESTABILIDAD", 11, Blue, FontWeights.Bold,
            new Thickness(0, 0, 0, 9)));
        var perfGrid = new Grid();
        for (var i = 0; i < 4; i++) perfGrid.ColumnDefinitions.Add(new ColumnDefinition
        { Width = new GridLength(1, GridUnitType.Star) });
        _scanFilesRateText = T("—", 18, Ink, FontWeights.Bold);
        _scanLogicalRateText = T("—", 18, Ink, FontWeights.Bold);
        _scanCpuText = T("—", 18, Ink, FontWeights.Bold);
        _scanRamText = T("—", 18, Ink, FontWeights.Bold);
        AddCounter(perfGrid, 0, "ARCHIVOS / SEG.", _scanFilesRateText);
        AddCounter(perfGrid, 1, "MiB LÓGICOS / SEG.", _scanLogicalRateText);
        AddCounter(perfGrid, 2, "CPU MOTOR (EQUIPO)", _scanCpuText);
        AddCounter(perfGrid, 3, "RAM DEL MOTOR", _scanRamText);
        perfStack.Children.Add(perfGrid);
        _scanEngineActivityText = T("Motor en espera", 12, Muted, FontWeights.SemiBold,
            new Thickness(0, 10, 0, 3));
        perfStack.Children.Add(_scanEngineActivityText);
        _scanPerformanceNote = T("Las velocidades se calculan a partir de resultados notificados; " +
            "el tamaño lógico excluye archivos virtuales y no representa lectura física de disco.",
            10.5, Muted, FontWeights.Normal);
        perfStack.Children.Add(_scanPerformanceNote);
        perfPanel.Child = perfStack;
        stack.Children.Add(perfPanel);
        var chartCard = Card(Surface, Outline);
        chartCard.Margin = new Thickness(0, 1, 0, 12);
        var chartStack = new StackPanel();
        chartStack.Children.Add(T("TENDENCIAS EN VIVO · ÚLTIMAS 120 MUESTRAS", 11, Blue,
            FontWeights.Bold, new Thickness(0, 0, 0, 6)));
        chartStack.Children.Add(T("Azul: archivos/s · Violeta: CPU del motor · Verde: RAM del motor. " +
            "Cada trazo se escala independientemente; son tendencias, no magnitudes comparables entre sí.",
            10.5, Muted));
        var chartHost = new Viewbox { Stretch = Stretch.Fill, Height = 84,
            Margin = new Thickness(0, 10, 0, 8) };
        var chartCanvas = new Canvas { Width = 690, Height = 82, Background = BluePale };
        chartCanvas.Children.Add(new Line { X1 = 4, X2 = 685, Y1 = 76, Y2 = 76,
            Stroke = Outline, StrokeThickness = 1 });
        _liveRatePolyline = new Polyline { Stroke = Blue, StrokeThickness = 2.5 };
        _liveCpuPolyline = new Polyline { Stroke = Purple, StrokeThickness = 2.1 };
        _liveRamPolyline = new Polyline { Stroke = Green, StrokeThickness = 2.1 };
        chartCanvas.Children.Add(_liveRatePolyline);
        chartCanvas.Children.Add(_liveCpuPolyline);
        chartCanvas.Children.Add(_liveRamPolyline);
        chartHost.Child = chartCanvas;
        chartStack.Children.Add(chartHost);
        var compareButton = ActionButton("▥  Historial gráfico / comparar análisis", false,
            (_, _) => new PerformanceWindow(this).ShowDialog());
        chartStack.Children.Add(compareButton);
        chartCard.Child = chartStack;
        stack.Children.Add(chartCard);
        stack.Children.Add(T("La cifra de archivos se obtiene de la salida del motor. El porcentaje, cuando se muestra, " +
            "compara con una estimación previa; ZIP, permisos y cambios durante el análisis pueden alterar el total.",
            11, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 11)));
        // Las acciones están en la barra inferior fija, no al final del panel desplazable.
        card.Child = stack;
        return card;
    }

    private Border BuildScanActionDock()
    {
        var bar = new Border
        {
            Background = Surface,
            BorderBrush = Outline,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(25, 10, 25, 10),
            Visibility = Visibility.Collapsed,
            Effect = new DropShadowEffect
            {
                Color = C("#283A60"), Opacity = 0.10, BlurRadius = 12, ShadowDepth = -2
            }
        };
        var layout = new Grid();
        layout.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star)
        });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var description = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        description.Children.Add(T("CONTROL DEL ANÁLISIS", 10, Blue,
            FontWeights.Bold, new Thickness(0, 0, 0, 3)));
        _scanActionHint = T("●  Sin análisis en curso. Puedes iniciar uno cuando quieras.",
            11, Muted, FontWeights.SemiBold);
        _scanActionHint.TextWrapping = TextWrapping.Wrap;
        description.Children.Add(_scanActionHint);
        _dockPath = T("Preparado para analizar", 11, Ink, FontWeights.SemiBold,
            new Thickness(0, 3, 0, 0));
        _dockPath.TextTrimming = TextTrimming.CharacterEllipsis;
        description.Children.Add(_dockPath);
        layout.Children.Add(description);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0, 0, 0)
        };
        _dockPauseButton = ActionButton("Ⅱ  Pausar", false,
            (_, _) => ToggleScanPause());
        SetSemanticButton(_dockPauseButton, "warn");
        _dockPauseButton.MinWidth = 136;
        _dockPauseButton.Margin = new Thickness(0,0,7,0);
        _dockPauseButton.Visibility = Visibility.Collapsed;
        actions.Children.Add(_dockPauseButton);
        // El rojo se reserva para detener un proceso REALMENTE activo.
        _cancelButton = ActionButton("■  Detener análisis", false,
            (_, _) => RequestStopCurrentOperation());
        SetSemanticButton(_cancelButton, "danger");
        _cancelButton.MinWidth = 175;
        _cancelButton.Margin = new Thickness(0);
        _cancelButton.Visibility = Visibility.Collapsed;
        _cancelButton.IsEnabled = false;
        actions.Children.Add(_cancelButton);

        _retryScanButton = ActionButton("↻  Reintentar análisis incompleto", false,
            (_, _) => _ = _lastScanPlan is { } plan ? StartScanPlanAsync(plan) : StartScanAsync(_targetBox.Text));
        _retryScanButton.MinWidth = 225;
        _retryScanButton.Margin = new Thickness(0);
        _retryScanButton.Visibility = Visibility.Collapsed;
        actions.Children.Add(_retryScanButton);
        Grid.SetColumn(actions, 1);
        layout.Children.Add(actions);
        bar.Child = layout;
        return bar;
    }

    private Border BuildScanResultsPanel()
    {
        var card = Card(Surface, Outline);
        card.Margin = new Thickness(0, 14, 0, 0);
        var stack = new StackPanel();
        stack.Children.Add(T("Resultados de archivos", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 8)));
        stack.Children.Add(T("Últimos 350 resultados informados por ClamAV. Las amenazas y los errores se " +
            "conservan en el informe detallado local.", 11, Muted, FontWeights.Normal,
            new Thickness(0, 0, 0, 12)));
        _resultFilter = new ComboBox { Width = 190, Height = 32, HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            ItemsSource = new[] { "Todos", "Detecciones", "Errores" }, SelectedIndex = 0,
            Margin = new Thickness(0, 0, 0, 10) };
        _resultFilter.SelectionChanged += (_, _) => RefreshResultsView();
        stack.Children.Add(_resultFilter);
        _scanResultsList = new ListView { Height = 235, FontSize = 11.5,
            BorderBrush = Outline, BorderThickness = new Thickness(1),
            Background = Surface, Foreground = Ink };
        var view = new GridView();
        view.Columns.Add(new GridViewColumn { Header = "Estado", Width = 115,
            DisplayMemberBinding = new Binding(nameof(ScanFileEvent.DisplayStatus)) });
        view.Columns.Add(new GridViewColumn { Header = "Archivo", Width = 395,
            DisplayMemberBinding = new Binding(nameof(ScanFileEvent.FilePath)) });
        view.Columns.Add(new GridViewColumn { Header = "Detalle", Width = 205,
            DisplayMemberBinding = new Binding(nameof(ScanFileEvent.Detail)) });
        _scanResultsList.View = view;
        stack.Children.Add(_scanResultsList);
        var quarantineRow = new WrapPanel { Margin = new Thickness(0, 9, 0, 2) };
        quarantineRow.Children.Add(ActionButton("▧  Aislar detección seleccionada", false,
            async (_, _) => await IsolateDetectionAsync()));
        var threatDelete = ActionButton("✕  Aislar y eliminar…", false,
            async (_, _) => await IsolateAndDeleteDetectionAsync());
        threatDelete.Background = B("#FDE8E8");
        threatDelete.Foreground = B("#AE1C30");
        threatDelete.BorderBrush = B("#F4B8C1");
        quarantineRow.Children.Add(threatDelete);
        quarantineRow.Children.Add(ActionButton("Ver cuarentena", false, (_, _) => Navigate("quarantine")));
        stack.Children.Add(quarantineRow);
        stack.Children.Add(T("Solo se pueden aislar archivos reales detectados por ClamAV; " +
            "debes seleccionarlos y confirmar. La eliminación exige dos confirmaciones: " +
            "primero aislar y después eliminar. No se realizan acciones automáticas.",
            11, Muted, FontWeights.Normal, new Thickness(0, 3, 0, 9)));
        _openScanReport = ActionButton("▤  Abrir informe completo TXT", false, (_, _) => OpenLocalReport(_scanLogPath));
        _openScanReport.IsEnabled = false;
        _openScanReport.Margin = new Thickness(0, 10, 0, 0);
        stack.Children.Add(_openScanReport);
        card.Child = stack;
        return card;
    }

    private void RefreshResultsView()
    {
        if (_scanResultsList is null || _resultFilter is null) return;
        var filter = _resultFilter.SelectedIndex;
        _scanResultsList.ItemsSource = _recentResults.Where(item => filter switch
        {
            1 => item.Status == ScanFileStatus.Detected,
            2 => item.Status == ScanFileStatus.Error,
            _ => _settings.ShowCleanResults || item.Status != ScanFileStatus.Clean
        }).ToArray();
    }

    private void OnFilesDragEnter(object sender, DragEventArgs e)
    {
        e.Effects = !_busy && !_inspectorScanActive && e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnFilesDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (_busy || _installingAppUpdate || _inspectorScanActive) return;
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length != 1)
        {
            MessageBox.Show(this, "Arrastra un único archivo o carpeta para analizar.",
                "PalClam", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        // En el Inspector, arrastrar significa examinar: nunca iniciar un proceso ClamAV
        // ni tratar el archivo como correo automáticamente.
        if (ReferenceEquals(_pageHost.Content, _pages["inspector"]))
        {
            if (File.Exists(paths[0])) _ = InspectSelectedFileAsync(paths[0]);
            else MessageBox.Show(this, "El inspector solo acepta un archivo individual.",
                "Inspector de archivos", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (File.Exists(paths[0]) && MailScanService.IsSupported(paths[0]))
        {
            Navigate("mail");
            _mailPathBox.Text = paths[0];
            _ = StartMailScanAsync();
        }
        else _ = StartScanAsync(paths[0]);
    }

    private void OnMainKeyDown(object sender, KeyEventArgs e)
    {
        // F1 conserva los consejos; Mayús+F1 abre la ayuda de funciones y ventajas.
        if (e.Key == Key.F1)
        {
            Navigate((Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? "help" : "advice");
            e.Handled = true;
            return;
        }
        if ((e.Key == Key.Home || (e.Key == Key.System && e.SystemKey == Key.Home)) &&
            Keyboard.Modifiers == ModifierKeys.Alt)
        {
            Navigate("dashboard");
            e.Handled = true;
            return;
        }
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        if (e.Key == Key.OemComma)
        {
            OpenOptionsTab(0);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.O)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) PickFolder();
            else PickFile();
            Navigate("scan");
            e.Handled = true;
        }
    }

    private void SaveExplorerIntegration()
    {
        if (_explorerOption is null || _settingExplorerOption) return;
        _settingExplorerOption = true;
        try
        {
            ExplorerIntegration.SetEnabled(_explorerOption.IsChecked == true,
                PalClam.Localization.DisplayText.Translate("Analizar con PalClam"));
            _optionsStatus.Text = _explorerOption.IsChecked == true
                ? "Menú contextual habilitado en tu cuenta de Windows."
                : "Menú contextual eliminado de tu cuenta de Windows.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo configurar el Explorador: " + ex.Message,
                "Menú contextual", MessageBoxButton.OK, MessageBoxImage.Warning);
            // Reflejar estado real sin volver a disparar la configuración.
            var real = ExplorerIntegration.IsEnabled();
            if (_explorerOption.IsChecked != real) _explorerOption.IsChecked = real;
        }
        finally
        {
            _settingExplorerOption = false;
            if (_explorerIntegrationText is not null)
                _explorerIntegrationText.Text = ExplorerIntegration.Status();
        }
    }

    private void BuildDiagnosticsPage(StackPanel page)
    {
        page.Children.Add(T("Diagnóstico de PalClam", 22, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 7)));
        page.Children.Add(T("Comprobaciones locales del motor, las bases de firmas, el entorno y el Explorador. " +
            "El informe puede contener rutas personales: revisa antes de compartirlo.", 12, Muted,
            FontWeights.Normal, new Thickness(0, 0, 0, 13)));
        var card = Card(Surface, Outline);
        var stack = new StackPanel();
        var actions = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        actions.Children.Add(ActionButton("↻  Volver a comprobar", true, (_, _) => _ = RefreshDiagnosticsAsync()));
        actions.Children.Add(ActionButton("Copiar diagnóstico", false, (_, _) =>
        {
            try { Clipboard.SetText(_diagnosticBox.Text); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Copiar", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }));
        actions.Children.Add(ActionButton("Guardar TXT...", false, (_, _) =>
        {
            var dlg = new SaveFileDialog { Title = "Guardar diagnóstico", Filter = "Texto|*.txt",
                FileName = "PalClam_diagnostico_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".txt" };
            if (PalClam.UI.LocalizedDialogs.Show(dlg, this) == true)
            {
                try { File.WriteAllText(dlg.FileName, _diagnosticBox.Text); }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "Guardar", MessageBoxButton.OK, MessageBoxImage.Warning); }
            }
        }));
        stack.Children.Add(actions);
        _diagnosticBox = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.NoWrap,
            AcceptsReturn = true, Height = 435, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, FontFamily = new FontFamily("Consolas"),
            FontSize = 12, Padding = new Thickness(12), Background = BackgroundColor,
            BorderBrush = Outline, Text = "Abre esta sección para comprobar el estado." };
        PalClam.UI.PresentationLocalization.SetReport(_diagnosticBox, _diagnosticBox.Text);
        stack.Children.Add(_diagnosticBox);
        card.Child = stack;
        page.Children.Add(card);
    }

    private async Task RefreshDiagnosticsAsync()
    {
        if (_diagnosticBox is null || _engineBox is null) return;
        PalClam.UI.PresentationLocalization.SetReport(_diagnosticBox, "Comprobando el estado local...");
        string version = "No comprobada";
        var scanner = _engineBox.Text;
        if (ClamAvLocator.IsValidScanner(scanner))
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            try
            {
                var versionLines = new List<string>();
                var result = await ProcessRunner.RunAsync(scanner, new[] { "--version" },
                    (line, _) => { lock (versionLines) versionLines.Add(line); }, timeout.Token);
                version = result.ExitCode == 0 ? string.Join(" ", versionLines.Take(2))
                    : "No se pudo obtener versión (salida " + result.ExitCode + ")";
            }
            catch (Exception ex) { version = "No disponible: " + ex.GetType().Name; }
        }
        PalClam.UI.PresentationLocalization.SetReport(_diagnosticBox, DiagnosticReport.Create(scanner, version));
    }

    private void BeginScanTelemetry(string target, IReadOnlyList<string> targets)
    {
        Navigate("scan");
        Interlocked.Exchange(ref _filesReported, 0);
        Interlocked.Exchange(ref _threatsReported, 0);
        Interlocked.Exchange(ref _errorsReported, 0);
        Interlocked.Exchange(ref _engineSummaryScanned, -1);
        Interlocked.Exchange(ref _engineSummaryInfected, -1);
        _estimateCount = -1;
        Interlocked.Exchange(ref _scanLogicalBytes, 0);
        Interlocked.Exchange(ref _scanLastResultElapsedTicks, 0);
        _engineLiveSample = null;
        _activeScanMetrics = _settings.ShowScanPerformance;
        _activeScanPriority = _settings.ScanPriority;
        _scanPeakMemory = 0;
        _scanCpuSum = 0;
        _scanCpuPeak = 0;
        _scanCpuSamples = 0;
        _sampledFiles = 0;
        _sampledBytes = 0;
        _sampledAt = TimeSpan.Zero;
        _displayFilesRate = 0;
        _displayLogicalRate = 0;
        _longestSilence = 0;
        _livePerformance.Clear();
        _lastChartSampleTicks = 0;
        if (_liveRatePolyline is not null) _liveRatePolyline.Points.Clear();
        if (_liveCpuPolyline is not null) _liveCpuPolyline.Points.Clear();
        if (_liveRamPolyline is not null) _liveRamPolyline.Points.Clear();
        _inaccessibleCount = 0;
        _estimateTruncated = false;
        _scanLogPath = "";
        _lastReportedPath = "";
        _scanBatch = null;
        _scanPausedBetweenBatches = false;
        _floatingHiddenByUser = false;
        _scanRunning = true;
        unchecked { _scanGeneration++; }
        _scanEverStarted = true;
        UpdateTrayState();
        _scanFinishedNormally = false;
        _recentResults.Clear();
        while (_pendingResults.TryDequeue(out _)) { }
        Interlocked.Exchange(ref _pendingResultCount, 0);
        while (_pendingMessages.TryDequeue(out _)) { }
        RefreshResultsView();
        _scanStage.Text = "Preparando motor y analizando...";
        _scanCurrentFile.Text = "Esperando el primer resultado del motor...";
        _scanEstimate.Text = "Estimando cantidad de archivos...";
        _scanFinalCard.Visibility = Visibility.Collapsed;
        _scanProgressContext.Text = "Preparando el análisis. El porcentaje será orientativo cuando haya datos fiables.";
        _scanResultRate.Reset();
        _scanPercentVisible = null;
        _scanEtaMessage = "";
        _scanProgressText.Text = "Calculando...";
        _scanProgressBar.IsIndeterminate = true;
        _scanProgressBar.Value = 0;
        _openScanReport.IsEnabled = false;
        _scanClock = Stopwatch.StartNew();
        _estimateCancellation = new CancellationTokenSource();
        var excludedPaths = ScanExclusions.Normalize(_settings.ExcludedPaths);
        _estimateTask = Task.Run(() => ScanFileEstimator.Estimate(targets,
            _estimateCancellation.Token, excludedPaths), _estimateCancellation.Token);
        _scanUiTimer ??= new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(250) };
        _scanUiTimer.Tick -= OnScanUiTick;
        _scanUiTimer.Tick += OnScanUiTick;
        _scanUiTimer.Start();
        RefreshScanProgress();
    }

    private void OnScanUiTick(object? sender, EventArgs e) => RefreshScanProgress();

    private void RefreshScanProgress()
    {
        if (_scanStage is null) return;
        var changed = false;
        var processedOnUi = 0;
        while (processedOnUi < 250 && _pendingResults.TryDequeue(out var item))
        {
            Interlocked.Decrement(ref _pendingResultCount);
            _lastReportedPath = item.FilePath;
            _recentResults.Add(item);
            if (_recentResults.Count > 350)
            {
                var firstClean = _recentResults.FindIndex(r => r.Status == ScanFileStatus.Clean);
                _recentResults.RemoveAt(firstClean >= 0 ? firstClean : 0);
            }
            processedOnUi++;
            changed = true;
        }
        // Agrupar mensajes de advertencia; NO volcar un evento WPF por archivo.
        var warnings = 0;
        while (warnings < 20 && _pendingMessages.TryDequeue(out var line))
        {
            AppendLog(line);
            warnings++;
        }
        if (changed) RefreshResultsView();
        if (!string.IsNullOrWhiteSpace(_lastReportedPath))
        {
            _scanCurrentFile.Text = _lastReportedPath;
            _scanCurrentFile.ToolTip = _lastReportedPath;
        }
        var processed = Interlocked.Read(ref _filesReported);
        var threats = Interlocked.Read(ref _threatsReported);
        var errors = Interlocked.Read(ref _errorsReported);
        _scanProcessedText.Text = processed.ToString("N0");
        _scanThreatsText.Text = threats.ToString("N0");
        _scanErrorsText.Text = errors.ToString("N0");
        _scanThreatsText.Foreground = threats > 0 ? Red : Ink;
        _scanErrorsText.Foreground = errors > 0 ? Amber : Ink;
        _scanProgressBar.Foreground = threats > 0 ? Red : errors > 0 ? Amber : Blue;
        if (_scanClock is not null) _scanElapsedText.Text = _scanClock.Elapsed.ToString(@"hh\:mm\:ss");
        RefreshScanPerformance();
        if (_estimateTask is { IsCompletedSuccessfully: true } && _estimateCount == -1)
        {
            var estimate = _estimateTask.Result;
            _estimateCount = estimate.Count;
            _inaccessibleCount = estimate.InaccessibleDirectories;
            _estimateTruncated = estimate.Truncated;
        }
        var stopping = _scanRunning && _running?.IsCancellationRequested == true;
        var pauseRequested = _scanRunning && _scanPauseGate?.IsPauseRequested == true;
        var paused = pauseRequested && _scanPausedBetweenBatches;
        var phase = !_scanRunning
            ? (_scanFinishedNormally ? ScanProgressPhase.Completed : ScanProgressPhase.Incomplete)
            : stopping ? ScanProgressPhase.Stopping
            : paused ? ScanProgressPhase.Paused
            : pauseRequested ? ScanProgressPhase.Pausing
            : ScanProgressPhase.Running;
        var elapsedSeconds = _scanClock?.Elapsed.TotalSeconds ?? 0;
        var smoothRate = _scanResultRate.Observe(processed, elapsedSeconds,
            phase == ScanProgressPhase.Running);
        var display = ScanProgressPolicy.Evaluate(phase, processed, _estimateCount,
            _estimateTruncated, _inaccessibleCount,
            _estimateTask is { IsCompleted: false }, elapsedSeconds, smoothRate);
        _scanPercentVisible = display.Percent;
        _scanEtaMessage = display.Eta;
        _scanProgressBar.IsIndeterminate = display.IsIndeterminate;
        if (!display.IsIndeterminate) _scanProgressBar.Value = display.Percent ?? 0;
        _scanProgressText.Text = display.Label;
        if (_scanRunning)
        {
            var stage = phase switch
            {
                ScanProgressPhase.Paused => "Pausa aplicada · esperando reanudación",
                ScanProgressPhase.Pausing => "Pausa solicitada · completando lote actual",
                ScanProgressPhase.Stopping => "Deteniendo análisis · esperando al motor",
                _ => processed == 0 ? "Preparando motor y analizando…" : "Analizando archivos con ClamAV…"
            };
            if (PalClam.UI.PresentationLocalization.Original(_scanStage) != stage) _scanStage.Text = stage;
            _scanStage.Foreground = phase is ScanProgressPhase.Pausing or
                ScanProgressPhase.Paused or ScanProgressPhase.Stopping ? Amber : Blue;
        }
        _scanEstimate.Text = display.Coverage;
        _scanProgressContext.Text = display.Guidance +
            (string.IsNullOrWhiteSpace(display.Eta) ? "" : "  ·  " + display.Eta);
        _scanProgressContext.ToolTip = display.Guidance;
        RefreshScanControlUx();
    }

    private void RefreshScanPerformance()
    {
        if (_scanClock is null || _scanFilesRateText is null) return;
        if (!_activeScanMetrics)
        {
            _scanFilesRateText.Text = _scanLogicalRateText.Text = "Oculto";
            _scanCpuText.Text = _scanRamText.Text = "Oculto";
            _scanEngineActivityText.Text = "Telemetría desactivada desde Opciones";
            return;
        }
        var elapsed = _scanClock.Elapsed;
        var seconds = (elapsed - _sampledAt).TotalSeconds;
        if (seconds >= 0.8)
        {
            var files = Interlocked.Read(ref _filesReported);
            var bytes = Interlocked.Read(ref _scanLogicalBytes);
            _displayFilesRate = ScanPerformanceMath.Rate(files - _sampledFiles, seconds);
            _displayLogicalRate = ScanPerformanceMath.Rate(bytes - _sampledBytes, seconds) / 1048576.0;
            _sampledFiles = files;
            _sampledBytes = bytes;
            _sampledAt = elapsed;
        }
        _scanFilesRateText.Text = _scanRunning && !_scanPausedBetweenBatches ? $"{_displayFilesRate:0.0}" : _scanPausedBetweenBatches ? "0.0" : "—";
        _scanLogicalRateText.Text = _scanRunning && !_scanPausedBetweenBatches ? $"{_displayLogicalRate:0.0}" : _scanPausedBetweenBatches ? "0.0" : "—";
        var sample = _scanPausedBetweenBatches ? null : Volatile.Read(ref _engineLiveSample);
        if (sample is not null)
        {
            _scanCpuText.Text = $"{sample.MachineCpuPercent:0.0} %";
            _scanRamText.Text = $"{sample.WorkingSetBytes / 1048576.0:0} MiB";
            _scanCpuText.ToolTip = "Consumo de CPU del proceso dividido entre los procesadores lógicos del sistema.";
        }
        else
        {
            _scanCpuText.Text = _scanRunning ? "Midiendo…" : "—";
            _scanRamText.Text = _scanRunning ? "Midiendo…" : "—";
        }
        if (_scanRunning && (elapsed.Ticks - _lastChartSampleTicks) >= TimeSpan.TicksPerSecond)
        {
            _lastChartSampleTicks = elapsed.Ticks;
            _livePerformance.Add(new PerformancePoint(elapsed.TotalSeconds,
                _displayFilesRate, sample?.MachineCpuPercent ?? 0,
                (sample?.WorkingSetBytes ?? 0) / 1048576.0));
            RefreshLiveCharts();
        }
        var last = TimeSpan.FromTicks(Interlocked.Read(ref _scanLastResultElapsedTicks));
        var silentSeconds = Math.Max(0, (elapsed - last).TotalSeconds);
        _longestSilence = Math.Max(_longestSilence, silentSeconds);
        if (_scanRunning)
        {
            var paused = _scanPausedBetweenBatches && _scanPauseGate?.IsPauseRequested == true;
            _scanEngineActivityText.Text = paused ? "Análisis pausado entre lotes; sin proceso ClamAV activo." :
                $"{ScanPerformanceMath.QuietStatus(silentSeconds, true)} · sin resultados desde hace {silentSeconds:0}s";
            _scanEngineActivityText.Foreground = paused ? Blue : silentSeconds >= 90 ? Amber : Muted;
        }
        else
        {
            _scanEngineActivityText.Text = "Motor detenido · consulta el veredicto y el resumen final.";
            _scanEngineActivityText.Foreground = Muted;
        }
    }

    private void RefreshLiveCharts()
    {
        if (_liveRatePolyline is null) return;
        var samples = _livePerformance.Samples;
        var rate = ScanPerformanceSeries.Normalize(samples.Select(x => x.FilesPerSecond));
        var cpu = ScanPerformanceSeries.Normalize(samples.Select(x => x.CpuPercent));
        var ram = ScanPerformanceSeries.Normalize(samples.Select(x => x.WorkingSetMiB));
        void Update(Polyline line, double[] normalized)
        {
            line.Points.Clear();
            for (var i = 0; i < normalized.Length; i++)
                line.Points.Add(new System.Windows.Point(4 + (i * 681.0 / 119.0), 76 - normalized[i] * 66));
        }
        Update(_liveRatePolyline, rate);
        Update(_liveCpuPolyline, cpu);
        Update(_liveRamPolyline, ram);
    }

    private void FinishScanTelemetry(string finalStatus, string outcome)
    {
        _scanRunning = false;
        UpdateTrayState();
        _scanFinishedNormally = finalStatus.StartsWith("Análisis finalizado:", StringComparison.Ordinal);
        _scanStage.Text = finalStatus;
        _scanStage.Foreground = finalStatus.Contains("amenazas", StringComparison.OrdinalIgnoreCase)
            ? Red : _scanFinishedNormally ? Green : Amber;
        _scanClock?.Stop();
        _scanUiTimer?.Stop();
        _estimateCancellation?.Cancel();
        RefreshScanProgress();
        var threats = Interlocked.Read(ref _threatsReported);
        var errors = Interlocked.Read(ref _errorsReported);
        var engineScanned = Interlocked.Read(ref _engineSummaryScanned);
        _scanFinalCard.Background = _scanFinishedNormally
            ? threats > 0 ? RedPale : GreenPale : AmberPale;
        _scanFinalTitle.Text = _scanFinishedNormally
            ? threats > 0 ? "ATENCIÓN · Detecciones registradas" : "Análisis finalizado"
            : "REVISAR · Análisis no completado";
        _scanFinalTitle.Foreground = _scanFinishedNormally
            ? threats > 0 ? Red : Green : Amber;
        _scanFinalDetails.Text = $"Resultado: {outcome}. " +
            $"{Interlocked.Read(ref _filesReported):N0} resultados de archivos; " +
            $"{threats:N0} detecciones notificadas; {errors:N0} errores contabilizados. " +
            (engineScanned >= 0
                ? $"ClamAV confirma {engineScanned:N0} archivos examinados. "
                : "ClamAV no proporcionó un recuento total confirmado. ") +
            (_scanFinishedNormally
                ? "Consulta el informe: un resultado sin detecciones no garantiza protección total."
                : "No hay veredicto sobre los archivos pendientes o inaccesibles.");
        _scanFinalCard.Visibility = Visibility.Visible;
        // El estado ya está cerrado; no dejar visible la acción roja de parada.
        _cancelButton.Visibility = Visibility.Collapsed;
        _cancelButton.IsEnabled = false;
        if (_topScanStopButton is not null) _topScanStopButton.Visibility = Visibility.Collapsed;
        _scanActionHint.Visibility = Visibility.Visible;
        _scanActionHint.Text = _scanFinishedNormally
            ? "✓  Análisis finalizado. Inicia otro análisis cuando lo necesites."
            : "●  Análisis interrumpido o incompleto. Quedan archivos sin verificar.";
        _scanActionHint.Foreground = _scanFinishedNormally ? Green : Amber;
        _openScanReport.IsEnabled = !string.IsNullOrWhiteSpace(_scanLogPath) && File.Exists(_scanLogPath);
    }

    private void OpenLocalReport(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            MessageBox.Show(this, "Todavía no hay un informe detallado disponible.",
                "Informe de análisis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo abrir el informe local: " + ex.Message,
                "Informe de análisis", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static TextBlock T(string value, double size, Brush color) =>
        T(value, size, color, FontWeights.Normal);

    private static TextBlock T(string value, double size, Brush color, FontWeight weight, Thickness? margin = null) =>
        PalClam.UI.PresentationLocalization.Track(new TextBlock { Text = value, FontSize = size, Foreground = color, FontWeight = weight,
            TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center,
            Margin = margin ?? new Thickness(0) });

    private static Border Card(Brush fill, Brush border) => new()
    {
        Background = fill, BorderBrush = border, BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(16), Padding = new Thickness(18),
        Effect = new DropShadowEffect
        {
            Color = C("#334466"), Opacity = 0.035, BlurRadius = 12, ShadowDepth = 2
        },
    };

    private static Color C(string html) => (Color)System.Windows.Media.ColorConverter.ConvertFromString(
        AppearanceCatalog.MapColor(ActiveTheme, html))!;

    private static void RoundButton(Button button, double radius = 11)
    {
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
        border.SetBinding(Border.BackgroundProperty,
            new Binding("Background") { RelativeSource = RelativeSource.TemplatedParent });
        border.SetBinding(Border.BorderBrushProperty,
            new Binding("BorderBrush") { RelativeSource = RelativeSource.TemplatedParent });
        border.SetBinding(Border.BorderThicknessProperty,
            new Binding("BorderThickness") { RelativeSource = RelativeSource.TemplatedParent });
        border.SetBinding(Border.PaddingProperty,
            new Binding("Padding") { RelativeSource = RelativeSource.TemplatedParent });
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetBinding(ContentPresenter.VerticalAlignmentProperty,
            new Binding("VerticalContentAlignment") { RelativeSource = RelativeSource.TemplatedParent });
        presenter.SetBinding(ContentPresenter.HorizontalAlignmentProperty,
            new Binding("HorizontalContentAlignment") { RelativeSource = RelativeSource.TemplatedParent });
        border.AppendChild(presenter);
        template.VisualTree = border;
        button.Template = template;
        button.MouseEnter += (_, _) => { if (button.IsEnabled) button.Opacity = 0.86; };
        button.MouseLeave += (_, _) => button.Opacity = 1;
    }

    private static void SetSemanticButton(Button button, string role)
    {
        switch (role)
        {
            case "danger":
                button.Background = Red; button.BorderBrush = Red;
                button.Foreground = Brushes.White; break;
            case "warn":
                button.Background = AmberPale; button.BorderBrush = B("#EDC979");
                button.Foreground = Amber; break;
            case "scan":
                button.Background = Blue; button.BorderBrush = Blue;
                button.Foreground = Brushes.White; break;
            case "light":
                // Blanco en temas claros, superficie grafito en el tema oscuro.
                button.Background = Surface; button.BorderBrush = Outline;
                button.Foreground = Purple; break;
            case "glass":
                button.Background = B("#415D93"); button.BorderBrush = B("#93A7D5");
                button.Foreground = Brushes.White; break;
        }
    }

    private Border NavGroup(string title, string groupId, Brush accent,
        params (string Symbol, string Label, string Page)[] items)
    {
        var shell = new Border
        {
            Background = Brushes.Transparent, CornerRadius = new CornerRadius(13),
            Margin = new Thickness(0, 0, 0, 7)
        };
        var group = new StackPanel();
        var labelGrid = new Grid { Margin = new Thickness(6, 4, 6, 4) };
        labelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        labelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var head = T(title, 10, accent, FontWeights.Bold);
        head.Margin = new Thickness(8, 0, 0, 0);
        labelGrid.Children.Add(head);
        var arrow = T("▾", 15, Muted, FontWeights.SemiBold);
        Grid.SetColumn(arrow, 1);
        labelGrid.Children.Add(arrow);
        var toggle = new Button
        {
            Content = labelGrid, Padding = new Thickness(2, 7, 2, 7),
            Background = Brushes.Transparent, BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand, HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch
        };
        RoundButton(toggle);
        var groupEntries = new StackPanel();
        foreach (var item in items)
        {
            groupEntries.Children.Add(Nav(item.Symbol, item.Label, item.Page));
            _navPageGroups[item.Page] = groupId;
        }
        _navGroupPanels[groupId] = groupEntries;
        _navGroupIndicators[groupId] = arrow;
        toggle.Click += (_, _) => ExpandNavigationGroup(groupId, groupEntries.Visibility != Visibility.Visible);
        group.Children.Add(toggle);
        group.Children.Add(groupEntries);
        shell.Child = group;
        ExpandNavigationGroup(groupId, groupId == "base");
        return shell;
    }

    private void ExpandNavigationGroup(string groupId, bool open)
    {
        if (open)
        {
            foreach (var pair in _navGroupPanels)
            {
                pair.Value.Visibility = pair.Key == groupId ? Visibility.Visible : Visibility.Collapsed;
                if (_navGroupIndicators.TryGetValue(pair.Key, out var indicator))
                    indicator.Text = pair.Key == groupId ? "▾" : "▸";
            }
        }
        else if (_navGroupPanels.TryGetValue(groupId, out var panel))
        {
            panel.Visibility = Visibility.Collapsed;
            if (_navGroupIndicators.TryGetValue(groupId, out var indicator)) indicator.Text = "▸";
        }
    }

    private Button Nav(string symbol, string label, string page)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        var glyph = T(symbol, 16, Muted, FontWeights.SemiBold, new Thickness(0, 0, 12, 0));
        glyph.Width = 21;
        row.Children.Add(glyph);
        row.Children.Add(T(label, 12, Slate, FontWeights.SemiBold));
        var button = new Button
        {
            Content = row, HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left,
            Background = Brushes.Transparent, BorderThickness = new Thickness(0),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(2, 1, 2, 1), Cursor = Cursors.Hand,
            ToolTip = "Abrir " + label
        };
        RoundButton(button);
        button.Click += (_, _) => Navigate(page);
        _navButtons[page] = button;
        return button;
    }

    private void Navigate(string page)
    {
        if (!_pages.TryGetValue(page, out var selected)) return;
        _pageHost.Content = selected;
        // Solo la vista Análisis muestra los controles fijos; el resto recupera
        // toda la altura de contenido al ocultar la barra (GridLength.Auto).
        _scanActionDock.Visibility = page == "scan" || _scanRunning ? Visibility.Visible : Visibility.Collapsed;
        _pageScroll.ScrollToTop();
        if (_navPageGroups.TryGetValue(page, out var groupId))
            ExpandNavigationGroup(groupId, true);
        foreach (var pair in _navButtons)
        {
            var active = pair.Key == page;
            pair.Value.Background = active ? PurplePale : Brushes.Transparent;
            pair.Value.BorderBrush = active ? B("#E5D8FF") : Brushes.Transparent;
            pair.Value.BorderThickness = active ? new Thickness(1) : new Thickness(0);
            if (pair.Value.Content is StackPanel row)
            {
                var blocks = row.Children.OfType<TextBlock>().ToArray();
                if (blocks.Length >= 2)
                {
                    blocks[0].Foreground = active ? Purple : Muted;
                    blocks[1].Foreground = active ? Purple : Slate;
                }
            }
        }
        if (page == "dashboard")
        {
            RefreshDashboardUi();
            _ = RefreshDashboardInsightsAsync(force: true);
        }
        if (page == "inspector") RefreshInspectorActions();
        if (page == "history") RefreshHistory();
        if (page == "alerts") _ = RefreshSecurityCenterAsync();
        if (page == "health") _ = RefreshProtectionHealthAsync();
        if (page == "devices") RefreshRemovableDrives();
        if (page == "signatures")
        {
            _ = RefreshSignatureMetadataAsync();
            _ = RefreshSignatureCenterAsync();
        }
        if (page == "diagnostics") _ = RefreshDiagnosticsAsync();
        if (page == "advice") RefreshAdviceStatus();
        if (page == "selfdefense") RefreshSelfDefenseUi();
        if (page == "quarantine") RefreshQuarantine();
        if (page == "schedule") _ = RefreshScheduleAsync();
    }

    private static void AddModule(Grid grid, int col, string symbol, string label,
        TextBlock state, Brush badgeBackground, Brush badgeColor)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(12), Background = B("#F8F9FD"),
            BorderBrush = Outline, BorderThickness = new Thickness(1),
            Margin = new Thickness(col == 0 ? 0 : 5, 0, col == 2 ? 0 : 5, 0),
            Padding = new Thickness(12)
        };
        var stack = new StackPanel();
        var top = new StackPanel { Orientation = Orientation.Horizontal };
        top.Children.Add(new Border
        {
            Background = badgeBackground, CornerRadius = new CornerRadius(9),
            Width = 28, Height = 28, Margin = new Thickness(0, 0, 9, 0),
            Child = CreateDashboardModuleIcon(symbol, badgeColor)
        });
        var labelText = T(label, 12, Ink, FontWeights.SemiBold);
        labelText.TextWrapping = TextWrapping.Wrap;
        top.Children.Add(labelText);
        stack.Children.Add(top);
        state.Margin = new Thickness(0, 9, 0, 0);
        state.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(state);
        card.Child = stack;
        Grid.SetColumn(card, col);
        grid.Children.Add(card);
    }

    private void RefreshDashboardUi()
    {
        if (_dashboardMoodBorder is null || _status is null || _engineBox is null) return;
        var status = _status.Text ?? "";
        var running = _scanRunning;
        var threat = _trayAlert || status.StartsWith("Detecciones", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("Amenazas detectadas", StringComparison.OrdinalIgnoreCase);
        var inconclusive = status.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("Cancel", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("Incompleto", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("incidencia", StringComparison.OrdinalIgnoreCase);
        var signatureHealth = SignatureHealthService.Evaluate(SignatureStore.HasSignatures(),
            SignatureHealthService.DailyFileTimeUtc(SignatureStore.DatabaseDirectory),
            _settings.LastSignatureCheckUtc, _settings.LastSignatureAttemptUtc,
            _settings.LastSignatureUpdateError, DateTimeOffset.UtcNow);
        var ready = ClamAvLocator.IsValidScanner(_engineBox.Text) && SignatureStore.HasSignatures();
        var hue = running ? Blue : threat ? Red : inconclusive ? Amber : ready ? Green : Amber;
        if (_dashboardHeroReadiness is not null)
        {
            _dashboardHeroReadiness.Text = running ?
                (_scanPausedBetweenBatches && _scanPauseGate?.IsPauseRequested == true
                    ? "Análisis pausado" : _scanPauseGate?.IsPauseRequested == true
                    ? "Pausando…" : "Análisis en curso") :
                !ready ? "Requiere atención" : threat ? "Revisar alertas" :
                inconclusive ? "Repetir análisis" : "Listo para analizar";
            _dashboardHeroReadinessNote.Text = running ? "Puedes consultar el progreso actual." :
                !ready ? "Verifica el motor y las firmas." : threat ? "Consulta las detecciones registradas." :
                inconclusive ? "No hay resultado completo." : signatureHealth.NeedsAttention
                    ? "Verifica el estado de las firmas en Motor y firmas." :
                      "Análisis bajo demanda disponible.";
        }
        if (_dashboardMainAction is not null)
            _dashboardMainAction.Content = running ? "⌕  Ver análisis en curso" : "⌕  Iniciar análisis";
        _dashboardMoodBorder.Background = running ? BluePale : threat ? RedPale :
            inconclusive ? AmberPale : ready ? GreenPale : AmberPale;
        if (_dashboardMoodBorder.Child is Ellipse dot) dot.Fill = hue;
        _dashboardMoodText.Text = running ?
            (_scanPausedBetweenBatches && _scanPauseGate?.IsPauseRequested == true
                ? "Análisis pausado" : _scanPauseGate?.IsPauseRequested == true
                ? "Pausando después del lote" : "Análisis en curso") :
            threat ? "Revisar detecciones" :
            inconclusive ? "Último análisis no concluyente" :
            !ready ? "Motor o firmas pendientes" : "Motor listo para analizar";
        _dashboardMoodText.Foreground = hue;
        _dashboardMoodDetail.Text = running ? "Consulta el progreso y los resultados en Análisis de archivos." :
            threat ? "Se han comunicado detecciones; revisa los informes y la cuarentena." :
            inconclusive ? "No se ha obtenido un resultado completo. Puedes repetir el análisis." :
            !ready ? "Abre Motor y firmas para comprobar la configuración." :
            "Disponible para análisis bajo demanda. No implica protección preventiva.";
        _dashboardEngineState.Text = ClamAvLocator.IsValidScanner(_engineBox.Text)
            ? "Motor localizado" : "Motor no disponible";
        _dashboardEngineState.Foreground = ClamAvLocator.IsValidScanner(_engineBox.Text) ? Green : Red;
        _dashboardSignaturesState.Text = signatureHealth.State switch
        {
            SignatureHealthState.Missing => "Firmas pendientes",
            SignatureHealthState.UpdateFailed => "Revisar actualización",
            SignatureHealthState.Attention => "Revisar antigüedad",
            SignatureHealthState.Unknown => "Verificación pendiente",
            _ => "Base local disponible"
        };
        _dashboardSignaturesState.Foreground = signatureHealth.NeedsAttention ? Amber : Green;
        _dashboardSignaturesState.ToolTip = signatureHealth.Detail;
        var guardActive = _guardService?.IsRunning == true;
        var guardDegraded = _guardService?.IsDegraded == true;
        _dashboardGuardState.Text = guardDegraded ? "Vigilancia degradada · revisar" :
            _guardPaused ? "Procesamiento Guard pausado" :
            guardActive ? "Vigilancia reactiva activa" : "Vigilancia inactiva";
        _dashboardGuardState.Foreground = guardDegraded || _guardPaused ? Amber :
            guardActive ? Green : Muted;
        UpdateDashboardPeripheralStates();
        RefreshScanControlUx();
        _status.Foreground = running ? Blue : threat ? Red : inconclusive ? Amber :
            status.Contains("Sin detecciones", StringComparison.OrdinalIgnoreCase) ? Green : Ink;
        _detections.Foreground = _detections.Text != "—" && _detections.Text != "0" ? Red : Ink;
    }

    private void RefreshHistory()
    {
        if (_historyList is null) return;
        try { _historyList.ItemsSource = ScanHistory.ReadRecent(120); }
        catch (Exception ex) { AppendLog("No se pudo cargar historial: " + ex.Message); }
    }

    private static ListView CreateHistoryList()
    {
        var list = new ListView { Height = 420, Background = Surface,
            BorderBrush = Outline, BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 11, 0, 13), FontSize = 12 };
        var view = new GridView();
        view.Columns.Add(new GridViewColumn { Header = "Fecha", Width = 135,
            DisplayMemberBinding = new Binding(nameof(ScanRecord.CompletedAt)) { StringFormat = "dd/MM/yyyy HH:mm" } });
        view.Columns.Add(new GridViewColumn { Header = "Resultado", Width = 142,
            DisplayMemberBinding = new Binding(nameof(ScanRecord.Outcome)) });
        view.Columns.Add(new GridViewColumn { Header = "Detectados", Width = 75,
            DisplayMemberBinding = new Binding(nameof(ScanRecord.Detections)) });
        view.Columns.Add(new GridViewColumn { Header = "Tiempo (s)", Width = 85,
            DisplayMemberBinding = new Binding(nameof(ScanRecord.Seconds)) });
        view.Columns.Add(new GridViewColumn { Header = "Destino", Width = 375,
            DisplayMemberBinding = new Binding(nameof(ScanRecord.Target)) });
        list.View = view;
        return list;
    }

    private static void AddCounter(Grid grid, int index, string heading, TextBlock value)
    {
        var card = Card(Surface, Outline);
        card.Margin = new Thickness(index == 0 ? 0 : 6, 0, index == 2 ? 0 : 6, 0);
        var stack = new StackPanel();
        var header = new StackPanel { Orientation = Orientation.Horizontal };
        var color = heading.Contains("DETECC") ? Red :
            heading.Contains("TIEMPO") || heading.Contains("ANÁLISIS") ? Blue : Purple;
        header.Children.Add(T("●", 13, color, FontWeights.Bold, new Thickness(0, 0, 7, 0)));
        header.Children.Add(T(heading, 10, Muted, FontWeights.Bold));
        stack.Children.Add(header);
        value.Margin = new Thickness(0, 9, 0, 0);
        stack.Children.Add(value);
        card.Child = stack;
        Grid.SetColumn(card, index);
        grid.Children.Add(card);
    }

    private static TextBox Input(string hint) => new()
    {
        MinHeight = 34, Padding = new Thickness(10, 8, 10, 7),
        Background = B("#FCFCFE"), Foreground = Ink,
        BorderBrush = Outline, BorderThickness = new Thickness(1),
        ToolTip = hint, FontSize = 12,
        VerticalContentAlignment = VerticalAlignment.Center,
    };

    private static Button ActionButton(string label, bool primary, RoutedEventHandler click)
    {
        var button = new Button
        {
            Content = label, MinHeight = 37, Padding = new Thickness(15, 9, 15, 9),
            Margin = new Thickness(0, 0, 8, 6),
            Foreground = primary ? Brushes.White : Purple,
            Background = primary ? Purple : PurplePale,
            BorderBrush = primary ? Purple : B("#E1D9F5"), BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand, FontSize = 12, FontWeight = FontWeights.SemiBold,
        };
        RoundButton(button);
        button.Click += click;
        return button;
    }

    private void PickFile()
    {
        var dialog = new OpenFileDialog { Title = "Archivo para analizar", Filter = "Todos los archivos (*.*)|*.*", CheckFileExists = true };
        if (PalClam.UI.LocalizedDialogs.Show(dialog, this) == true) _targetBox.Text = dialog.FileName;
    }

    private void PickFolder()
    {
        var dialog = new OpenFolderDialog { Title = "Carpeta para analizar" };
        if (PalClam.UI.LocalizedDialogs.Show(dialog, this) == true) _targetBox.Text = dialog.FolderName;
    }

    private void PickEngine()
    {
        var dialog = new OpenFileDialog { Title = "Selecciona el clamscan.exe oficial de ClamAV", Filter = "ClamAV (clamscan.exe)|clamscan.exe" };
        if (PalClam.UI.LocalizedDialogs.Show(dialog, this) == true) _engineBox.Text = dialog.FileName;
    }

    private void RefreshEngineStatus()
    {
        if (_engineStatus is null || _engineBox is null) return;
        var ready = ClamAvLocator.IsValidScanner(_engineBox.Text.Trim());
        _engineStatus.Text = ready
            ? "✓  clamscan.exe localizado. Las firmas se comprobarán durante el análisis."
            : "⚠  Motor no instalado. PalClam puede descargarlo e instalarlo por ti.";
        _engineStatus.Foreground = ready ? B("#207454") : B("#966023");
    }

    private async Task RefreshSignatureMetadataAsync()
    {
        if (_readingSignatures || _signatureMetadata is null || _engineBox is null) return;
        _readingSignatures = true;
        try
        {
            var health = SignatureHealthService.Evaluate(SignatureStore.HasSignatures(),
                SignatureHealthService.DailyFileTimeUtc(SignatureStore.DatabaseDirectory),
                _settings.LastSignatureCheckUtc, _settings.LastSignatureAttemptUtc,
                _settings.LastSignatureUpdateError, DateTimeOffset.UtcNow);
            _signatureHealthText.Text = health.Title + ". " + health.Detail;
            _signatureHealthText.Foreground = health.NeedsAttention ? Amber : Green;
            if (!SignatureStore.HasSignatures())
            {
                _signatureMetadata.Text = "No hay bases main y daily completas. El análisis no está disponible.";
                return;
            }
            _signatureMetadata.Text = "Leyendo versiones y firmas locales...";
            var scanner = _engineBox.Text.Trim();
            var databases = await SignatureInfoService.ReadAsync(scanner, CancellationToken.None);
            var detail = string.Join("  ·  ", databases.Select(db =>
                db.Name + " v" + db.Version + " (" +
                (db.Signatures is null ? "firmas: no disponibles" : db.Signatures.Value.ToString("N0") + " firmas") +
                ", fichero: " + db.LastModifiedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm") + ")"));
            _signatureMetadata.Text = "Bases locales: " + detail + "\n" +
                "Última comprobación de FreshClam: " +
                (_settings.LastSignatureCheckUtc == DateTimeOffset.MinValue ? "sin registro" :
                    _settings.LastSignatureCheckUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
        }
        catch (Exception ex)
        {
            _signatureMetadata.Text = "No se pudieron consultar los metadatos: " + ex.Message;
            _signatureHealthText.Text = "La disponibilidad y vigencia de las firmas no se pudo verificar localmente.";
            _signatureHealthText.Foreground = Amber;
        }
        finally { _readingSignatures = false; }
    }

    private void SaveUpdatePreferences()
    {
        // Evita perder la configuración de la ruta del motor cuando se cambia una preferencia.
        _settings.CheckAppUpdatesOnStartup = _checkUpdatesOnStartup.IsChecked == true;
        _settings.AutoInstallAppUpdates = _installUpdatesAutomatically.IsChecked == true;
        _settings.AutoUpdateSignatures = _updateSignaturesAutomatically.IsChecked == true;
        _settings.AutoUpdateEngine = _autoEngine.IsChecked == true;
        _settings.UpdateIntervalHours = _checkIntervalOption.SelectedIndex switch { 0 => 6, 2 => 24, _ => 12 };
        SaveSettings();
        if (_settings.AutoInstallAppUpdates && _availableRelease is not null && !_busy)
            _ = InstallAvailableUpdateAsync();
    }

    private async Task CheckAppUpdatesAsync(bool manual)
    {
        if (_checkingAppUpdates || _installingAppUpdate) return;
        _checkingAppUpdates = true;
        _checkAppUpdatesButton.IsEnabled = false;
        _appUpdateCancellation = new CancellationTokenSource();
        try
        {
            _appUpdateStatus.Text = "Comprobando canal oficial de PalClam...";
            var release = await AppUpdateService.CheckAsync(_appUpdateCancellation.Token);
            _availableRelease = release;
            _installAppUpdateButton.IsEnabled = release is not null;
            if (release is null)
            {
                _appUpdateStatus.Text = "No hay una actualización publicada más reciente (o el canal aún no se ha publicado).";
                if (manual) AppendLog("[ACTUALIZACIONES] No hay una versión posterior publicada.");
                return;
            }
            _appUpdateStatus.Text = $"Nueva versión {release.Version} disponible. " +
                (string.IsNullOrWhiteSpace(release.Notes) ? "" : release.Notes);
            AppendLog("[ACTUALIZACIONES] Nueva versión disponible: " + release.Version);
            if (_settings.AutoInstallAppUpdates)
            {
                if (_busy)
                {
                    _pendingAutomaticInstall = true;
                    PalClam.UI.PresentationLocalization.Append(_appUpdateStatus, " Se instalará al finalizar la operación actual.");
                }
                else await InstallAvailableUpdateAsync();
            }
        }
        catch (OperationCanceledException) { /* Ventana cerrada */ }
        catch (HttpRequestException ex)
        {
            _appUpdateStatus.Text = "No se pudo contactar con el canal de actualizaciones.";
            if (manual) MessageBox.Show(this, "No se pudo comprobar la actualización: " + ex.Message,
                "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _appUpdateStatus.Text = "Actualización no disponible o no válida. " + ex.Message;
            AppendLog("[ACTUALIZACIONES] Rechazado: " + ex.Message);
            if (manual) MessageBox.Show(this, "No se ha instalado ningún archivo. " + ex.Message,
                "Verificación de actualizaciones", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _checkingAppUpdates = false;
            _checkAppUpdatesButton.IsEnabled = true;
            _appUpdateCancellation?.Dispose();
            _appUpdateCancellation = null;
        }
    }

    private async Task InstallAvailableUpdateAsync()
    {
        if (AppPaths.IsPortable) { _appUpdateStatus.Text = "En modo portable, actualiza PalClam sustituyendo su carpeta con el ZIP de la nueva versión."; return; }
        if (_installingAppUpdate || _availableRelease is null) return;
        if (_busy)
        {
            _appUpdateStatus.Text = "Hay un análisis en curso. Espera a que finalice antes de actualizar.";
            _pendingAutomaticInstall = _settings.AutoInstallAppUpdates;
            return;
        }
        _installingAppUpdate = true;
        _quickButton.IsEnabled = false;
        _profileButton.IsEnabled = false;
        _selectedButton.IsEnabled = false;
        _updateButton.IsEnabled = false;
        _installAppUpdateButton.IsEnabled = false;
        _checkAppUpdatesButton.IsEnabled = false;
        _appUpdateCancellation ??= new CancellationTokenSource();
        try
        {
            var release = _availableRelease;
            _appUpdateStatus.Text = "Descargando versión " + release.Version + "...";
            _appDownloadProgress.Visibility = Visibility.Visible;
            _appDownloadProgress.Value = 0;
            var progress = new Progress<double>(p =>
            {
                _appDownloadProgress.Value = Math.Clamp(p, 0, 100);
                _appUpdateStatus.Text = $"Descargando PalClam {release.Version}: {p:0}%";
            });
            var installer = await AppUpdateService.DownloadVerifiedInstallerAsync(
                release, progress, _appUpdateCancellation.Token);
            // Comprobar de nuevo tras la descarga y antes del lanzamiento.
            if (!await AppUpdateService.HashMatchesAsync(installer, release.Sha256, _appUpdateCancellation.Token))
                throw new CryptographicException("Hash del instalador no válido.");
            _appUpdateStatus.Text = "Instalador verificado. PalClam se cerrará para actualizarse.";
            AppendLog("[ACTUALIZACIONES] SHA-256 verificado, iniciando instalador externo.");
            UpdateLauncher.LaunchInstallerAfterExit(installer, release.Sha256);
            // Forzar cierre real: OnClosing no debe minimizar a la bandeja
            // mientras el actualizador externo espera a que termine PalClam.
            _trayExitRequested = true;
            Application.Current.Shutdown();
        }
        catch (OperationCanceledException) { _appUpdateStatus.Text = "Actualización cancelada."; }
        catch (Exception ex)
        {
            _appUpdateStatus.Text = "Actualización no instalada: " + ex.Message;
            AppendLog("[ACTUALIZACIONES] Error: " + ex.Message);
            MessageBox.Show(this, ex.Message, "No se ha podido actualizar", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _installingAppUpdate = false;
            _quickButton.IsEnabled = !_busy;
            _profileButton.IsEnabled = !_busy;
            _selectedButton.IsEnabled = !_busy;
            _updateButton.IsEnabled = !_busy;
            _engineUpdateButton.IsEnabled = !_busy;
            _appDownloadProgress.Visibility = Visibility.Collapsed;
            _installAppUpdateButton.IsEnabled = _availableRelease is not null;
            _checkAppUpdatesButton.IsEnabled = true;
            _appUpdateCancellation?.Dispose();
            _appUpdateCancellation = null;
        }
    }

    private async Task CheckAndInstallEngineAsync(bool manual)
    {
        if (_checkingEngine || _busy || _installingAppUpdate) return;
        if (!manual && !_settings.AutoUpdateEngine) return;
        _checkingEngine = true;
        _engineUpdateButton.IsEnabled = false;
        var previousScanner = _engineBox.Text.Trim();
        var missing = !ClamAvLocator.IsValidScanner(previousScanner);
        try
        {
            _engineUpdateStatus.Text = "Consultando última versión estable de ClamAV en GitHub oficial...";
            AppendLog("[MOTOR] Comprobando nueva versión oficial Cisco-Talos/clamav...");
            using var checkCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var release = await ClamAvEngineManager.CheckLatestAsync(checkCancellation.Token);
            if (release is null) return;
            _settings.LastEngineCheckUtc = DateTimeOffset.UtcNow;
            SaveSettings();
            var installed = ClamAvLocator.EffectiveManagedVersion;
            // No reemplazar un motor externo: instalamos siempre bajo el perfil de PalClam.
            var shouldInstall = installed is null || release.Version > installed;
            if (!shouldInstall)
            {
                _engineUpdateStatus.Text = $"Motor actualizado: ClamAV {installed}.";
                if (manual) AppendLog("[MOTOR] El motor ya está actualizado.");
                return;
            }
            _engineUpdateStatus.Text = missing
                ? $"Falta el motor. Instalando ClamAV {release.Version} desde el programa..."
                : $"Actualización de ClamAV {release.Version} disponible.";
            if (!manual && !_settings.AutoUpdateEngine) return;
            // El usuario puede haber empezado un análisis mientras consultábamos GitHub.
            // Nunca iniciar una sustitución de motor con un análisis ya en marcha.
            if (_busy || _installingAppUpdate)
            {
                _engineUpdateStatus.Text = "Actualización aplazada: hay una operación en curso.";
                return;
            }

            _running = new CancellationTokenSource();
            SetBusy(true); // Nunca sustituir el motor mientras se escanea.
            _engineDownloadProgress.Visibility = Visibility.Visible;
            _engineDownloadProgress.Value = 0;
            try
            {
                var progress = new Progress<double>(p =>
                {
                    _engineDownloadProgress.Value = Math.Clamp(p, 0, 100);
                    _engineUpdateStatus.Text = $"Preparando ClamAV {release.Version}: {p:0}%";
                });
                var scanner = await ClamAvEngineManager.DownloadAndActivateAsync(release, progress, _running.Token);
                _engineBox.Text = scanner;
                RefreshEngineStatus();
                SaveSettings();
                _engineUpdateStatus.Text = $"ClamAV {release.Version} instalado y verificado. Listo para actualizar firmas.";
                AppendLog($"[MOTOR] ClamAV {release.Version} instalado. SHA-256 verificado. Motor probado con --version.");
            }
            finally
            {
                _engineDownloadProgress.Visibility = Visibility.Collapsed;
                SetBusy(false);
                _running?.Dispose();
                _running = null;
            }
        }
        catch (OperationCanceledException)
        {
            _engineUpdateStatus.Text = "Comprobación o instalación cancelada. Se mantiene el motor anterior.";
            AppendLog("[MOTOR] Operación cancelada.");
        }
        catch (Exception ex)
        {
            _engineUpdateStatus.Text = ClamAvLocator.IsValidScanner(_engineBox.Text)
                ? "No se pudo consultar la actualización. El motor instalado sigue disponible, también sin Internet."
                : "No se pudo consultar la actualización y no hay motor disponible.";
            AppendLog("[MOTOR] " + ex.Message);
            if (manual) MessageBox.Show(this, ex.Message, "Actualización de ClamAV", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _checkingEngine = false;
            _engineUpdateButton.IsEnabled = !_busy && !_installingAppUpdate;
        }
    }

    private void SaveSettings()
    {
        try
        {
            _settings.ClamScanPath = _engineBox.Text.Trim();
            _settings.LastTarget = _targetBox.Text.Trim();
            SettingsStore.Save(_settings);
        }
        catch (Exception ex) { AppendLog("Aviso: no se pudo guardar la configuración: " + ex.Message); }
    }

    private bool TryGetScanner(out string scanner)
    {
        scanner = _engineBox.Text.Trim();
        if (ClamAvLocator.IsValidScanner(scanner)) return true;
        MessageBox.Show(this, "No se encuentra ClamAV. Pulsa 'Comprobar / instalar motor ClamAV' para descargarlo desde PalClam.",
            "Motor no configurado", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        RefreshDashboardUi();
        _quickButton.IsEnabled = !busy;
        _profileButton.IsEnabled = !busy;
        _selectedButton.IsEnabled = !busy;
        _updateButton.IsEnabled = !busy;
        _engineUpdateButton.IsEnabled = !busy && !_checkingEngine;
        RefreshStopControls();
        // La barra fija acompaña al usuario incluso si navega al panel principal.
        if (_pageHost?.Content is StackPanel page)
            _scanActionDock.Visibility = busy && _scanRunning || ReferenceEquals(page, _pages["scan"])
                ? Visibility.Visible : Visibility.Collapsed;
        _historyButton.IsEnabled = !busy;
        RefreshInspectorActions();
        if (!busy && _pendingAutomaticInstall && _availableRelease is not null && !_installingAppUpdate)
        {
            _pendingAutomaticInstall = false;
            _ = InstallAvailableUpdateAsync();
        }
    }

    private void RequestStopCurrentOperation()
    {
        // El proceso puede tardar en confirmar su cancelación: no simular que ya paró.
        var cancellation = _running;
        if (cancellation is null || cancellation.IsCancellationRequested) return;
        try { cancellation.Cancel(); }
        catch (ObjectDisposedException) { return; }
        RefreshStopControls();
        if (_scanRunning)
        {
            _scanStage.Text = "Deteniendo análisis… esperando confirmación del motor";
            _scanStage.Foreground = Amber;
        }
    }

    private void RefreshStopControls()
    {
        if (_cancelButton is null || _scanActionHint is null || _retryScanButton is null) return;
        var canStop = _busy && _running is not null && !_running.IsCancellationRequested;
        var pendingStop = _busy && _running is not null && _running.IsCancellationRequested;
        _cancelButton.Visibility = _busy && _running is not null ? Visibility.Visible : Visibility.Collapsed;
        _cancelButton.IsEnabled = canStop;
        _cancelButton.Content = pendingStop ? "◷  Deteniendo…" :
            _scanRunning ? "■  Detener análisis" : "■  Cancelar operación";
        SetSemanticButton(_cancelButton, pendingStop ? "warn" : "danger");
        if (_topScanStopButton is not null)
        {
            _topScanStopButton.Visibility = _scanRunning && _busy ? Visibility.Visible : Visibility.Collapsed;
            _topScanStopButton.IsEnabled = canStop;
            _topScanStopButton.Content = pendingStop ? "◷  Deteniendo…" : "■  Detener análisis";
            SetSemanticButton(_topScanStopButton, pendingStop ? "warn" : "danger");
        }

        _scanActionHint.Visibility = Visibility.Visible;
        if (_scanRunning && _busy)
            _scanActionHint.Text = "● " + ScanControlStateText();
        else _scanActionHint.Text = !_scanEverStarted
            ? "●  Sin análisis en curso. Puedes iniciar uno cuando quieras."
            : _scanFinishedNormally
                ? "✓  Análisis finalizado. Inicia otro análisis cuando lo necesites."
                : "●  Análisis interrumpido o incompleto. Quedan archivos sin verificar.";
        _scanActionHint.Foreground = !_scanEverStarted ? Muted : _scanFinishedNormally ? Green : Amber;
        _retryScanButton.Visibility = !_busy && _scanEverStarted && !_scanFinishedNormally
            ? Visibility.Visible : Visibility.Collapsed;
        _retryScanButton.IsEnabled = !_busy && (_lastScanPlan is not null || !string.IsNullOrWhiteSpace(_targetBox.Text));
        RefreshScanControlUx();
    }

    private async Task StartScanAsync(string selectedTarget)
    {
        try
        {
            var plan = SmartScanPlanPolicy.Create(SmartScanPreset.Custom,
                new[] { selectedTarget }, _settings.ScanPriority, _settings.ScanControlMode);
            await StartScanPlanAsync(plan);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            MessageBox.Show(this, "Elige un archivo o carpeta válido.", "Ruta incorrecta",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            Navigate("scan");
        }
    }

    private async Task StartScanPlanAsync(SmartScanPlan plan)
    {
        if (_busy || _installingAppUpdate || !TryGetScanner(out var scanner)) return;
        if (!SignatureStore.HasSignatures())
        {
            MessageBox.Show(this, "No hay firmas descargadas en la carpeta local de PalClam.\n" +
                "Abre 'Motor y firmas' y pulsa 'Actualizar firmas' antes de analizar.",
                "Firmas pendientes", MessageBoxButton.OK, MessageBoxImage.Warning);
            Navigate("signatures");
            return;
        }
        var excluded = ScanExclusions.Normalize(_settings.ExcludedPaths);
        var targets = plan.Targets.Where(path => !ScanExclusions.IsExcluded(path, excluded) &&
            (Directory.Exists(path) || File.Exists(path))).ToArray();
        if (targets.Length == 0)
        {
            MessageBox.Show(this, "No quedan objetivos accesibles tras aplicar las exclusiones. " +
                "Comprueba las rutas y la configuración antes de iniciar.",
                "Smart Scan · Sin objetivos", MessageBoxButton.OK, MessageBoxImage.Warning);
            Navigate("scan");
            return;
        }
        if (targets.Length != plan.Targets.Count)
        {
            var answer = MessageBox.Show(this, $"Hay {plan.Targets.Count - targets.Length} ubicaciones " +
                "no accesibles o excluidas. El análisis sería parcial. ¿Continuar?",
                "Smart Scan · Cobertura parcial", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;
        }
        var target = targets.Length == 1 ? targets[0] :
            plan.Preset + ": " + string.Join(" | ", targets);
        if (plan.Preset == SmartScanPreset.Custom && targets.Length == 1)
            _targetBox.Text = target;
        _lastScanPlan = plan with { Targets = targets, DisplayTarget = target };
        _scanEngineVersion = ClamAvLocator.EffectiveManagedVersion?.ToString() ?? "no verificable";
        _scanSignatureSnapshot = ScanComparisonService.SignatureSnapshot(SignatureStore.DatabaseDirectory);
        _activeScanControlMode = plan.BatchSize == 1 ? "Preciso" : "Equilibrado";
        _scanConfigurationId = "SmartScan-" + plan.Preset + ":batch=" + plan.BatchSize + ":" +
            ScanComparisonService.ConfigurationId(_settings.ScanArchives,
                _settings.DetectPua, plan.Priority,
                ScanExclusions.Normalize(_settings.ExcludedPaths));
        SaveSettings();
        _detections.Text = "—";
        _status.Text = "Analizando...";
        _status.Foreground = Purple;
        _lastAnalysis.Text = DateTime.Now.ToString("dd/MM HH:mm");
        BeginScanTelemetry(target, targets);
        _activeScanPriority = plan.Priority;
        _scanPauseGate = new ScanPauseGate();
        AppendLog("");
        AppendLog("[SMART SCAN] Perfil: " + plan.Preset + " · " + plan.Guidance);
        AppendLog("[ANÁLISIS] " + target);
        AppendLog("Objetivos: " + targets.Length + " · prioridad: " + plan.Priority +
            " · lote: " + plan.BatchSize + " archivos.");
        AppendLog("Motor: " + scanner);
        AppendLog("Pausa cooperativa: espera al final del lote, sin suspender ClamAV.");
        AppendLog(plan.BatchSize == 1
            ? "Modo preciso: un archivo por lote; recarga firmas y puede ser muy lento."
            : $"Modo por lotes: hasta {plan.BatchSize} archivos; ruta exacta desconocida mientras el lote sigue activo.");
        AppendLog("ClamAV informa resultados por archivo al terminarlos; el progreso es orientativo.");
        AppendLog("No se eliminará ni moverá ningún archivo.");

        var foundLines = new ConcurrentQueue<string>();
        int? resultCode = null;
        var outcome = "Incompleto";
        var finalStage = "Análisis incompleto";
        ScanLogWriter? detailedLog = null;
        ScanVerdict? scanVerdict = null;
        try
        {
            try
            {
                detailedLog = new ScanLogWriter(target);
                _scanLogPath = detailedLog.FilePath;
            }
            catch (Exception ex)
            {
                AppendLog("[AVISO] No se puede guardar informe completo: " + ex.Message);
            }

            _running = new CancellationTokenSource();
            SetBusy(true);
            // No usar --infected: oculta los archivos normales y hace imposible el
            // seguimiento basado en las respuestas del propio clamscan.
            var args = new List<string> { "--stdout", "--verbose",
                "--database=" + SignatureStore.DatabaseDirectory,
                "--scan-archive=" + (_settings.ScanArchives ? "yes" : "no") };
            if (_settings.DetectPua) args.Add("--detect-pua=yes");
            var exclusions = ScanExclusions.Normalize(_settings.ExcludedPaths);
            args.AddRange(ScanExclusions.BuildClamArguments(exclusions));
            if (exclusions.Count > 0) AppendLog($"[OPCIONES] Exclusiones activas: {exclusions.Count}. No se analizarán esas rutas.");
            var currentScanGeneration = _scanGeneration;
            var result = await ControlledScanRunner.RunAsync(scanner, args,
                targets, exclusions, plan.BatchSize,
                _scanPauseGate!, batch => ReceiveScanBatch(currentScanGeneration, batch), (line, isError) =>
            {
                try { detailedLog?.WriteLine(line, isError); }
                catch { /* Un fallo del informe no puede detener el escaneo. */ }
                var item = ScanOutputParser.ParseFile(line);
                if (item is not null)
                {
                    Interlocked.Exchange(ref _scanLastResultElapsedTicks,
                        _scanClock?.Elapsed.Ticks ?? 0);
                    if (_activeScanMetrics)
                        Interlocked.Add(ref _scanLogicalBytes,
                            ScanPerformanceMath.TryGetLogicalFileBytes(item.FilePath));
                    Interlocked.Increment(ref _filesReported);
                    if (item.Status == ScanFileStatus.Detected)
                    {
                        Interlocked.Increment(ref _threatsReported);
                        if (foundLines.Count < 250) foundLines.Enqueue($"{item.FilePath}: {item.Detail} FOUND");
                    }
                    else if (item.Status == ScanFileStatus.Error)
                        Interlocked.Increment(ref _errorsReported);
                    // En análisis enormes, limitar la cola de resultados normales.
                    // Las detecciones y errores nunca se descartan de esta cola.
                    var pending = Interlocked.Increment(ref _pendingResultCount);
                    if (pending <= 2500 || item.Status != ScanFileStatus.Clean)
                        _pendingResults.Enqueue(item);
                    else Interlocked.Decrement(ref _pendingResultCount);
                }
                else if (ScanOutputParser.TryReadCount(line, "Scanned files", out var scanned))
                    Interlocked.Exchange(ref _engineSummaryScanned, scanned);
                else if (ScanOutputParser.TryReadCount(line, "Infected files", out var infected))
                    Interlocked.Exchange(ref _engineSummaryInfected, infected);
                else if (ScanOutputParser.TryReadCount(line, "Total errors", out var totalErrors))
                {
                    long old;
                    do { old = Interlocked.Read(ref _errorsReported); }
                    while (totalErrors > old && Interlocked.CompareExchange(ref _errorsReported, totalErrors, old) != old);
                }
                else if (isError || line.Contains("ERROR:", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("WARNING:", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Scanned files:", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Infected files:", StringComparison.OrdinalIgnoreCase))
                {
                    if (line.Contains("ERROR:", StringComparison.OrdinalIgnoreCase))
                        Interlocked.Increment(ref _errorsReported);
                    _pendingMessages.Enqueue((isError ? "[STDERR] " : "") + line);
                }
            }, _running.Token,
                reportMetrics: _activeScanMetrics ? new Action<EngineProcessSample>(sample =>
                {
                    Volatile.Write(ref _engineLiveSample, sample);
                    _scanPeakMemory = Math.Max(_scanPeakMemory, sample.PeakWorkingSetBytes);
                    _scanCpuPeak = Math.Max(_scanCpuPeak, sample.MachineCpuPercent);
                    _scanCpuSum += sample.MachineCpuPercent;
                    _scanCpuSamples++;
                }) : null,
                priority: _activeScanPriority switch
                {
                    "Económico" => ProcessPriorityClass.BelowNormal,
                    "Prioritario" => ProcessPriorityClass.AboveNormal,
                    _ => ProcessPriorityClass.Normal
                });

            resultCode = result.ExitCode;
            Interlocked.Exchange(ref _engineSummaryScanned, result.ScannedFiles);
            Interlocked.Exchange(ref _engineSummaryInfected, result.InfectedFiles);
            if (result.TotalErrors > 0)
            {
                var current = Interlocked.Read(ref _errorsReported);
                if (result.TotalErrors > current)
                    Interlocked.Exchange(ref _errorsReported, result.TotalErrors);
            }
            if (result.FilesSubmitted > 0 && result.ScannedFiles == 0)
            {
                AppendLog("[COBERTURA] ClamAV no ha confirmado ningún archivo analizado.");
                Interlocked.Increment(ref _errorsReported);
            }
            if (result.TraversalErrors > 0)
            {
                AppendLog($"[COBERTURA] {result.TraversalErrors:N0} rutas inaccesibles al enumerar; " +
                    "el resultado no puede clasificarse como limpio.");
                Interlocked.Add(ref _errorsReported, result.TraversalErrors);
            }
            if (result.FilesSubmitted == 0)
            {
                AppendLog("[AVISO] No se ha enviado ningún archivo a ClamAV.");
                Interlocked.Increment(ref _errorsReported);
            }
            if (!result.FinishedEnumeration)
            {
                AppendLog("[AVISO] La enumeración no terminó; cobertura incompleta.");
                Interlocked.Increment(ref _errorsReported);
            }
            var threats = Interlocked.Read(ref _threatsReported);
            var errors = Interlocked.Read(ref _errorsReported);
            var engineInfected = Interlocked.Read(ref _engineSummaryInfected);
            scanVerdict = ScanVerdictPolicy.Evaluate(result.ExitCode, threats, errors, engineInfected);
            var anyThreat = scanVerdict.HasThreats;
            if (scanVerdict.Status == ScanVerdictStatus.Clean)
            {
                outcome = "Sin detecciones";
                finalStage = "Análisis finalizado: sin detecciones notificadas";
                _status.Text = "Sin detecciones";
                _status.Foreground = B("#207454");
                _detections.Text = "0";
                AppendLog("[OK] ClamAV finalizó sin detectar amenazas. El análisis tiene los límites del motor.");
            }
            else if (scanVerdict.Status == ScanVerdictStatus.Detected)
            {
                outcome = "Detecciones";
                finalStage = "Análisis finalizado: amenazas detectadas";
                _status.Text = "Amenazas detectadas";
                MarkThreatForTray();
                _status.Foreground = B("#B44A37");
                _detections.Text = scanVerdict.Detections.ToString();
                AppendLog($"[ATENCIÓN] Detecciones informadas: {scanVerdict.Detections}. Revisa el detalle.");
            }
            else
            {
                outcome = anyThreat ? "Detecciones e incidencias" : "Incompleto / errores";
                finalStage = "Análisis finalizado con errores: resultado no concluyente";
                _status.Text = "Incompleto";
                _status.Foreground = B("#966023");
                _detections.Text = anyThreat ? scanVerdict.Detections.ToString() : "—";
                AppendLog($"[AVISO] Código ClamAV: {result.ExitCode}; errores de archivos: {errors}. " +
                    "No se debe considerar un veredicto limpio.");
            }
        }
        catch (OperationCanceledException)
        {
            outcome = "Cancelado";
            finalStage = "Cancelado: quedan archivos sin verificar";
            _status.Text = "Cancelado";
            _status.Foreground = B("#966023");
            _detections.Text = Interlocked.Read(ref _threatsReported) > 0
                ? Interlocked.Read(ref _threatsReported).ToString() : "—";
            AppendLog("[CANCELADO] No hay veredicto sobre los archivos no examinados.");
        }
        catch (Exception ex)
        {
            outcome = "Error de ejecución";
            finalStage = "Fallo durante el análisis: no concluyente";
            _status.Text = "Error de análisis";
            _status.Foreground = B("#B44A37");
            _detections.Text = "—";
            AppendLog("[ERROR] " + ex.Message);
        }
        finally
        {
            try { detailedLog?.WriteLine($"[PALCLAM] Fin: {outcome}; código: {resultCode?.ToString() ?? "sin código"}"); }
            catch { }
            try { detailedLog?.Dispose(); } catch { }
            FinishScanTelemetry(finalStage, outcome);
            var elapsed = _scanClock?.Elapsed ?? TimeSpan.Zero;
            var logicalBytes = Interlocked.Read(ref _scanLogicalBytes);
            if (_activeScanMetrics)
            {
                AppendLog($"[RENDIMIENTO] Resultados/s (promedio): " +
                    $"{ScanPerformanceMath.Rate(Interlocked.Read(ref _filesReported), elapsed.TotalSeconds):0.0}; " +
                    $"volumen lógico completado: {logicalBytes / 1048576.0:0.0} MiB " +
                    $"(no equivale a bytes físicos leídos).");
                AppendLog(_scanCpuSamples > 0
                    ? $"[RECURSOS] CPU media motor: {_scanCpuSum / _scanCpuSamples:0.0}% del equipo; " +
                      $"pico RAM: {_scanPeakMemory / 1048576.0:0} MiB. " +
                      $"Intervalo máximo sin resultados: {_longestSilence:0}s."
                    : $"[RECURSOS] Sin muestras de CPU/RAM (análisis demasiado corto o acceso denegado). " +
                      $"Silencio máximo: {_longestSilence:0}s.");
            }
            try
            {
                ScanHistory.Append(new ScanRecord
                {
                    CompletedAt = DateTimeOffset.Now,
                    Target = target,
                    Outcome = outcome,
                    ExitCode = resultCode,
                    Detections = (int)Math.Clamp(scanVerdict?.Detections ??
                        Math.Max(Interlocked.Read(ref _threatsReported), Interlocked.Read(ref _engineSummaryInfected)),
                        0, int.MaxValue),
                    Errors = (int)Math.Clamp(Interlocked.Read(ref _errorsReported), 0, int.MaxValue),
                    FilesReported = Interlocked.Read(ref _filesReported),
                    EngineScannedFiles = Interlocked.Read(ref _engineSummaryScanned),
                    EstimatedFiles = _estimateCount,
                    LogPath = _scanLogPath,
                    Seconds = Math.Max(0, (long)elapsed.TotalSeconds),
                    MetricsEnabled = _activeScanMetrics,
                    LogicalBytesReported = logicalBytes,
                    PeakEngineMemoryBytes = _scanPeakMemory,
                    AverageEngineCpuPercent = _scanCpuSamples > 0 ? _scanCpuSum / _scanCpuSamples : 0,
                    PeakEngineCpuPercent = _scanCpuPeak,
                    LongestSilenceSeconds = _longestSilence,
                    ScanPriority = _activeScanPriority,
                    EngineMode = plan.BatchSize == 1 ? "clamscan-file" : "clamscan-batch",
                    EngineVersion = _scanEngineVersion,
                    SignatureSnapshot = _scanSignatureSnapshot,
                    ScanConfigurationId = _scanConfigurationId,
                    PerformanceSampleCount = _livePerformance.Samples.Count,
                    MeanFilesPerSecond = ScanPerformanceMath.Rate(
                        Interlocked.Read(ref _filesReported), Math.Max(0.001, elapsed.TotalSeconds)),
                    MeanLogicalMiBPerSecond = ScanPerformanceMath.Rate(
                        logicalBytes, Math.Max(0.001, elapsed.TotalSeconds)) / 1048576.0,
                    Threats = foundLines.ToArray()
                });
                RefreshHistory();
            }
            catch (Exception ex) { AppendLog("No se pudo guardar historial: " + ex.Message); }
            SetBusy(false);
            if (_settings.NotifyOnScanEnd)
            {
                var warn = outcome != "Sin detecciones";
                _desktopNotifications?.Show(warn ? "PalClam: revisar análisis" : "PalClam: análisis terminado",
                    warn ? $"Resultado: {outcome}. Consulta los detalles en PalClam." :
                    "Análisis finalizado sin detecciones notificadas. No equivale a protección completa.", warn);
            }
            _scanPauseGate?.Resume();
            _scanPauseGate = null;
            _scanPausedBetweenBatches = false;
            _running?.Dispose();
            _running = null;
            RefreshScanControlUx();
        }
    }

    private void LoadLastScan()
    {
        try
        {
            var last = ScanHistory.ReadRecent(1).FirstOrDefault();
            if (last is null) return;
            _lastAnalysis.Text = last.CompletedAt.ToLocalTime().ToString("dd/MM HH:mm");
            _status.Text = last.Outcome;
            _detections.Text = last.Outcome.StartsWith("Detecciones", StringComparison.Ordinal) ?
                Math.Max(1, last.Detections).ToString() :
                last.Outcome == "Sin detecciones" ? "0" : "—";
        }
        catch { /* El historial no debe impedir iniciar PalClam */ }
    }

    private async Task UpdateSignaturesAsync()
    {
        if (_busy || _installingAppUpdate || !TryGetScanner(out var scanner)) return;
        var updater = ClamAvLocator.FreshClamFor(scanner);
        if (!File.Exists(updater))
        {
            _settings.LastSignatureAttemptUtc = DateTimeOffset.UtcNow;
            _settings.LastSignatureUpdateError = "No se encontró freshclam.exe junto al motor";
            SaveSettings();
            await RefreshSignatureMetadataAsync();
            MessageBox.Show(this, "No se ha encontrado freshclam.exe junto al motor seleccionado. Repara la instalación de PalClam o actualiza su motor desde la interfaz.",
                "Actualizador ausente", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        SaveSettings();
        string configPath;
        try { configPath = SignatureStore.PrepareFreshClamConfig(); }
        catch (Exception ex)
        {
            _settings.LastSignatureAttemptUtc = DateTimeOffset.UtcNow;
            _settings.LastSignatureUpdateError = "Configuración de FreshClam: " + ex.GetType().Name;
            SaveSettings();
            await RefreshSignatureMetadataAsync();
            MessageBox.Show(this, "No se pudo preparar el directorio local de firmas: " + ex.Message,
                "Error de configuración", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        AppendLog("");
        AppendLog("[ACTUALIZACIÓN] Iniciando FreshClam oficial...");
        AppendLog("Configuración: " + configPath);
        AppendLog("Las firmas se actualizarán en: " + SignatureStore.DatabaseDirectory);
        _running = new CancellationTokenSource();
        _settings.LastSignatureAttemptUtc = DateTimeOffset.UtcNow;
        SaveSettings();
        SetBusy(true);
        try
        {
            var result = await ProcessRunner.RunAsync(updater, new[] { "--config-file=" + configPath },
                (line, isError) => WriteFromBackground((isError ? "[ERROR] " : "") + line), _running.Token);
            if (result.ExitCode == 0 && SignatureStore.HasSignatures())
            {
                _settings.LastSignatureCheckUtc = DateTimeOffset.UtcNow;
                _settings.LastSignatureUpdateError = "";
            }
            else
                _settings.LastSignatureUpdateError = $"FreshClam devolvió código {result.ExitCode}";
            SaveSettings();
            AppendLog(result.ExitCode == 0 && SignatureStore.HasSignatures()
                ? "[OK] FreshClam terminó correctamente y hay firmas disponibles para analizar."
                : $"[AVISO] FreshClam devolvió código {result.ExitCode}. Firmas disponibles: {(SignatureStore.HasSignatures() ? "sí" : "no")}. Comprueba el detalle de la consola.");
            await RefreshSignatureMetadataAsync();
            await RefreshSignatureCenterAsync();
        }
        catch (OperationCanceledException)
        {
            _settings.LastSignatureUpdateError = "Actualización cancelada";
            SaveSettings();
            AppendLog("[CANCELADO] Actualización interrumpida.");
        }
        catch (Exception ex)
        {
            _settings.LastSignatureUpdateError = ex.GetType().Name + ": " +
                (ex.Message.Length > 125 ? ex.Message[..125] : ex.Message);
            SaveSettings();
            AppendLog("[ERROR] No fue posible actualizar las firmas: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
            _running?.Dispose();
            _running = null;
            await RefreshSignatureMetadataAsync();
        }
    }

    private void WriteFromBackground(string line)
    {
        if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
            Dispatcher.BeginInvoke(new Action(() => AppendLog(line)));
    }

    private void AppendLog(string message)
    {
        if (_console is null) return;
        if (_console.Text.Length > 90000) _console.Text = _console.Text[^60000..];
        _console.AppendText($"[{DateTime.Now:HH:mm:ss}] {PalClam.Localization.DisplayText.Translate(message)}{Environment.NewLine}");
        _console.ScrollToEnd();
    }

    protected override void OnClosed(EventArgs e)
    {
        try { _running?.Cancel(); } catch (ObjectDisposedException) { }
        try { _appUpdateCancellation?.Cancel(); } catch (ObjectDisposedException) { }
        StopGuardOnClose();
        StopRansomware();
        try { _mailCancellation?.Cancel(); } catch (ObjectDisposedException) { }
        try { _outlookCancellation?.Cancel(); } catch (ObjectDisposedException) { }
        _outlookClient.Dispose();
        DisposeTray();
        base.OnClosed(e);
    }
}
