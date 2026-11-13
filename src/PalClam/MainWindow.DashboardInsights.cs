// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PalClam.Infrastructure;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private TextBlock? _overviewScanSummary;
    private TextBlock? _overviewLatestScan;
    private TextBlock? _overviewPerformance;
    private TextBlock? _overviewNoticeSummary;
    private TextBlock? _overviewLoadStatus;
    private StackPanel? _overviewScanBars;
    private StackPanel? _overviewNoticeRows;
    private TextBlock? _dashboardMachineState;
    private TextBlock? _dashboardDeviceState;
    private bool _overviewRefreshing;
    private DateTimeOffset _overviewLastRefreshed = DateTimeOffset.MinValue;
    private DashboardReadResult? _overviewRead;

    private void BuildSecurityDashboardInsights(StackPanel page)
    {
        var title = new Grid { Margin = new Thickness(2, 17, 2, 9) };
        title.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        title.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        title.Children.Add(T("ACTIVIDAD DE SEGURIDAD", 10, Muted, FontWeights.Bold));
        var openEvents = ActionButton("Ver notificaciones", false, (_, _) => Navigate("alerts"));
        openEvents.MinHeight = 30;
        openEvents.Padding = new Thickness(10, 5, 10, 5);
        Grid.SetColumn(openEvents, 1);
        title.Children.Add(openEvents);
        page.Children.Add(title);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var scansCard = Card(Surface, Outline);
        scansCard.Margin = new Thickness(0, 0, 6, 0);
        scansCard.Padding = new Thickness(15, 13, 15, 14);
        var scansBody = new StackPanel();
        scansBody.Children.Add(T("Análisis registrados", 14, Ink, FontWeights.Bold));
        _overviewScanSummary = T("Consultando historial local…", 11, Muted,
            FontWeights.SemiBold, new Thickness(0, 7, 0, 0));
        _overviewScanSummary.TextWrapping = TextWrapping.Wrap;
        scansBody.Children.Add(_overviewScanSummary);
        _overviewLatestScan = T("Último análisis: no verificado", 11, Muted,
            FontWeights.Normal, new Thickness(0, 7, 0, 0));
        _overviewLatestScan.TextWrapping = TextWrapping.Wrap;
        scansBody.Children.Add(_overviewLatestScan);
        scansBody.Children.Add(T("DURACIÓN · ÚLTIMOS 6", 9, Muted,
            FontWeights.Bold, new Thickness(0, 13, 0, 7)));
        _overviewScanBars = new StackPanel();
        _overviewScanBars.Children.Add(T("Sin métricas disponibles", 11, Muted));
        scansBody.Children.Add(_overviewScanBars);
        _overviewPerformance = T("Rendimiento: pendiente de datos", 10, Muted,
            FontWeights.Normal, new Thickness(0, 10, 0, 0));
        _overviewPerformance.TextWrapping = TextWrapping.Wrap;
        scansBody.Children.Add(_overviewPerformance);
        scansBody.Children.Add(ActionButton("Historial completo", false, (_, _) => Navigate("history")));
        scansCard.Child = scansBody;
        grid.Children.Add(scansCard);

        var noticesCard = Card(Surface, Outline);
        noticesCard.Margin = new Thickness(6, 0, 0, 0);
        noticesCard.Padding = new Thickness(15, 13, 15, 14);
        var noticesBody = new StackPanel();
        noticesBody.Children.Add(T("Alertas recientes", 14, Ink, FontWeights.Bold));
        _overviewNoticeSummary = T("Consultando registros…", 11, Muted,
            FontWeights.SemiBold, new Thickness(0, 7, 0, 0));
        _overviewNoticeSummary.TextWrapping = TextWrapping.Wrap;
        noticesBody.Children.Add(_overviewNoticeSummary);
        _overviewNoticeRows = new StackPanel { Margin = new Thickness(0, 9, 0, 0) };
        _overviewNoticeRows.Children.Add(T("No se han consultado aún los avisos", 11, Muted));
        noticesBody.Children.Add(_overviewNoticeRows);
        _overviewLoadStatus = T("Datos locales: no se envían a Internet.", 10, Muted,
            FontWeights.Normal, new Thickness(0, 11, 0, 0));
        _overviewLoadStatus.TextWrapping = TextWrapping.Wrap;
        noticesBody.Children.Add(_overviewLoadStatus);
        noticesBody.Children.Add(ActionButton("Revisar avisos", false, (_, _) => Navigate("alerts")));
        noticesCard.Child = noticesBody;
        Grid.SetColumn(noticesCard, 1);
        grid.Children.Add(noticesCard);
        page.Children.Add(grid);

        bool? compact = null;
        void Reflow()
        {
            var small = grid.ActualWidth < 670 || _settings.UiTextScale > 1.15;
            if (compact == small) return;
            compact = small;
            grid.ColumnDefinitions[1].Width = small ? new GridLength(0) :
                new GridLength(1, GridUnitType.Star);
            Grid.SetColumn(noticesCard, small ? 0 : 1);
            Grid.SetRow(noticesCard, small ? 1 : 0);
            scansCard.Margin = small ? new Thickness(0) : new Thickness(0, 0, 6, 0);
            noticesCard.Margin = small ? new Thickness(0, 11, 0, 0) :
                new Thickness(6, 0, 0, 0);
        }
        grid.Loaded += (_, _) => Reflow();
        grid.SizeChanged += (_, _) => Reflow();
    }

    private async Task RefreshDashboardInsightsAsync(bool force = false)
    {
        if (_overviewRefreshing || _overviewScanSummary is null) return;
        if (!force && DateTimeOffset.UtcNow - _overviewLastRefreshed < TimeSpan.FromSeconds(20))
            return;
        if (!force && (!_pages.TryGetValue("dashboard", out var page) ||
            !ReferenceEquals(_pageHost.Content, page))) return;
        _overviewRefreshing = true;
        try
        {
            var now = DateTimeOffset.UtcNow;
            var history = System.IO.Path.Combine(
                PalClam.Services.AppPaths.Root, "history.jsonl");
            var state = await Task.Run(() => DashboardOverviewReader.Read(
                history, PersonalGuardFolder, SharedGuardFolder, SecurityReviewedPath, now));
            if (!IsLoaded) return;
            _overviewRead = state;
            _overviewLastRefreshed = DateTimeOffset.UtcNow;
            RenderDashboardOverview(state.Overview);
            UpdateDashboardPeripheralStates();
        }
        catch (Exception ex)
        {
            if (_overviewLoadStatus is not null)
                _overviewLoadStatus.Text = "No se pudo completar la consulta: " + ex.GetType().Name +
                    ". Abre Diagnóstico para investigar.";
        }
        finally { _overviewRefreshing = false; }
    }

    private void RenderDashboardOverview(DashboardOverview overview)
    {
        if (_overviewScanSummary is null || _overviewScanBars is null ||
            _overviewNoticeRows is null) return;
        _overviewScanSummary.Text = overview.ScanCount == 0 ? "Sin análisis recientes registrados" :
            $"Últimos {overview.ScanCount} · {overview.DetectionsInScans} detecciones comunicadas · " +
            $"{overview.IncompleteScans} no concluyentes";
        _overviewScanSummary.Foreground = overview.DetectionsInScans > 0 ? Red :
            overview.IncompleteScans > 0 ? Amber : Ink;
        var latest = overview.RecentScans.FirstOrDefault();
        _overviewLatestScan!.Text = latest is null ? "Último análisis: sin datos" :
            "Último: " + latest.CompletedAt.ToLocalTime().ToString("dd/MM HH:mm") +
            " · " + latest.Outcome + " · " + latest.Seconds + " s";
        _overviewScanBars.Children.Clear();
        var recent = overview.RecentScans.Take(6).Reverse().ToArray();
        var max = recent.Length == 0 ? 0 : recent.Max(x => Math.Max(0, x.Seconds));
        if (recent.Length == 0)
            _overviewScanBars.Children.Add(T("Sin duración registrada", 11, Muted));
        foreach (var run in recent)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(67) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            row.Children.Add(T(run.CompletedAt.ToLocalTime().ToString("dd/MM HH:mm"), 9, Muted));
            var bar = new ProgressBar { Height = 8, Minimum = 0, Maximum = 100,
                Value = max > 0 ? 100d * Math.Max(0, run.Seconds) / max : 0,
                Foreground = run.Detections > 0 ? Red : run.Errors > 0 ? Amber : Purple,
                Background = PurplePale, BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(7, 0, 7, 0) };
            Grid.SetColumn(bar, 1);
            row.Children.Add(bar);
            var seconds = T(run.Seconds + " s", 10, Muted);
            seconds.TextAlignment = TextAlignment.Right;
            Grid.SetColumn(seconds, 2);
            row.Children.Add(seconds);
            _overviewScanBars.Children.Add(row);
        }
        var metrics = overview.LastScanWithMetrics;
        _overviewPerformance!.Text = metrics is null ?
            "Rendimiento: sin muestras comparables del motor." :
            $"Último muestreo: CPU media {metrics.AverageEngineCpuPercent:0.0}% del equipo · " +
            $"RAM pico {metrics.PeakEngineMemoryBytes / 1048576d:0} MiB · " +
            $"{metrics.PerformanceSampleCount} muestras. No equivale a lectura física del disco.";
        _overviewNoticeSummary!.Text =
            $"Hasta 80 avisos · {overview.NoticeThreats} detecciones · " +
            $"{overview.NoticeWarnings} advertencias · {overview.NoticeErrors} errores · " +
            $"{overview.UnreviewedNotices} sin revisar";
        _overviewNoticeSummary.Foreground = overview.NoticeThreats > 0 ? Red :
            overview.NoticeErrors + overview.NoticeWarnings > 0 ? Amber : Ink;
        _overviewNoticeRows.Children.Clear();
        if (overview.RecentNotices.Count == 0)
            _overviewNoticeRows.Children.Add(T("Sin incidencias registradas en las fuentes consultadas.",
                11, Muted));
        foreach (var n in overview.RecentNotices)
        {
            var item = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            var accent = n.Level switch
            {
                SecurityNoticeLevel.Threat => Red,
                SecurityNoticeLevel.Error => Amber,
                SecurityNoticeLevel.Warning => Amber,
                _ => Muted
            };
            item.Children.Add(T(n.DisplayTime + " · " + n.DisplayLevel + " · " + n.DisplaySource,
                10, accent, FontWeights.SemiBold));
            var detail = T(n.DisplayDetail, 10, Muted);
            detail.MaxWidth = 540;
            detail.TextTrimming = TextTrimming.CharacterEllipsis;
            detail.TextWrapping = TextWrapping.NoWrap;
            detail.ToolTip = n.DisplayDetail;
            item.Children.Add(detail);
            _overviewNoticeRows.Children.Add(item);
        }
        _overviewLoadStatus!.Text = overview.HasPartialData ?
            "Datos parciales: algún registro es ilegible, incompleto o supera el límite de lectura. " +
            "Consulta el centro de notificaciones para más detalles." :
            "Registro local limitado; pueden existir eventos anteriores no incluidos. " +
            "Las detecciones registradas no implican bloqueo del archivo.";
        _overviewLoadStatus.Foreground = overview.HasPartialData ? Amber : Muted;
    }

    private void UpdateDashboardPeripheralStates()
    {
        if (_dashboardMachineState is not null)
        {
            var m = _overviewRead;
            if (m is null || m.MachineInstalled is null)
            {
                _dashboardMachineState.Text = "No verificado";
                _dashboardMachineState.Foreground = Amber;
            }
            else if (m.MachineInstalled == false)
            {
                _dashboardMachineState.Text = "Opcional · no instalado";
                _dashboardMachineState.Foreground = Muted;
            }
            else if (m.Machine is null || !m.Machine.IsFresh(DateTimeOffset.UtcNow))
            {
                _dashboardMachineState.Text = "Sin estado reciente";
                _dashboardMachineState.Foreground = Amber;
            }
            else
            {
                _dashboardMachineState.Text = m.Machine.IsHealthy(DateTimeOffset.UtcNow) ?
                    "Vigilancia reactiva · sin incidencias" : "Revisar servicio degradado";
                _dashboardMachineState.Foreground = m.Machine.IsHealthy(DateTimeOffset.UtcNow)
                    ? Green : Amber;
            }
        }
        if (_dashboardDeviceState is not null)
        {
            var usb = _settings.UsbWatchEnabled && _removableTimer?.IsEnabled == true;
            var downloads = _settings.DownloadGuardEnabled && _guardService?.IsRunning == true &&
                _guardService.WatchedFolders.Any(folder =>
                    DownloadGuardPolicy.IsWithin(DownloadGuardPolicy.DownloadsFolder, folder));
            _dashboardDeviceState.Text = usb && downloads ? "USB y Descargas en vigilancia" :
                usb ? "Solo USB Watch activo" : downloads ? "Solo Descargas en Guard" :
                _settings.UsbWatchEnabled || _settings.DownloadGuardEnabled
                    ? "Activados, sin confirmar vigilancia" : "Opcionales · no activados";
            _dashboardDeviceState.Foreground = usb || downloads ? Green :
                _settings.UsbWatchEnabled || _settings.DownloadGuardEnabled ? Amber : Muted;
        }
    }
}
