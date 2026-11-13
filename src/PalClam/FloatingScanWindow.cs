// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace PalClam;

/// <summary>
/// Monitor flotante de análisis: muestra exclusivamente métricas reales o estimadas
/// identificadas como tales. No ejecuta el motor ni modifica la política de detección.
/// Ocultar la ventana jamás detiene un análisis en curso.
/// </summary>
internal sealed class FloatingScanWindow : Window
{
    private readonly Border _card;
    private readonly Border _statusChip;
    private readonly Border _iconShell;
    private readonly Border _statsPanel;
    private readonly TextBlock _brandLabel;
    private readonly TextBlock _brandTitle;
    private readonly TextBlock _progressHeading;
    private readonly TextBlock _state;
    private readonly TextBlock _path;
    private readonly TextBlock _statistics;
    private readonly TextBlock _lastConfirmed;
    private readonly TextBlock _progressLabel;
    private readonly TextBlock _fileCaption;
    private readonly ProgressBar _progress;
    private readonly Button _pause;
    private readonly Button _stop;
    private readonly Button _details;
    private readonly Button _hide;
    private readonly Action _onPause;
    private readonly Action _onHide;
    private bool _hasPlaced;
    private bool _allowClose;
    private Brush _accent = Brushes.DodgerBlue;
    private Brush _accentPale = Brushes.AliceBlue;
    private Brush? _lastSurface, _lastInk, _lastAccent, _lastAccentPale, _lastOutline;
    private double _lastAppearanceScale = double.NaN;

    public FloatingScanWindow(Action onPause, Action onStop, Action onHide, Action onDetails)
    {
        _onPause = onPause;
        _onHide = onHide;
        Width = 454;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        Title = "PalClam · Monitor flotante de análisis";
        Background = Brushes.Transparent;

        var root = new StackPanel { Margin = new Thickness(18, 15, 18, 15) };
        var heading = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var dragRegion = new Border { Background = Brushes.Transparent, Cursor = Cursors.SizeAll };
        var brandRow = new StackPanel { Orientation = Orientation.Horizontal };
        _iconShell = new Border
        {
            Background = Brushes.AliceBlue, CornerRadius = new CornerRadius(12),
            Padding = new Thickness(8), Margin = new Thickness(0, 0, 11, 0)
        };
        _iconShell.Child = new System.Windows.Controls.Image
        {
            Source = new BitmapImage(new Uri("pack://application:,,,/Assets/PalClam-256.png")),
            Width = 27, Height = 27, Stretch = Stretch.Uniform
        };
        brandRow.Children.Add(_iconShell);
        var brandTitle = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        _brandLabel = new TextBlock
        {
            Text = "PALCLAM  /  SCAN CONTROL", FontSize = 10,
            FontWeight = FontWeights.Bold, Foreground = Brushes.SlateGray
        };
        brandTitle.Children.Add(_brandLabel);
        _brandTitle = new TextBlock
        {
            Text = "Monitor flotante", FontSize = 16,
            FontWeight = FontWeights.Bold, Foreground = Brushes.Black
        };
        brandTitle.Children.Add(_brandTitle);
        brandRow.Children.Add(brandTitle);
        dragRegion.Child = brandRow;
        dragRegion.ToolTip = "Arrastra desde aquí para mover la ventana";
        dragRegion.MouseLeftButtonDown += (_, e) =>
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            try { DragMove(); } catch (InvalidOperationException) { }
        };
        heading.Children.Add(dragRegion);

        _hide = new Button
        {
            Content = "✕", Width = 31, Height = 31, Cursor = Cursors.Hand,
            ToolTip = "Ocultar monitor (el análisis continúa)",
            Background = Brushes.Transparent, BorderThickness = new Thickness(0),
            FontSize = 15, FontWeight = FontWeights.SemiBold
        };
        System.Windows.Automation.AutomationProperties.SetName(_hide, "Ocultar monitor flotante sin detener análisis");
        _hide.Click += (_, _) => { _onHide(); Hide(); };
        Grid.SetColumn(_hide, 1);
        heading.Children.Add(_hide);
        root.Children.Add(heading);

        var stateRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 11) };
        _statusChip = new Border { Background = Brushes.AliceBlue, CornerRadius = new CornerRadius(12),
            Padding = new Thickness(11, 6, 11, 6) };
        _state = new TextBlock { Text = "PREPARANDO ANÁLISIS", FontSize = 12.5,
            FontWeight = FontWeights.Bold, Foreground = Brushes.DodgerBlue };
        _statusChip.Child = _state;
        stateRow.Children.Add(_statusChip);
        root.Children.Add(stateRow);

        _fileCaption = new TextBlock { Text = "ARCHIVO O LOTE EN EXAMEN", FontSize = 10,
            FontWeight = FontWeights.Bold, Foreground = Brushes.SlateGray };
        root.Children.Add(_fileCaption);
        _path = new TextBlock
        {
            Text = "Esperando información del motor…", Margin = new Thickness(0, 5, 0, 12),
            TextTrimming = TextTrimming.CharacterEllipsis, TextWrapping = TextWrapping.NoWrap,
            FontSize = 12.5, FontWeight = FontWeights.SemiBold
        };
        root.Children.Add(_path);

        var progressHeader = new Grid { Margin = new Thickness(0, 0, 0, 5) };
        progressHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        progressHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _progressHeading = new TextBlock { Text = "PROGRESO DEL ANÁLISIS", FontSize = 10,
            FontWeight = FontWeights.Bold, Foreground = Brushes.SlateGray };
        progressHeader.Children.Add(_progressHeading);
        _progressLabel = new TextBlock { Text = "Preparando…", FontSize = 10,
            Foreground = Brushes.SlateGray, FontWeight = FontWeights.SemiBold };
        Grid.SetColumn(_progressLabel, 1);
        progressHeader.Children.Add(_progressLabel);
        root.Children.Add(progressHeader);
        _progress = new ProgressBar
        {
            Height = 8, Minimum = 0, Maximum = 100, IsIndeterminate = true,
            Foreground = Brushes.DodgerBlue, Background = Brushes.Gainsboro,
            BorderThickness = new Thickness(0), Margin = new Thickness(0, 0, 0, 12)
        };
        root.Children.Add(_progress);

        _statsPanel = new Border { Background = Brushes.AliceBlue, CornerRadius = new CornerRadius(9),
            Padding = new Thickness(10, 9, 10, 9), Margin = new Thickness(0, 0, 0, 11) };
        _statistics = new TextBlock { FontSize = 11.5, Text = "Esperando el motor…", TextWrapping = TextWrapping.Wrap,
            MaxHeight = 75 };
        _statsPanel.Child = _statistics;
        root.Children.Add(_statsPanel);
        // Estas cifras nunca se interpretan como "archivo actual"; ClamAV notifica
        // el último elemento cuando termina y el modo equilibrado usa lotes.
        _lastConfirmed = new TextBlock
        {
            Text = "Último archivo confirmado: —", FontSize = 11,
            TextWrapping = TextWrapping.Wrap, MaxHeight = 45,
            Margin = new Thickness(0, 0, 0, 11)
        };
        root.Children.Add(_lastConfirmed);

        var buttons = new Grid();
        for (int i = 0; i < 3; i++)
            buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _pause = new Button
        {
            Content = "Ⅱ  Pausar", MinHeight = 39, Margin = new Thickness(0, 0, 7, 0),
            FontWeight = FontWeights.SemiBold, Cursor = Cursors.Hand,
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 243, 217)),
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(159, 101, 10)),
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 196, 127)),
            BorderThickness = new Thickness(1)
        };
        _pause.Click += (_, _) => _onPause();
        buttons.Children.Add(_pause);
        _stop = new Button
        {
            Content = "■  Detener", MinHeight = 39, Margin = new Thickness(0, 0, 7, 0),
            FontWeight = FontWeights.Bold, Cursor = Cursors.Hand,
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 43, 66)),
            Foreground = Brushes.White, BorderThickness = new Thickness(0)
        };
        _stop.Click += (_, _) => onStop();
        Grid.SetColumn(_stop, 1);
        buttons.Children.Add(_stop);
        _details = new Button
        {
            Content = "Detalles ↗", MinHeight = 39, FontWeight = FontWeights.SemiBold,
            Cursor = Cursors.Hand, Background = Brushes.AliceBlue, BorderThickness = new Thickness(1)
        };
        _details.Click += (_, _) => onDetails();
        Grid.SetColumn(_details, 2);
        buttons.Children.Add(_details);
        root.Children.Add(buttons);
        foreach (var command in new[] { _pause, _stop, _details, _hide })
            RoundFloatingButton(command);

        // Bordes suavizados y sombra real; se arrastra desde el encabezado.
        _card = new Border
        {
            Background = Brushes.White, BorderBrush = Brushes.Gainsboro,
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(17),
            Child = root, Margin = new Thickness(13),
            Effect = new DropShadowEffect
            {
                Color = Colors.Black, BlurRadius = 22,
                ShadowDepth = 4, Opacity = 0.22
            }
        };
        Content = _card;
        Closing += (_, e) =>
        {
            if (_allowClose) return;
            e.Cancel = true;
            _onHide();
            Hide();
        };
    }

    private static void RoundFloatingButton(Button button)
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
        border.SetBinding(Border.BackgroundProperty, new Binding("Background")
        { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        border.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush")
        { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        border.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness")
        { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.ContentSourceProperty, "Content");
        content.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
        content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(content);
        button.Template = new ControlTemplate(typeof(Button)) { VisualTree = border };
        var style = new Style(typeof(Button));
        var hover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.84));
        var disabled = new Trigger { Property = Button.IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.43));
        style.Triggers.Add(hover);
        style.Triggers.Add(disabled);
        button.Style = style;
    }

    public void ApplyAppearance(Brush surface, Brush ink, Brush accent, Brush accentPale,
        Brush outline, double scale)
    {
        // Durante un análisis se actualizan las métricas cuatro veces por segundo.
        // Los pinceles son objetos compartidos: su Color se actualiza automáticamente
        // sin tener que tocar 25 propiedades WPF en cada tick.
        var s = Math.Clamp(scale, 0.9, 1.3);
        if (ReferenceEquals(surface, _lastSurface) && ReferenceEquals(ink, _lastInk) &&
            ReferenceEquals(accent, _lastAccent) && ReferenceEquals(accentPale, _lastAccentPale) &&
            ReferenceEquals(outline, _lastOutline) && Math.Abs(s - _lastAppearanceScale) < .001)
            return;
        _lastSurface = surface;
        _lastInk = ink;
        _lastAccent = accent;
        _lastAccentPale = accentPale;
        _lastOutline = outline;
        _lastAppearanceScale = s;
        _accent = accent;
        _accentPale = accentPale;
        _card.Background = surface;
        _card.BorderBrush = outline;
        _iconShell.Background = accentPale;
        _statsPanel.Background = accentPale;
        _brandTitle.Foreground = ink;
        _brandLabel.Foreground = accent;
        _progressHeading.Foreground = ink;
        _progressLabel.Foreground = ink;
        _fileCaption.Foreground = ink;
        _path.Foreground = ink;
        _statistics.Foreground = ink;
        _lastConfirmed.Foreground = ink;
        _lastConfirmed.Opacity = 0.8;
        _progress.Foreground = accent;
        _progress.Background = outline;
        _details.Background = accentPale;
        _details.Foreground = accent;
        _details.BorderBrush = outline;
        _hide.Foreground = ink;
        _brandTitle.FontSize = 16 * s;
        _brandLabel.FontSize = 10 * s;
        _progressHeading.FontSize = 10 * s;
        _state.FontSize = 12.5 * s;
        _path.FontSize = 12.5 * s;
        _statistics.FontSize = 11.5 * s;
        _lastConfirmed.FontSize = 11 * s;
        _progressLabel.FontSize = 10 * s;
        _fileCaption.FontSize = 10 * s;
        _pause.FontSize = _stop.FontSize = _details.FontSize = 11.5 * s;
        _statusChip.Background = accentPale;
        _state.Foreground = accent;
    }

    public void ShowForScan(bool alwaysOnTop)
    {
        Topmost = alwaysOnTop;
        if (IsVisible) return;
        if (!_hasPlaced)
        {
            var bounds = SystemParameters.WorkArea;
            Left = Math.Max(bounds.Left, bounds.Right - Width - 24);
            Top = Math.Max(bounds.Top, bounds.Bottom - 345);
            _hasPlaced = true;
        }
        Show();
    }

    public void UpdateScan(string state, string path, string statistics, string lastConfirmed,
        double? percent, bool paused, bool pausePending, bool stopping, bool canPause, bool expanded)
    {
        _state.Text = "●  " + state;
        bool warning = paused || pausePending || stopping;
        _state.Foreground = warning ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(159, 101, 10)) : _accent;
        _statusChip.Background = warning ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 243, 217)) : _accentPale;
        _path.Text = path;
        _path.ToolTip = path;
        _statistics.Text = statistics;
        _lastConfirmed.Text = "Último archivo confirmado: " + lastConfirmed;
        _lastConfirmed.ToolTip = lastConfirmed;
        _lastConfirmed.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
        _progress.IsIndeterminate = !percent.HasValue && !paused && !stopping;
        if (percent.HasValue) _progress.Value = Math.Clamp(percent.Value, 0, 100);
        _progressLabel.Text = percent.HasValue ? $"≈ {Math.Clamp(percent.Value,0,100):0}% estimado" : "Progreso no estimable";
        _pause.IsEnabled = canPause && !stopping;
        _pause.Content = paused ? "▶  Reanudar" : pausePending ? "↶  Anular pausa" : "Ⅱ  Pausar";
        _stop.IsEnabled = !stopping;
        _stop.Content = stopping ? "Deteniendo…" : "■  Detener";
    }

    public void EndScan() => Hide();

    public void DisposeWindow()
    {
        _allowClose = true;
        Close();
    }
}
