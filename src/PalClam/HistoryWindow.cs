// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using PalClam.Infrastructure;
using PalClam.Services;

namespace PalClam;

internal sealed class HistoryWindow : Window
{
    public HistoryWindow(Window owner)
    {
        Owner = owner;
        var appearance = SettingsStore.Load();
        var palette = AppearanceCatalog.Find(appearance.UiTheme);
        var textScale = AppearanceCatalog.NormalizeScaleValue(appearance.UiTextScale);
        Title = "PalClam · Historial de análisis";
        Width = 850;
        Height = 560;
        MinWidth = 650;
        MinHeight = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(palette.Surface)!);
        FontFamily = new FontFamily("Segoe UI");

        var root = new DockPanel { Margin = new Thickness(22) };
        var heading = new TextBlock
        {
            Text = "Historial de análisis (local)", FontSize = 22 * textScale,
            Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(palette.Ink)!), FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 12)
        };
        DockPanel.SetDock(heading, Dock.Top);
        root.Children.Add(heading);

        var bar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        var export = new Button { Content = "Exportar informe TXT", FontSize = 12 * textScale,
            Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(palette.AccentPale)!),
            Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(palette.AccentText)!), Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 10, 0) };
        export.Click += (_, _) => Export();
        bar.Children.Add(export);
        var close = new Button { Content = "Cerrar", FontSize = 12 * textScale, Padding = new Thickness(14, 8, 14, 8) };
        close.Click += (_, _) => Close();
        bar.Children.Add(close);
        DockPanel.SetDock(bar, Dock.Bottom);
        root.Children.Add(bar);

        var text = new TextBox
        {
            Text = ScanHistory.RenderText(), IsReadOnly = true, TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Consolas"), FontSize = 12 * textScale,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(12)
        };
        PalClam.UI.PresentationLocalization.SetReport(text, ScanHistory.RenderText());
        root.Children.Add(text);
        Content = root;
    }

    private void Export()
    {
        var save = new SaveFileDialog
        {
            Title = "Exportar historial de PalClam",
            FileName = $"PalClam_Historial_{DateTime.Now:yyyyMMdd_HHmm}.txt",
            Filter = "Archivo de texto|*.txt",
            AddExtension = true,
            DefaultExt = ".txt"
        };
        if (PalClam.UI.LocalizedDialogs.Show(save, this) != true) return;
        try
        {
            File.WriteAllText(save.FileName, PalClam.Localization.DisplayText.Translate(ScanHistory.RenderText()));
            MessageBox.Show(this, "Informe exportado. Puede contener rutas personales: compártelo solo si lo deseas.",
                "Informe guardado", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo exportar: " + ex.Message,
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
