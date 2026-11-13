// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private Button? _quickThemeButton;
    private TextBlock? _quickThemeName;

    /// <summary>
    /// Selector compacto y accesible en la cabecera: los siete temas se aplican
    /// mediante el ComboBox existente de Opciones, sin crear otra persistencia.
    /// </summary>
    private Button CreateQuickThemeButton()
    {
        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Children.Add(new Border
        {
            Width = 19, Height = 19,
            Background = Purple,
            BorderBrush = Surface,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(10),
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        });

        var labels = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        labels.Children.Add(new TextBlock
        {
            Text = "TEMA DE COLOR",
            FontSize = 8.7,
            FontWeight = FontWeights.Bold,
            Foreground = Muted
        });
        _quickThemeName = new TextBlock
        {
            FontSize = 11.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ink,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 128
        };
        labels.Children.Add(_quickThemeName);
        content.Children.Add(labels);
        content.Children.Add(new TextBlock
        {
            Text = "⌄",
            FontSize = 17,
            Foreground = Purple,
            Margin = new Thickness(8, 0, 0, 2),
            VerticalAlignment = VerticalAlignment.Center
        });

        var button = new Button
        {
            Content = content,
            MinHeight = 39,
            Padding = new Thickness(11, 5, 11, 5),
            Background = PurplePale,
            BorderBrush = Outline,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0),
            HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left
        };
        RoundButton(button, 13);
        _quickThemeButton = button;
        button.Click += (_, _) => ShowQuickThemeMenu();
        UpdateQuickThemeButton();
        return button;
    }

    private void ShowQuickThemeMenu()
    {
        if (_quickThemeButton is null) return;

        var menu = new System.Windows.Controls.ContextMenu
        {
            PlacementTarget = _quickThemeButton,
            Placement = PlacementMode.Bottom,
            Background = Surface,
            BorderBrush = Outline,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(5),
            MinWidth = 215,
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                Opacity = 0.13,
                BlurRadius = 16,
                ShadowDepth = 3
            }
        };
        foreach (var palette in AppearanceCatalog.Themes)
        {
            var itemContent = new StackPanel { Orientation = Orientation.Horizontal };
            itemContent.Children.Add(new Border
            {
                Width = 19, Height = 19,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(palette.Accent)!),
                BorderBrush = Outline,
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 11, 0)
            });
            itemContent.Children.Add(new TextBlock
            {
                Text = palette.Name,
                FontSize = 12 * AppearanceCatalog.NormalizeScaleValue(_settings.UiTextScale),
                FontWeight = palette.Id == ActiveTheme ? FontWeights.Bold : FontWeights.Normal,
                Foreground = Ink,
                VerticalAlignment = VerticalAlignment.Center
            });
            var item = new System.Windows.Controls.MenuItem
            {
                Header = itemContent,
                IsCheckable = true,
                IsChecked = palette.Id == ActiveTheme,
                MinHeight = 35 * AppearanceCatalog.NormalizeScaleValue(_settings.UiTextScale),
                Padding = new Thickness(7, 4, 8, 4),
                ToolTip = "Aplicar el tema " + palette.Name
            };
            System.Windows.Automation.AutomationProperties.SetName(item, "Cambiar tema a " + palette.Name);
            item.Click += (_, _) => SelectQuickTheme(palette);
            menu.Items.Add(item);
        }
        menu.Items.Add(new Separator());
        var more = new System.Windows.Controls.MenuItem
        {
            Header = "⚙  Más opciones de apariencia…",
            Foreground = Purple,
            Padding = new Thickness(10, 8, 10, 8)
        };
        more.Click += (_, _) => OpenOptionsTab(1);
        menu.Items.Add(more);
        menu.IsOpen = true;
    }

    private void SelectQuickTheme(AppearancePalette palette)
    {
        if (_themeOption is not null)
        {
            // Provoca OnAppearanceChanged: guarda la selección y respeta la escala actual.
            _themeOption.SelectedItem = palette;
        }
        else
        {
            // Respaldo para una cabecera cargada antes que la página Opciones.
            _settings.UiTheme = palette.Id;
            ApplyAppearancePalette(palette.Id);
            SaveSettings();
        }
        UpdateQuickThemeButton();
    }

    private void UpdateQuickThemeButton()
    {
        var theme = AppearanceCatalog.Find(ActiveTheme);
        if (_quickThemeName is not null)
            _quickThemeName.Text = theme.Name;
        if (_quickThemeButton is not null)
        {
            _quickThemeButton.ToolTip = "Tema activo: " + theme.Name + ". Cambiar color de PalClam";
            System.Windows.Automation.AutomationProperties.SetName(_quickThemeButton,
                "Cambiar tema visual. Actual: " + theme.Name);
        }
    }
}
