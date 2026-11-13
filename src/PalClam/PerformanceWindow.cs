// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using PalClam.Infrastructure;
using PalClam.Services;

namespace PalClam;

/// <summary>Historial y comparación local, sin envío de telemetría a servidores.</summary>
internal sealed class PerformanceWindow : Window
{
    private readonly List<ScanRecord> _records;
    private readonly ComboBox _targetPicker;
    private readonly StackPanel _content;
    private readonly Brush _ink;
    private readonly Brush _muted;
    private readonly AppearancePalette _palette;
    private readonly double _textScale;

    public PerformanceWindow(Window owner)
    {
        Owner = owner;
        var appearance = SettingsStore.Load();
        _palette = AppearanceCatalog.Find(appearance.UiTheme);
        _textScale = AppearanceCatalog.NormalizeScaleValue(appearance.UiTextScale);
        _ink = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_palette.Ink)!);
        _muted = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_palette.Muted)!);
        Title = "PalClam · Rendimiento histórico y comparaciones";
        Width = 900; Height = 760; MinWidth = 700; MinHeight = 550;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_palette.Surface)!);
        FontFamily = new FontFamily("Segoe UI");
        _records = ScanHistory.ReadRecent(500).Where(x => x.MetricsEnabled).ToList();
        var outer = new DockPanel { Margin = new Thickness(20) };
        Content = outer;
        var top = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
        DockPanel.SetDock(top, Dock.Top);
        outer.Children.Add(top);
        top.Children.Add(Text("RENDIMIENTO DE PALCLAM", 22, _ink, true));
        top.Children.Add(Text("Métricas de ejecución; NO miden la eficacia de detección. " +
            "Las ejecuciones antiguas pueden carecer de metadatos suficientes.", 12, _muted));
        var scanner = ClamAvLocator.FindScanner();
        var clamd = string.IsNullOrEmpty(scanner) ? "" : System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(scanner) ?? "", "clamd.exe");
        top.Children.Add(Text(!string.IsNullOrEmpty(clamd) && File.Exists(clamd)
            ? "clamd.exe localizado. Multihilo experimental NO habilitado: requiere configuración aislada y validación."
            : "clamd.exe no detectado junto al escáner. Se mantiene el motor clamscan seguro y probado.",
            11, _muted));
        var controls = new WrapPanel { Margin = new Thickness(0, 15, 0, 4) };
        top.Children.Add(controls);
        controls.Children.Add(Text("Objetivo:", 13, _ink, true));
        _targetPicker = new ComboBox { Width = 480, Margin = new Thickness(12, 0, 12, 0), Height = 32,
            FontSize = 12 * _textScale };
        _targetPicker.ItemsSource = _records.Select(x => x.Target)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        controls.Children.Add(_targetPicker);
        var export = new Button { Content = "Exportar CSV", Padding = new Thickness(12, 5, 12, 5),
            FontSize = 12 * _textScale,
            Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_palette.AccentPale)!),
            Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_palette.AccentText)!) };
        export.Click += (_, _) => ExportCsv();
        controls.Children.Add(export);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        outer.Children.Add(scroll);
        _content = new StackPanel();
        scroll.Content = _content;
        _targetPicker.SelectionChanged += (_, _) => Render();
        if (_records.Count > 0) _targetPicker.SelectedIndex = 0;
        else _content.Children.Add(Text("No hay análisis con métricas guardadas todavía.", 14, _muted));
    }

    private void Render()
    {
        _content.Children.Clear();
        var target = _targetPicker.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(target)) return;
        var rows = _records.Where(r => string.Equals(r.Target, target,
            StringComparison.OrdinalIgnoreCase)).OrderBy(r => r.CompletedAt).ToArray();
        if (rows.Length == 0) return;
        var recent = rows.TakeLast(20).ToArray();
        _content.Children.Add(Text($"{recent.Length} análisis históricos de este objetivo", 14, _ink, true));
        _content.Children.Add(Text("Gráficas históricas (no implican comparabilidad estadística):", 12, _muted));
        AddChart("Velocidad · archivos/s (resumen oficial)", recent.Select(r =>
            r.Seconds > 0 && r.EngineScannedFiles >= 0 ? (double)r.EngineScannedFiles / r.Seconds : 0).ToArray(),
            Color.FromRgb(33, 103, 211));
        AddChart("CPU media del motor · % del equipo", recent.Select(r =>
            r.AverageEngineCpuPercent).ToArray(), (Color)System.Windows.Media.ColorConverter.ConvertFromString(_palette.Accent)!);
        AddChart("Pico de RAM · MiB", recent.Select(r =>
            r.PeakEngineMemoryBytes / 1048576.0).ToArray(), Color.FromRgb(22, 129, 92));

        _content.Children.Add(Text("Dos últimos análisis (comparación estricta)", 15, _ink, true));
        if (rows.Length < 2)
        {
            _content.Children.Add(Text("Repite el mismo análisis para obtener una comparación.", 12, _muted));
        }
        else
        {
            var previous = rows[^2]; var current = rows[^1];
            var comparison = ScanComparisonService.Compare(previous, current);
            _content.Children.Add(Text(comparison.Status == Comparability.Comparable
                ? $"Duración: {previous.Seconds}s → {current.Seconds}s ({comparison.SecondsDeltaPercent:+0.0;-0.0;0.0} %). " +
                  $"Velocidad: {comparison.FilesRateDeltaPercent:+0.0;-0.0;0.0} %."
                : $"No comparables: {comparison.Explanation}", 13,
                comparison.Status == Comparability.Comparable ? _ink : _muted, true));
            if (comparison.Status == Comparability.Comparable)
                _content.Children.Add(Text(comparison.Explanation, 12, _muted));
        }
        var table = new System.Windows.Controls.DataGrid { AutoGenerateColumns = false, IsReadOnly = true,
            FontSize = 12 * _textScale,
            CanUserAddRows = false, CanUserDeleteRows = false,
            Height = 260, Margin = new Thickness(0, 12, 0, 20), HeadersVisibility = DataGridHeadersVisibility.Column };
        table.Columns.Add(new DataGridTextColumn { Header = "Fecha", Binding = new System.Windows.Data.Binding("CompletedAt")
            { StringFormat = "dd/MM HH:mm" }, Width = 110 });
        table.Columns.Add(new DataGridTextColumn { Header = "Estado", Binding = new System.Windows.Data.Binding("Outcome"), Width = 160 });
        table.Columns.Add(new DataGridTextColumn { Header = "Tiempo (s)", Binding = new System.Windows.Data.Binding("Seconds"), Width = 88 });
        table.Columns.Add(new DataGridTextColumn { Header = "CPU media %", Binding = new System.Windows.Data.Binding("AverageEngineCpuPercent")
            { StringFormat = "F1" }, Width = 96 });
        table.Columns.Add(new DataGridTextColumn { Header = "Oficial: archivos", Binding = new System.Windows.Data.Binding("EngineScannedFiles"), Width = 110 });
        table.Columns.Add(new DataGridTextColumn { Header = "Prioridad", Binding = new System.Windows.Data.Binding("ScanPriority"), Width = 95 });
        table.ItemsSource = rows.Reverse().Take(50).ToArray();
        _content.Children.Add(table);
    }

    private void AddChart(string caption, double[] points, Color color)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 10, 0, 14) };
        panel.Children.Add(Text(caption, 12, _ink, true));
        var view = new Viewbox { Stretch = Stretch.Fill, Height = 94, HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch };
        var canvas = new Canvas { Width = 680, Height = 90, Background =
            new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_palette.Sidebar)!) };
        canvas.Children.Add(new Line { X1 = 12, X2 = 667, Y1 = 77, Y2 = 77,
            Stroke = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_palette.Outline)!), StrokeThickness = 1 });
        var normalized = ScanPerformanceSeries.Normalize(points);
        var poly = new Polyline { Stroke = new SolidColorBrush(color), StrokeThickness = 2.5,
            StrokeLineJoin = PenLineJoin.Round };
        for (var i = 0; i < normalized.Length; i++)
            poly.Points.Add(new System.Windows.Point(12 + (normalized.Length == 1 ? 0 : (655.0 * i / (normalized.Length - 1))),
                77 - normalized[i] * 62));
        canvas.Children.Add(poly);
        view.Child = canvas;
        panel.Children.Add(view);
        var max = points.Length == 0 ? 0 : points.Max();
        panel.Children.Add(Text($"Máximo mostrado: {max:0.0}. Gráfica relativa a la escala de esta serie.", 11, _muted));
        _content.Children.Add(panel);
    }

    private void ExportCsv()
    {
        var target = _targetPicker.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(target)) return;
        var dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "PalClam_Rendimiento.csv" };
        if (PalClam.UI.LocalizedDialogs.Show(dialog, this) != true) return;
        try
        {
            static string Csv(string? input) => "\"" + (input ?? "").Replace("\"", "\"\"") + "\"";
            var sb = new StringBuilder("Fecha;Objetivo;Estado;Segundos;ArchivosOficiales;CPUmedia;CPUpico;RAMpicoMiB;Modo;Motor;Prioridad\n");
            foreach (var row in _records.Where(x => string.Equals(x.Target, target, StringComparison.OrdinalIgnoreCase)))
            {
                sb.Append(string.Join(';', Csv(row.CompletedAt.ToString("O")), Csv(row.Target), Csv(row.Outcome),
                    row.Seconds.ToString(CultureInfo.InvariantCulture), row.EngineScannedFiles.ToString(CultureInfo.InvariantCulture),
                    row.AverageEngineCpuPercent.ToString("F2", CultureInfo.InvariantCulture),
                    row.PeakEngineCpuPercent.ToString("F2", CultureInfo.InvariantCulture),
                    (row.PeakEngineMemoryBytes / 1048576.0).ToString("F2", CultureInfo.InvariantCulture),
                    Csv(row.EngineMode), Csv(row.EngineVersion), Csv(row.ScanPriority))).Append('\n');
            }
            File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(true));
            MessageBox.Show(this, "CSV exportado. Puede contener rutas personales; revísalo antes de compartirlo.",
                "PalClam", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Error al exportar: " + ex.Message, "PalClam",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private TextBlock Text(string text, double size, Brush color, bool bold = false) => new()
    {
        Text = text, TextWrapping = TextWrapping.Wrap, FontSize = Math.Max(8, size * _textScale), Foreground = color,
        Margin = new Thickness(0, 4, 0, 4), FontWeight = bold ? FontWeights.Bold : FontWeights.Normal
    };
}
