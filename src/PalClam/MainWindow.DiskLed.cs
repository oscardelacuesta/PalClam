// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PalClam.Infrastructure;
using PalClam.Localization;
using PalClam.Services;
using D = System.Drawing;
using Forms = System.Windows.Forms;

namespace PalClam;

public sealed partial class MainWindow
{
    private Forms.NotifyIcon? _diskLedIcon;
    private DispatcherTimer? _diskLedTimer;
    private DiskActivitySampler? _diskSampler;
    private DiskActivitySnapshot? _diskSnapshot;
    private DiskLedOptions _diskLedOptions = new();
    private readonly Dictionary<string, D.Icon> _diskLedImages = new();
    private bool _diskLedSampling, _diskLedPulse;
    private int _diskLedGeneration;
    private FrameworkElement? _diskLedCard;
    private Forms.ToolStripMenuItem? _diskStateItem, _diskReadItem, _diskWriteItem, _diskSpaceItem, _diskAlertItem;

    private void ConfigureDiskLed()
    {
        DisposeDiskLed();
        _diskLedOptions = DiskLedOptions.Normalize(_settings.DiskLed);
        if (!_diskLedOptions.Enabled || !_trayHasInitialized) return;
        _diskSampler = new DiskActivitySampler(_diskLedOptions);
        var menu = new Forms.ContextMenuStrip();
        _diskStateItem = new Forms.ToolStripMenuItem { Enabled = false };
        _diskReadItem = new Forms.ToolStripMenuItem { Enabled = false };
        _diskWriteItem = new Forms.ToolStripMenuItem { Enabled = false };
        _diskSpaceItem = new Forms.ToolStripMenuItem { Enabled = false };
        menu.Items.AddRange(new Forms.ToolStripItem[] { _diskStateItem, _diskReadItem, _diskWriteItem, _diskSpaceItem,
            new Forms.ToolStripSeparator() });
        var open = new Forms.ToolStripMenuItem();
        open.Click += (_, _) => { ShowFromTray(); Navigate("scan"); };
        var settings = new Forms.ToolStripMenuItem();
        settings.Click += (_, _) => OpenDiskLedOptions();
        _diskAlertItem = new Forms.ToolStripMenuItem();
        _diskAlertItem.Click += (_, _) => { _trayAlert = false; _ransomwareAlert = false; UpdateTrayState(); RenderDiskLed(); };
        menu.Items.Add(open); menu.Items.Add(settings); menu.Items.Add(_diskAlertItem);
        menu.Opening += (_, _) =>
        {
            open.Text = DisplayText.Translate("Abrir PalClam");
            settings.Text = DisplayText.Translate("Configurar LED de disco");
            _diskAlertItem.Text = DisplayText.Translate("Reconocer alerta");
            RenderDiskLed();
        };
        _diskLedIcon = new Forms.NotifyIcon { ContextMenuStrip = menu, Visible = false };
        _diskLedIcon.DoubleClick += (_, _) => OpenDiskLedOptions();
        _diskLedTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_diskLedOptions.IntervalMs) };
        _diskLedTimer.Tick += async (_, _) => await SampleDiskLedAsync();
        _diskLedTimer.Start();
        RenderDiskLed();
        _ = SampleDiskLedAsync();
    }

    private void OpenDiskLedOptions()
    {
        ShowFromTray(); OpenOptionsTab(0);
        _diskLedCard?.BringIntoView();
    }

    private async Task SampleDiskLedAsync()
    {
        if (_diskLedSampling || _diskSampler is null || _diskLedIcon is null) return;
        var sampler = _diskSampler; var generation = _diskLedGeneration;
        _diskLedSampling = true;
        try
        {
            var snapshot = await Task.Run(sampler.Sample);
            if (generation == _diskLedGeneration) _diskSnapshot = snapshot;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ExternalException or DllNotFoundException)
        {
            if (generation == _diskLedGeneration)
                _diskSnapshot = new(false, 0, 0, false, 0, 0, sampler.DriveRoot, DateTimeOffset.UtcNow, ex.Message);
        }
        finally
        {
            _diskLedSampling = false;
            if (generation == _diskLedGeneration) RenderDiskLed();
        }
    }

    private void RenderDiskLed()
    {
        if (_diskLedIcon is null) return;
        var snapshot = _diskSnapshot ?? new DiskActivitySnapshot(false, 0, 0, false, 0, 0,
            _diskSampler?.DriveRoot ?? "", DateTimeOffset.UtcNow, "");
        var scanning = (_scanRunning && !_scanPausedBetweenBatches) || _mailActive || _outlookScanning ||
            _guardService?.IsProcessing == true;
        var state = DiskLedPolicy.Select(_diskLedOptions, snapshot, scanning, _trayAlert);
        _diskLedPulse = !_diskLedPulse;
        var dim = _diskLedOptions.Pulse && _diskLedPulse && state is DiskLedState.Reading or DiskLedState.Writing or
            DiskLedState.ReadWrite or DiskLedState.Scanning;
        var color = _diskLedOptions.Colors[state.ToString()];
        var key = color + (dim ? "dim" : "full");
        if (!_diskLedImages.TryGetValue(key, out var icon))
        {
            icon = CreateDiskLedIcon(color, dim); _diskLedImages[key] = icon;
        }
        _diskLedIcon.Icon = icon; _diskLedIcon.Visible = true;
        var label = DisplayText.Translate(state == DiskLedState.Threat && _ransomwareAlert ? "Posible ransomware: revisar alertas" : DiskLedPolicy.Label(state));
        var tip = "PalClam LED · " + label;
        _diskLedIcon.Text = tip[..Math.Min(63, tip.Length)];
        if (_diskStateItem is not null)
            _diskStateItem.Text = label + " · " + DisplayText.Translate(_diskLedOptions.AllPhysicalDisks
                ? "Actividad de todos los discos físicos" : "Actividad del volumen seleccionado");
        if (_diskReadItem is not null) _diskReadItem.Text = DisplayText.Translate("Lectura:") + " " +
            (snapshot.IoAvailable ? DiskRate(snapshot.ReadBytesPerSecond) : "—");
        if (_diskWriteItem is not null) _diskWriteItem.Text = DisplayText.Translate("Escritura:") + " " +
            (snapshot.IoAvailable ? DiskRate(snapshot.WriteBytesPerSecond) : "—");
        if (_diskSpaceItem is not null) _diskSpaceItem.Text = DisplayText.Translate("Espacio libre:") + " " +
            snapshot.DriveRoot + " · " + (snapshot.SpaceAvailable
                ? (snapshot.FreeBytes / 1073741824.0).ToString("N1") + " GB (" + snapshot.FreePercent.ToString("N1") + " %)" : "—");
        if (_diskAlertItem is not null) _diskAlertItem.Enabled = _trayAlert;
    }

    private static string DiskRate(double bytes) => bytes >= 1048576
        ? (bytes / 1048576).ToString("N1", CultureInfo.CurrentCulture) + " MB/s"
        : (bytes / 1024).ToString("N1", CultureInfo.CurrentCulture) + " KB/s";

    private static D.Icon CreateDiskLedIcon(string htmlColor, bool dim)
    {
        using var bitmap = new D.Bitmap(32, 32, D.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = D.Graphics.FromImage(bitmap);
        graphics.SmoothingMode = D.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(D.Color.Transparent);
        var c = D.ColorTranslator.FromHtml(htmlColor);
        if (dim) c = D.Color.FromArgb(c.R * 2 / 3, c.G * 2 / 3, c.B * 2 / 3);
        using var halo = new D.SolidBrush(D.Color.FromArgb(48, c));
        using var fill = new D.Drawing2D.LinearGradientBrush(new D.Rectangle(5, 4, 22, 24),
            D.Color.FromArgb(Math.Min(255, c.R + 35), Math.Min(255, c.G + 35), Math.Min(255, c.B + 35)), c, 90f);
        using var edge = new D.Pen(D.Color.FromArgb(220, 28, 32, 42), 2);
        using var shine = new D.SolidBrush(D.Color.FromArgb(130, D.Color.White));
        graphics.FillEllipse(halo, 0, 0, 32, 32);
        graphics.FillEllipse(fill, 4, 4, 24, 24); graphics.DrawEllipse(edge, 4, 4, 24, 24);
        graphics.FillEllipse(shine, 9, 7, 12, 6);
        var handle = bitmap.GetHicon();
        try { using var borrowed = D.Icon.FromHandle(handle); return (D.Icon)borrowed.Clone(); }
        finally { DestroyLedIcon(handle); }
    }
    [DllImport("user32.dll", EntryPoint = "DestroyIcon")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyLedIcon(IntPtr icon);

    private void DisposeDiskLed()
    {
        _diskLedGeneration++; _diskLedTimer?.Stop(); _diskLedTimer = null;
        var sampler = _diskSampler; _diskSampler = null; _diskSnapshot = null;
        if (sampler is not null) _ = Task.Run(sampler.Dispose);
        if (_diskLedIcon is not null)
        {
            _diskLedIcon.Visible = false; _diskLedIcon.ContextMenuStrip?.Dispose();
            _diskLedIcon.Dispose(); _diskLedIcon = null;
        }
        foreach (var icon in _diskLedImages.Values) icon.Dispose();
        _diskLedImages.Clear();
    }

    private Border BuildDiskLedOptions()
    {
        var value = DiskLedOptions.Normalize(_settings.DiskLed);
        var card = Card(Surface, Outline); card.Margin = new Thickness(0, 13, 0, 0); _diskLedCard = card;
        var stack = new StackPanel(); card.Child = stack;
        stack.Children.Add(T("LED de disco en la bandeja", 16, Ink, FontWeights.Bold, new Thickness(0, 0, 0, 8)));
        stack.Children.Add(T("Un segundo icono muestra la actividad real de lectura y escritura. El aviso de espacio se refiere al volumen elegido. Windows puede guardar el icono en el menú de iconos ocultos.", 12, Muted));
        CheckBox Check(string label, bool selected)
        {
            var box = new CheckBox { Content = T(label, 12, Ink), HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch, IsChecked = selected, Margin = new Thickness(0, 9, 0, 0) };
            stack.Children.Add(box); return box;
        }
        var enabled = Check("Mostrar LED de disco", value.Enabled);
        var all = Check("Medir la actividad conjunta de todos los discos físicos", value.AllPhysicalDisks);
        var antivirus = Check("Mostrar análisis antivirus y detecciones en el LED", value.ShowAntivirusStates);
        var low = Check("Avisar de poco espacio libre", value.WarnLowSpace);
        var pulse = Check("Pulsación suave durante la actividad", value.Pulse);
        var row = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) }; stack.Children.Add(row);
        ComboBox Combo(string label, object[] items, object selected)
        {
            var group = new StackPanel { Margin = new Thickness(0, 0, 18, 8) };
            group.Children.Add(T(label, 12, Ink));
            var box = new ComboBox { ItemsSource = items, SelectedItem = selected, MinWidth = 120, Margin = new Thickness(0, 5, 0, 0) };
            group.Children.Add(box); row.Children.Add(group); return box;
        }
        var roots = new List<object> { "auto" };
        try { roots.AddRange(DriveInfo.GetDrives().Where(d => d.DriveType is DriveType.Fixed or DriveType.Removable).Select(d => (object)d.Name)); }
        catch (IOException) { }
        if (!roots.Contains(value.DriveRoot)) roots.Add(value.DriveRoot);
        var drive = Combo("Volumen (auto = Windows)", roots.ToArray(), value.DriveRoot);
        var interval = Combo("Intervalo (ms)", new object[] { 250, 500, 1000, 2000 }, value.IntervalMs);
        TextBox Number(string label, int number)
        {
            var group = new StackPanel { Margin = new Thickness(0, 0, 18, 8) };
            group.Children.Add(T(label, 12, Ink));
            var box = new TextBox { Text = number.ToString(CultureInfo.InvariantCulture), Width = 120,
                MaxLength = 7, Padding = new Thickness(6), Margin = new Thickness(0, 5, 0, 0) };
            group.Children.Add(box); row.Children.Add(group); return box;
        }
        var threshold = Number("Actividad mínima (KB/s)", value.ActivityThresholdKb);
        var percent = Number("Avisar por debajo de (%)", value.MinFreePercent);
        var gb = Number("Avisar por debajo de (GB)", value.MinFreeGb);
        stack.Children.Add(T("Un umbral de espacio a 0 desactiva ese criterio. Si se cumplen ambos criterios de aviso, basta uno para mostrar la alerta. Prioridad: detección, poco espacio, análisis, actividad y reposo. Una detección permanece hasta reconocerla.", 11, Muted));
        var colorRows = new WrapPanel { Margin = new Thickness(0, 12, 0, 6) }; stack.Children.Add(colorRows);
        var colors = new Dictionary<string, TextBox>();
        foreach (var state in Enum.GetValues<DiskLedState>())
        {
            var group = new StackPanel { Width = 240, Margin = new Thickness(0, 0, 14, 9) };
            group.Children.Add(T(DiskLedPolicy.Label(state), 12, Ink));
            var line = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
            var preview = new Border { Width = 22, Height = 22, CornerRadius = new CornerRadius(11), Margin = new Thickness(0, 0, 9, 0) };
            var input = new TextBox { Text = value.Colors[state.ToString()], MaxLength = 7, Width = 100, Padding = new Thickness(4) };
            void Preview()
            {
                if (Regex.IsMatch(input.Text, "^#[0-9a-fA-F]{6}$"))
                    preview.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(input.Text));
            }
            input.TextChanged += (_, _) => Preview(); Preview();
            line.Children.Add(preview); line.Children.Add(input); group.Children.Add(line); colorRows.Children.Add(group);
            colors[state.ToString()] = input;
        }
        var status = T("", 12, Muted); var actions = new WrapPanel();
        actions.Children.Add(ActionButton("Restablecer colores", false, (_, _) =>
        {
            foreach (var pair in DiskLedOptions.Defaults()) colors[pair.Key].Text = pair.Value;
        }));
        actions.Children.Add(ActionButton("Guardar y aplicar LED", true, (_, _) =>
        {
            if (!int.TryParse(threshold.Text, out var activity) || activity is < 0 or > 1048576 ||
                !int.TryParse(percent.Text, out var pc) || pc is < 0 or > 50 ||
                !int.TryParse(gb.Text, out var space) || space is < 0 or > 10000 ||
                colors.Values.Any(box => !Regex.IsMatch(box.Text, "^#[0-9a-fA-F]{6}$")))
            { status.Text = "Revisa los valores: actividad 0–1048576, porcentaje 0–50, GB 0–10000 y colores #RRGGBB."; status.Foreground = Red; return; }
            var previous = _settings.DiskLed;
            _settings.DiskLed = DiskLedOptions.Normalize(new DiskLedOptions
            {
                Enabled = enabled.IsChecked == true, DriveRoot = drive.SelectedItem as string ?? "auto",
                AllPhysicalDisks = all.IsChecked == true, IntervalMs = (int)(interval.SelectedItem ?? 500),
                ActivityThresholdKb = activity, ShowAntivirusStates = antivirus.IsChecked == true,
                WarnLowSpace = low.IsChecked == true, MinFreePercent = pc, MinFreeGb = space,
                Pulse = pulse.IsChecked == true, Colors = colors.ToDictionary(p => p.Key, p => p.Value.Text)
            });
            try
            {
                SettingsStore.Save(_settings); ConfigureDiskLed(); status.Text = "Preferencias del LED guardadas y aplicadas."; status.Foreground = Green;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { _settings.DiskLed = previous; status.Text = "No se pudo guardar la configuración: " + ex.Message; status.Foreground = Red; }
        }));
        stack.Children.Add(actions); stack.Children.Add(status); return card;
    }
}
