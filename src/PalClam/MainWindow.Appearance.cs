// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    // AttachedProperty guarda el tamaño ORIGINAL. Cambio repetido 90 -> 130 -> 100
    // sin multiplicar escalas. Los controles sin tamaño local heredan del padre.
    private static readonly DependencyProperty OriginalTextSizeProperty =
        DependencyProperty.RegisterAttached("OriginalTextSize", typeof(double), typeof(MainWindow),
            new PropertyMetadata(double.NaN));

    private bool _applyingAppearanceChange;
    private double _appliedTextScale = double.NaN;

    private void ApplyAppearancePalette(string id)
    {
        // No aplicar la mitad de un tema si WPF ha congelado un pincel compartido:
        // detectar el fallo ANTES de modificar cualquier color y dejar constancia.
        // Se toma una instantánea por si un control crea nuevos pinceles al actualizarse.
        var brushes = UiBrushes.ToArray();
        if (brushes.Any(pair => pair.Value.IsFrozen))
            throw new InvalidOperationException(
                "La paleta contiene pinceles congelados por un estilo WPF.");

        if (_heroGradient is not null && (_heroGradient.IsFrozen ||
            _heroGradient.GradientStops.Any(stop => stop.IsFrozen)))
            throw new InvalidOperationException("El degradado de cabecera está congelado.");

        // Precalcular y validar TODA la paleta antes de tocar un pincel: un valor
        // inválido no debe dejar el escritorio con un tema aplicado a medias.
        var palette = AppearanceCatalog.Find(id);
        var changes = brushes.Select(pair => (pair.Value, Color: ParseThemeColor(
            AppearanceCatalog.MapColor(palette.Id, pair.Key)))).ToArray();
        var from = ParseThemeColor(palette.HeroFrom);
        var to = ParseThemeColor(palette.HeroTo);
        foreach (var change in changes)
            change.Value.Color = change.Color;
        if (_heroGradient is not null)
        {
            _heroGradient.GradientStops[0].Color = from;
            _heroGradient.GradientStops[1].Color = to;
        }
        ActiveTheme = palette.Id;
        PalClam.UI.ModernScrollBarTheme.Apply(Application.Current, palette);
        UpdateHeaderAuroraPalette(palette);
        Background = BackgroundColor;
        Foreground = Ink;
        RefreshOptionsTabTheme();
        UpdateQuickThemeButton();
        // Cambiar solo la presentación NUNCA pausa ni cancela el motor ClamAV.
        _floatingScanWindow?.ApplyAppearance(Surface, Ink, Purple, PurplePale,
            Outline, _settings.UiTextScale);
    }

    private static Color ParseThemeColor(string value) =>
        (Color)System.Windows.Media.ColorConverter.ConvertFromString(value)!;

    private void OnAppearanceChanged()
    {
        if (_applyingAppearanceChange || _themeOption is null || _textScaleOption is null ||
            _themeOption.SelectedItem is not AppearancePalette selected ||
            _textScaleOption.SelectedItem is not double selectedScale) return;

        var nextScale = AppearanceCatalog.NormalizeScaleValue(selectedScale);
        var oldTheme = ActiveTheme;
        var oldScale = _settings.UiTextScale;
        if (selected.Id == oldTheme && Math.Abs(nextScale - oldScale) < .001) return;

        _applyingAppearanceChange = true;
        try
        {
            // Una elección de COLOR no debe recorrer/escalar la vista de resultados
            // (puede estar recibiendo centenares de archivos durante el análisis).
            if (selected.Id != oldTheme)
                ApplyAppearancePalette(selected.Id);
            _settings.UiTextScale = nextScale;
            if (Math.Abs(nextScale - _appliedTextScale) >= .001)
                ApplyAppearanceTextScale();
            _settings.UiTheme = selected.Id;
            SaveSettings();
            if (_optionsStatus is not null)
                _optionsStatus.Text = "Tema " + selected.Name + " y tamaño " +
                    (nextScale * 100).ToString("0") + "% guardados.";
        }
        catch (Exception ex)
        {
            CrashDiagnostics.Record("cambio-tema", ex);
            _settings.UiTheme = oldTheme;
            _settings.UiTextScale = oldScale;
            try
            {
                // Revertir selección y vista; no tocar el análisis activo.
                if (ActiveTheme != oldTheme) ApplyAppearancePalette(oldTheme);
                if (Math.Abs(_appliedTextScale - oldScale) >= .001)
                    ApplyAppearanceTextScale();
                _themeOption.SelectedItem = AppearanceCatalog.Find(oldTheme);
                _textScaleOption.SelectedItem = oldScale;
            }
            catch (Exception rollbackError)
            {
                CrashDiagnostics.Record("reversion-tema", rollbackError);
            }
            if (_optionsStatus is not null)
                _optionsStatus.Text = "No se pudo aplicar la apariencia; el análisis no se ha detenido.";
            MessageBox.Show(this,
                "No se ha podido cambiar la apariencia. Se ha conservado la configuración anterior " +
                "y PalClam no ha cancelado el análisis.\n\nDiagnóstico local: " + CrashDiagnostics.LogPath,
                "PalClam · Apariencia", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { _applyingAppearanceChange = false; }
    }

    private void ApplyAppearanceTextScale()
    {
        var scale = AppearanceCatalog.NormalizeScaleValue(_settings.UiTextScale);
        if (Math.Abs(scale - _appliedTextScale) < .001) return;
        // Para los controles que heredan FontSize (combos, opciones, ventanas internas).
        FontSize = 13 * scale;
        var visited = new HashSet<DependencyObject>();
        if (Content is DependencyObject content) ScaleTree(content, scale, visited);
        foreach (var page in _pages.Values) ScaleTree(page, scale, visited);
        _appliedTextScale = scale;
        _reflowDashboard?.Invoke();
        _floatingScanWindow?.ApplyAppearance(Surface, Ink, Purple, PurplePale, Outline, scale);
        // Las subventanas se crean independientemente: no alterar su estilo al cambiar la página.
    }

    private static void ScaleTree(DependencyObject root, double scale, HashSet<DependencyObject> visited)
    {
        if (!visited.Add(root)) return;
        if (root is TextBlock t)
        {
            ScaleFont(root, TextBlock.FontSizeProperty, t.FontSize, scale);
        }
        else if (root is System.Windows.Controls.Control c)
        {
            ScaleFont(root, System.Windows.Controls.Control.FontSizeProperty, c.FontSize, scale);
        }
        // Árbol lógico: incluye páginas preparadas pero no mostradas y contenido de botones.
        // Evitar recorrer un enumerador vivo mientras WPF actualiza una lista
        // virtualizada o repinta resultados. La copia es pequeña en cada nivel.
        var children = LogicalTreeHelper.GetChildren(root)
            .OfType<DependencyObject>().ToArray();
        foreach (var child in children)
            ScaleTree(child, scale, visited);
    }

    private static void ScaleFont(DependencyObject element, DependencyProperty fontProperty,
        double current, double scale)
    {
        // No establecer el tamaño local a controles que heredan el valor (evita doble escala).
        var original = (double)element.GetValue(OriginalTextSizeProperty);
        if (double.IsNaN(original))
        {
            if (element.ReadLocalValue(fontProperty) == DependencyProperty.UnsetValue) return;
            original = current;
            element.SetValue(OriginalTextSizeProperty, original);
        }
        element.SetValue(fontProperty, Math.Max(8, Math.Round(original * scale, 1)));
    }


}
