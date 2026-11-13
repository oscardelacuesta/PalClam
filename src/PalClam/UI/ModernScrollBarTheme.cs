// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System;
using System.Windows;
using System.Windows.Media;
using PalClam.Services;

namespace PalClam.UI;

/// <summary>
/// Apariencia de barras verticales y horizontales, sin cambiar ScrollViewer,
/// Track ni el comportamiento de rueda/teclado. Los cuatro pinceles son recursos
/// dinámicos; cambiar tema NO reconstruye las vistas ni interrumpe un análisis.
/// </summary>
internal static class ModernScrollBarTheme
{
    internal const string TrackKey = "PalClam.Scroll.Track";
    internal const string ThumbKey = "PalClam.Scroll.Thumb";
    internal const string HoverKey = "PalClam.Scroll.Hover";
    internal const string PressedKey = "PalClam.Scroll.Pressed";
    internal const string SurfaceKey = "PalClam.Control.Surface";
    internal const string InkKey = "PalClam.Control.Ink";
    internal const string OutlineKey = "PalClam.Control.Outline";
    internal const string HighlightKey = "PalClam.Control.Highlight";
    internal const string FocusKey = "PalClam.Control.Focus";

    internal static void Install(Application app)
    {
        // El sistema de alto contraste de Windows tiene prioridad.
        if (SystemParameters.HighContrast) return;
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/" + typeof(ModernScrollBarTheme).Assembly.GetName().Name +
                             ";component/UI/ModernScrollBars.xaml", UriKind.Relative)
        });
        Apply(app, AppearanceCatalog.Find(AppearanceCatalog.DefaultId));
    }

    internal static void Apply(Application? app, AppearancePalette palette)
    {
        if (app is null || SystemParameters.HighContrast) return;
        var dark = palette.Id == "oscuro";
        Put(app.Resources, SurfaceKey, palette.Surface);
        Put(app.Resources, InkKey, palette.Ink);
        Put(app.Resources, OutlineKey, palette.Outline);
        Put(app.Resources, HighlightKey, palette.AccentPale);
        Put(app.Resources, FocusKey, palette.Accent);
        Put(app.Resources, TrackKey, dark ? "#202A40" : palette.Sidebar);
        Put(app.Resources, ThumbKey, dark ? "#7484A8" : palette.AccentBorder);
        Put(app.Resources, HoverKey, dark ? "#BBA0F2" : palette.Accent);
        Put(app.Resources, PressedKey, dark ? "#F1E8FF" : palette.AccentText);
    }

    private static void Put(ResourceDictionary resources, string key, string value)
    {
        var color = (Color)System.Windows.Media.ColorConverter.ConvertFromString(value)!;
        if (resources[key] is SolidColorBrush old && old.Color == color) return;
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        // Sustituir recursos evita modificar Freezables sellados por plantillas WPF.
        resources[key] = brush;
    }
}
