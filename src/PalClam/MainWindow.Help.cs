// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using System.Windows.Controls;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private void BuildHelpPage(StackPanel page)
    {
        var introduction = Card(PurplePale, B("#E1D9F5"));
        introduction.Margin = new Thickness(0, 0, 0, 16);
        var opening = new StackPanel();
        opening.Children.Add(LT("Help.Eyebrow", 10, Purple, FontWeights.Bold,
            new Thickness(0, 0, 0, 8)));
        opening.Children.Add(LT("Help.Title", 25, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 9)));
        opening.Children.Add(LT("Help.Introduction",
            13, Slate, FontWeights.Normal, new Thickness(0, 0, 0, 12)));
        var introActions = new WrapPanel();
        introActions.Children.Add(LocalizedAction("Help.Scan", true, (_, _) => Navigate("scan")));
        introActions.Children.Add(LocalizedAction("Help.About", false, (_, _) => Navigate("about")));
        opening.Children.Add(introActions);
        introduction.Child = opening;
        page.Children.Add(introduction);

        page.Children.Add(LT("Help.FirstSteps", 18, Ink, FontWeights.Bold,
            new Thickness(0, 3, 0, 9)));
        var steps = new[]
        {
            ("1. Motor y firmas", "Abre Motor y firmas. Comprueba que ClamAV esté disponible y la base local accesible; actualiza las firmas si procede.", "signatures"),
            ("2. Primera revisión", "En Análisis de archivos, elige Rápido o Personalizado. Una prueba incompleta nunca se considera limpia.", "scan"),
            ("3. Estado de protección", "Consulta Security Health 360: diferencia entre componentes instalados, activados, degradados y pendientes.", "health"),
            ("4. Revisar alertas", "Examina resultados e historial; decide manualmente qué aislar, reanalizar, restaurar o eliminar.", "quarantine")
        };
        foreach (var step in steps)
            page.Children.Add(BuildHelpFeatureCard(new HelpFeature(step.Item1, step.Item2, step.Item3), Surface));
        page.Children.Add(LT("Help.AppearanceTitle", 16, Ink, FontWeights.Bold,
            new Thickness(0, 11, 0, 8)));
        page.Children.Add(LT("Help.AppearanceText",
            12, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 13)));
        page.Children.Add(T("Ventajas principales", 18, Ink, FontWeights.Bold,
            new Thickness(0, 3, 0, 9)));
        page.Children.Add(T("Estas ventajas se refieren a PalClam; algunos proveedores externos " +
            "pueden aplicar condiciones o tarifas propias.", 12, Muted,
            FontWeights.Normal, new Thickness(0, 0, 0, 12)));
        foreach (var feature in HelpContentCatalog.Advantages)
            page.Children.Add(BuildHelpFeatureCard(feature, PurplePale));

        page.Children.Add(T("Características y herramientas", 18, Ink, FontWeights.Bold,
            new Thickness(0, 17, 0, 9)));
        page.Children.Add(T("Accede a cada función desde esta guía, sin acciones automáticas " +
            "sobre tus archivos.", 12, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 12)));
        foreach (var feature in HelpContentCatalog.Capabilities)
            page.Children.Add(BuildHelpFeatureCard(feature, Surface));

        var important = Card(AmberPale, B("#EDC979"));
        important.Margin = new Thickness(0, 18, 0, 10);
        var boundaries = new StackPanel();
        boundaries.Children.Add(T("Lo que PalClam todavía NO hace", 17, Amber,
            FontWeights.Bold, new Thickness(0, 0, 0, 10)));
        foreach (var limitation in HelpContentCatalog.Limitations)
            boundaries.Children.Add(T("• " + limitation, 12, Ink, FontWeights.Normal,
                new Thickness(0, 0, 0, 9)));
        important.Child = boundaries;
        page.Children.Add(important);
        var bottomActions = new WrapPanel { Margin = new Thickness(0, 3, 0, 8) };
        bottomActions.Children.Add(ActionButton("Consejos de seguridad", false,
            (_, _) => Navigate("advice")));
        bottomActions.Children.Add(ActionButton("Acerca de y avisos legales", false,
            (_, _) => Navigate("about")));
        page.Children.Add(bottomActions);
    }

    private Border BuildHelpFeatureCard(HelpFeature feature, System.Windows.Media.Brush background)
    {
        var card = Card(background, Outline);
        card.Margin = new Thickness(0, 0, 0, 9);
        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var description = new StackPanel { Margin = new Thickness(0, 0, 14, 0) };
        description.Children.Add(T(feature.Title, 13, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 4)));
        description.Children.Add(T(feature.Detail, 12, Slate));
        content.Children.Add(description);
        var go = LocalizedAction("Help.ViewFeature", false, (_, _) => Navigate(feature.Page));
        go.VerticalAlignment = VerticalAlignment.Center;
        PalClam.UI.LocalizedUi.Bind(go, FrameworkElement.ToolTipProperty, "Help.GoTo", feature.Title);
        Grid.SetColumn(go, 1);
        content.Children.Add(go);
        card.Child = content;
        return card;
    }
}
