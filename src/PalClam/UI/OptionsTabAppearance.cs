// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using PalClam.Services;
using WpfControl = System.Windows.Controls.Control;

namespace PalClam.UI;

/// <summary>
/// Apariencia de las pestañas de Opciones. La plantilla no contiene NINGÚN pincel de
/// la paleta mutable: WPF sella estilos y puede congelar valores Freezable.
/// Los colores se asignan localmente en instantáneas congeladas y se reemplazan
/// al cambiar tema/selección/hover, sin reinstalar estilos ni reconstruir vistas.
/// </summary>
internal static class OptionsTabAppearance
{
    internal static Style CreateStyle()
    {
        var style = new Style(typeof(TabItem));
        style.Setters.Add(new Setter(WpfControl.FontSizeProperty, 11.5));
        style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 7, 8)));
        style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 45.0));
        style.Setters.Add(new Setter(WpfControl.CursorProperty, Cursors.Hand));
        style.Setters.Add(new Setter(WpfControl.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(WpfControl.BorderThicknessProperty, new Thickness(1)));

        var active = new Trigger { Property = TabItem.IsSelectedProperty, Value = true };
        active.Setters.Add(new Setter(WpfControl.FontWeightProperty, FontWeights.Bold));
        style.Triggers.Add(active);

        var keyboardFocus = new Trigger { Property = UIElement.IsKeyboardFocusWithinProperty, Value = true };
        keyboardFocus.Setters.Add(new Setter(WpfControl.BorderThicknessProperty, new Thickness(2)));
        style.Triggers.Add(keyboardFocus);

        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
        border.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness")
        { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        border.SetBinding(Border.BackgroundProperty, new Binding("Background")
        { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        border.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush")
        { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });

        var grid = new FrameworkElementFactory(typeof(Grid));
        var header = new FrameworkElementFactory(typeof(ContentPresenter));
        header.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        header.SetValue(FrameworkElement.MarginProperty, new Thickness(11, 8, 11, 8));
        header.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
        header.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        grid.AppendChild(header);

        var indicator = new FrameworkElementFactory(typeof(Border));
        indicator.Name = "SelectedIndicator";
        indicator.SetValue(FrameworkElement.HeightProperty, 3.0);
        indicator.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Bottom);
        indicator.SetValue(FrameworkElement.MarginProperty, new Thickness(12, 0, 12, 0));
        indicator.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
        indicator.SetBinding(Border.BackgroundProperty, new Binding("BorderBrush")
        { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        grid.AppendChild(indicator);
        border.AppendChild(grid);
        var template = new ControlTemplate(typeof(TabItem)) { VisualTree = border };
        var selected = new Trigger { Property = TabItem.IsSelectedProperty, Value = true };
        selected.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "SelectedIndicator"));
        template.Triggers.Add(selected);
        style.Setters.Add(new Setter(WpfControl.TemplateProperty, template));
        return style;
    }

    internal static void Apply(TabItem tab, AppearancePalette palette)
    {
        // No añadir nunca aquí colores tomados del registro mutable de MainWindow.
        // Cada pincel es INDEPENDIENTE y congelado expresamente: WPF puede sellar
        // la plantilla sin inmovilizar la paleta compartida de las demás pantallas.
        var highlighted = tab.IsSelected || tab.IsMouseOver;
        SetBrush(tab, WpfControl.BackgroundProperty, highlighted ? palette.AccentPale : palette.Surface);
        SetBrush(tab, WpfControl.ForegroundProperty, highlighted ? palette.Accent : palette.Muted);
        SetBrush(tab, WpfControl.BorderBrushProperty,
            tab.IsSelected || tab.IsKeyboardFocusWithin ? palette.Accent : palette.Outline);
    }

    private static void SetBrush(TabItem tab, DependencyProperty property, string hex)
    {
        var color = (Color)System.Windows.Media.ColorConverter.ConvertFromString(hex)!;
        // Evitar modificaciones del árbol visual y nuevas asignaciones si no varió.
        if (tab.GetValue(property) is SolidColorBrush current && current.Color == color && current.IsFrozen)
            return;
        var snapshot = new SolidColorBrush(color);
        snapshot.Freeze();
        tab.SetValue(property, snapshot);
    }
}
