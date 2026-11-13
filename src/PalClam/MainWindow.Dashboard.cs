// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    // Estados vinculados a mediciones reales. Nunca equivale «motor listo» a protección preventiva.
    private TextBlock _dashboardHeroReadiness = null!;
    private TextBlock _dashboardHeroReadinessNote = null!;
    private Button _dashboardMainAction = null!;
    private Action? _reflowDashboard;

    private void BuildProfessionalDashboard(StackPanel page)
    {
        // Una única cabecera: sin repetir el escudo de la barra superior ni siete CTA.
        var hero = new Border
        {
            CornerRadius = new CornerRadius(19),
            Background = _heroGradient = new LinearGradientBrush(C("#182C55"), C("#6843BE"), 12),
            Padding = new Thickness(23, 20, 23, 20),
            Effect = new DropShadowEffect
            {
                Color = C("#243A77"), Opacity = 0.12, BlurRadius = 17, ShadowDepth = 3
            }
        };
        var heroGrid = new Grid();
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(286) });
        heroGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        heroGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var introduction = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        introduction.Children.Add(T("●  CENTRO DE SEGURIDAD", 10, B("#D0DBFF"),
            FontWeights.Bold, new Thickness(0, 0, 0, 8)));
        var heroTitle = T("Seguridad, sin complicaciones", 23, Brushes.White,
            FontWeights.Bold, new Thickness(0, 0, 0, 7));
        heroTitle.TextWrapping = TextWrapping.Wrap;
        introduction.Children.Add(heroTitle);
        var heroDescription = T("Revisa el estado de PalClam, inicia un análisis y gestiona alertas.",
            11, B("#E7ECFF"), FontWeights.Normal, new Thickness(0, 0, 0, 14));
        heroDescription.TextWrapping = TextWrapping.Wrap;
        introduction.Children.Add(heroDescription);
        var heroActions = new WrapPanel();
        _dashboardMainAction = ActionButton("⌕  Iniciar análisis", false, (_, _) => Navigate("scan"));
        SetSemanticButton(_dashboardMainAction, "light");
        _dashboardMainAction.MinWidth = 155;
        heroActions.Children.Add(_dashboardMainAction);
        var quarantine = ActionButton("▣  Cuarentena", false, (_, _) => Navigate("quarantine"));
        SetSemanticButton(quarantine, "glass");
        heroActions.Children.Add(quarantine);
        introduction.Children.Add(heroActions);
        heroGrid.Children.Add(introduction);

        var readiness = new Border
        {
            Background = B("#304A82"), CornerRadius = new CornerRadius(14),
            Padding = new Thickness(16, 14, 16, 14), MinHeight = 105,
            Margin = new Thickness(15, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var readinessContent = new StackPanel();
        readinessContent.Children.Add(T("ESTADO DEL MOTOR", 9, B("#D0DBFF"),
            FontWeights.Bold, new Thickness(0, 0, 0, 8)));
        _dashboardHeroReadiness = T("Comprobando…", 15, Brushes.White,
            FontWeights.Bold, new Thickness(0, 0, 0, 5));
        _dashboardHeroReadinessNote = T("Datos locales de ClamAV", 10, B("#E7ECFF"));
        // «Repetir análisis» y estados largos deben ocupar dos líneas si hace falta.
        _dashboardHeroReadiness.TextWrapping = TextWrapping.Wrap;
        _dashboardHeroReadiness.TextTrimming = TextTrimming.None;
        _dashboardHeroReadiness.MinHeight = 0;
        _dashboardHeroReadinessNote.TextWrapping = TextWrapping.Wrap;
        readinessContent.Children.Add(_dashboardHeroReadiness);
        readinessContent.Children.Add(_dashboardHeroReadinessNote);
        readiness.Child = readinessContent;
        Grid.SetColumn(readiness, 1);
        heroGrid.Children.Add(readiness);
        hero.Child = heroGrid;
        page.Children.Add(BuildDashboardActiveScan());
        page.Children.Add(hero);

        // Una única banda de estado enlazada a _scanRunning, resultados y motor/firmas.
        var strip = new Border
        {
            Background = Surface, BorderBrush = Outline, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(13), Padding = new Thickness(13, 10, 13, 10),
            Margin = new Thickness(0, 12, 0, 0)
        };
        var stripGrid = new Grid();
        stripGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        stripGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        stripGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        stripGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        stripGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _dashboardMoodBorder = new Border
        {
            Width = 34, Height = 34, Background = BluePale,
            CornerRadius = new CornerRadius(10), Margin = new Thickness(0, 0, 12, 0),
            Child = new Ellipse { Width = 12, Height = 12, Fill = Blue,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center }
        };
        stripGrid.Children.Add(_dashboardMoodBorder);
        var stripContent = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        _dashboardMoodText = T("Comprobando el estado", 13, Ink, FontWeights.Bold);
        _dashboardMoodDetail = T("El estado se calcula con los datos locales disponibles.", 10, Muted);
        _dashboardMoodText.TextWrapping = TextWrapping.Wrap;
        _dashboardMoodDetail.TextWrapping = TextWrapping.Wrap;
        stripContent.Children.Add(_dashboardMoodText);
        stripContent.Children.Add(_dashboardMoodDetail);
        Grid.SetColumn(stripContent, 1);
        stripGrid.Children.Add(stripContent);
        var diagnosis = ActionButton("Diagnóstico", false, (_, _) => Navigate("diagnostics"));
        diagnosis.MinWidth = 95;
        diagnosis.Margin = new Thickness(12, 0, 0, 0);
        Grid.SetColumn(diagnosis, 2);
        stripGrid.Children.Add(diagnosis);
        strip.Child = stripGrid;
        page.Children.Add(strip);

        // Datos sobre el último análisis. No mostrar un porcentaje inventado.
        var resultHeader = new Grid { Margin = new Thickness(2, 16, 2, 8) };
        resultHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        resultHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        resultHeader.Children.Add(T("ÚLTIMO ANÁLISIS", 10, Muted, FontWeights.Bold));
        var historyLink = ActionButton("Ver historial", false, (_, _) => Navigate("history"));
        historyLink.MinHeight = 30;
        historyLink.Padding = new Thickness(9, 5, 9, 5);
        historyLink.Margin = new Thickness(0);
        Grid.SetColumn(historyLink, 1);
        resultHeader.Children.Add(historyLink);
        page.Children.Add(resultHeader);
        var counters = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        for (var i = 0; i < 3; i++)
            counters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _status = T("Sin analizar", 17, Ink, FontWeights.Bold);
        _detections = T("—", 17, Ink, FontWeights.Bold);
        _lastAnalysis = T("—", 17, Ink, FontWeights.Bold);
        AddCounter(counters, 0, "ÚLTIMO ESTADO", _status);
        AddCounter(counters, 1, "DETECCIONES", _detections);
        AddCounter(counters, 2, "FECHA DEL ANÁLISIS", _lastAnalysis);
        page.Children.Add(counters);

        var modules = Card(Surface, Outline);
        modules.Padding = new Thickness(15, 13, 15, 14);
        var modulesBody = new StackPanel();
        modulesBody.Children.Add(T("Componentes de seguridad", 14, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 11)));
        var moduleTiles = new Grid();
        for (var i = 0; i < 3; i++)
            moduleTiles.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _dashboardEngineState = T("Comprobando…", 11, Blue, FontWeights.SemiBold);
        _dashboardSignaturesState = T("Comprobando…", 11, Blue, FontWeights.SemiBold);
        _dashboardGuardState = T("Comprobando…", 11, Muted, FontWeights.SemiBold);
        AddModule(moduleTiles, 0, "chip", "Motor ClamAV", _dashboardEngineState, BluePale, Blue);
        AddModule(moduleTiles, 1, "database", "Firmas antivirus", _dashboardSignaturesState, GreenPale, Green);
        AddModule(moduleTiles, 2, "shield", "PalClam Guard", _dashboardGuardState, PurplePale, Purple);
        _dashboardMachineState = T("Pendiente de comprobar", 11, Amber, FontWeights.SemiBold);
        _dashboardDeviceState = T("Pendiente de comprobar", 11, Muted, FontWeights.SemiBold);
        AddModule(moduleTiles, 3, "shield", "Servicio del equipo", _dashboardMachineState, AmberPale, Amber);
        AddModule(moduleTiles, 4, "database", "USB y Descargas", _dashboardDeviceState, BluePale, Blue);
        modulesBody.Children.Add(moduleTiles);
        modules.Child = modulesBody;
        page.Children.Add(modules);
        BuildSecurityDashboardInsights(page);

        // Vista compacta y ampliada SIN destruir tarjetas ni recrear controles en caliente.
        // El cambio de tema no invoca este rediseño; solo cambia los pinceles existentes.
        ConfigureDashboardResponsiveLayout(hero, heroGrid, readiness, counters, moduleTiles,
            diagnosis);

        // Funciones secundarias agrupadas. Mantener el panel respirable.
        var shortcutsHeading = T("ACCESOS DIRECTOS", 10, Muted, FontWeights.Bold,
            new Thickness(2, 15, 0, 8));
        page.Children.Add(shortcutsHeading);
        var shortcuts = new WrapPanel { ItemHeight = double.NaN };
        shortcuts.Children.Add(ActionButton("♥  Salud 360", false, (_, _) => Navigate("health")));
        shortcuts.Children.Add(ActionButton("▣  USB y Descargas", false, (_, _) => Navigate("devices")));
        shortcuts.Children.Add(ActionButton("↻  Actualizar firmas", false, (_, _) => Navigate("signatures")));
        shortcuts.Children.Add(ActionButton("!  Centro de notificaciones", false, (_, _) => Navigate("alerts")));
        shortcuts.Children.Add(ActionButton("✉  Revisar correo", false, (_, _) => Navigate("mail")));
        shortcuts.Children.Add(ActionButton("✦  PalClam AI", false, (_, _) => Navigate("ai")));
        shortcuts.Children.Add(ActionButton("?  Ayuda", false, (_, _) => Navigate("help")));
        page.Children.Add(shortcuts);
        var disclaimer = T("PalClam realiza análisis bajo demanda. Guard observa cambios; no bloquea malware " +
            "antes de ejecutarse ni sustituye Microsoft Defender.", 10, Muted, FontWeights.Normal,
            new Thickness(3, 8, 3, 0));
        disclaimer.TextWrapping = TextWrapping.Wrap;
        page.Children.Add(disclaimer);
    }

    private void ConfigureDashboardResponsiveLayout(Border hero, Grid heroGrid, Border readiness,
        Grid lastScanMetrics, Grid modules, Button diagnosisButton)
    {
        // Las filas vacías miden 0px. Permite apilar tarjetas con el mismo árbol visual.
        for (var i = 0; i < 5; i++)
        {
            if (i < 3) lastScanMetrics.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            modules.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        DashboardWidthMode? lastMode = null;
        _reflowDashboard = () =>
        {
            // El tamaño de texto 130% necesita más anchura para los mismos componentes.
            var mode = DashboardLayoutPolicy.ForWidth(hero.ActualWidth, _settings.UiTextScale);
            if (lastMode == mode) return;
            lastMode = mode;
            var stackedHero = mode == DashboardWidthMode.Compact;
            heroGrid.ColumnDefinitions[1].Width = stackedHero ? new GridLength(0) : new GridLength(286);
            Grid.SetColumn(readiness, stackedHero ? 0 : 1);
            Grid.SetRow(readiness, stackedHero ? 1 : 0);
            readiness.Margin = stackedHero ? new Thickness(0, 14, 0, 0) :
                new Thickness(15, 0, 0, 0);
            // FIX2: una anchura fija igual a la columna MAS un margen izquierdo
            // desbordaba el Grid y recortaba el principio de «Repetir análisis».
            // Dejar que WPF calcule la anchura útil después de descontar el margen.
            readiness.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            readiness.ClearValue(FrameworkElement.WidthProperty);

            // A anchos intermedios caben las métricas, pero no las tres tarjetas con texto largo.
            ArrangeDashboardRow(lastScanMetrics, mode == DashboardWidthMode.Compact);
            ArrangeDashboardModules(modules, mode == DashboardWidthMode.Wide ? 3 :
                mode == DashboardWidthMode.Medium ? 2 : 1);

            // En ventanas estrechas el boton no roba espacio al mensaje de estado.
            Grid.SetColumn(diagnosisButton, stackedHero ? 1 : 2);
            Grid.SetRow(diagnosisButton, stackedHero ? 1 : 0);
            diagnosisButton.HorizontalAlignment = stackedHero ? System.Windows.HorizontalAlignment.Left :
                System.Windows.HorizontalAlignment.Stretch;
            diagnosisButton.Margin = stackedHero ? new Thickness(0, 9, 0, 0) :
                new Thickness(12, 0, 0, 0);
        };
        hero.SizeChanged += (_, _) => _reflowDashboard?.Invoke();
        hero.Loaded += (_, _) => _reflowDashboard?.Invoke();
        _reflowDashboard();
    }

    // Cinco componentes en 3/2/1 columnas, sin imponer anchuras fijas ni cortar etiquetas.
    private static void ArrangeDashboardModules(Grid grid, int columns)
    {
        for (var i = 0; i < grid.Children.Count; i++)
        {
            if (grid.Children[i] is not FrameworkElement item) continue;
            var col = i % columns;
            Grid.SetColumn(item, col);
            Grid.SetRow(item, i / columns);
            item.Margin = new Thickness(col == 0 ? 0 : 5, 0, col == columns - 1 ? 0 : 5, 8);
        }
        for (var i = 0; i < grid.ColumnDefinitions.Count; i++)
            grid.ColumnDefinitions[i].Width = i < columns ?
                new GridLength(1, GridUnitType.Star) : new GridLength(0);
    }

    private static void ArrangeDashboardRow(Grid grid, bool stacked)
    {
        for (var i = 0; i < grid.Children.Count; i++)
        {
            if (grid.Children[i] is not FrameworkElement item) continue;
            Grid.SetColumn(item, stacked ? 0 : i);
            Grid.SetRow(item, stacked ? i : 0);
            item.Margin = stacked ? new Thickness(0, 0, 0, i == grid.Children.Count - 1 ? 0 : 9) :
                new Thickness(i == 0 ? 0 : 5, 0, i == grid.Children.Count - 1 ? 0 : 5, 0);
        }
        grid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
        for (var i = 1; i < grid.ColumnDefinitions.Count; i++)
            grid.ColumnDefinitions[i].Width = stacked ? new GridLength(0) :
                new GridLength(1, GridUnitType.Star);
    }

    private static System.Windows.Shapes.Path CreateDashboardModuleIcon(string kind, Brush color)
    {
        // Geometrías vectoriales WPF, sin fuentes de símbolos ni dependencias externas.
        var geometry = kind switch
        {
            "chip" => "M6,6 L18,6 18,18 6,18 Z M9,9 L15,9 15,15 9,15 Z " +
                      "M9,2 L9,6 M15,2 L15,6 M9,18 L9,22 M15,18 L15,22 " +
                      "M2,9 L6,9 M2,15 L6,15 M18,9 L22,9 M18,15 L22,15",
            "database" => "M4,6 C4,2 20,2 20,6 C20,10 4,10 4,6 Z " +
                          "M4,6 L4,18 C4,22 20,22 20,18 L20,6 " +
                          "M4,12 C4,16 20,16 20,12",
            _ => "M12,2 L20,6 L20,12 C20,17 16,20 12,22 " +
                 "C8,20 4,17 4,12 L4,6 Z M8,12 L11,15 L16,10"
        };
        return new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse(geometry), Stroke = color, StrokeThickness = 1.8,
            StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round, Stretch = Stretch.Uniform,
            Width = 18, Height = 18, HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }
}
