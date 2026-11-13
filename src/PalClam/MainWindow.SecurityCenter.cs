// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private readonly ObservableCollection<SecurityNotice> _securityCenterVisible = new();
    private IReadOnlyList<SecurityNotice> _securityCenterAll = Array.Empty<SecurityNotice>();
    private ComboBox? _securitySeverityFilter;
    private ComboBox? _securitySourceFilter;
    private CheckBox? _securityOnlyNew;
    private TextBox? _securitySearch;
    private ListView? _securityEventsView;
    private TextBlock? _securitySummary;
    private TextBlock? _securityLoadInfo;
    private TextBlock? _securitySelectedDetails;
    private bool _securityRefreshInProgress;
    private DateTimeOffset _securityReviewedUntil = DateTimeOffset.MinValue;

    private static string SecurityReviewedPath => System.IO.Path.Combine(
        PalClam.Services.AppPaths.Root, "Notifications", "revisado-hasta.txt");
    private static string PersonalGuardFolder => System.IO.Path.Combine(
        PalClam.Services.AppPaths.Root, "Guard");
    private static string SharedGuardFolder => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "PalClam", "MachineGuard");

    private void BuildSecurityCenterPage(StackPanel page)
    {
        page.Children.Add(T("Centro de notificaciones", 23, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 5)));
        var lead = T("Historial local · Guard personal, USB Watch, anti-ransomware y servicio Windows",
            12, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 12));
        lead.TextWrapping = TextWrapping.Wrap;
        page.Children.Add(lead);

        var intro = Card(PurplePale, PurplePale);
        var explanation = T("Aquí aparecen los registros disponibles; no significa que un archivo se haya " +
            "bloqueado o puesto en cuarentena. El servicio es experimental, puede no estar instalado " +
            "y la pérdida de eventos exige un análisis manual. Marcar como revisadas NO borra registros.",
            11, Ink, FontWeights.Normal);
        explanation.TextWrapping = TextWrapping.Wrap;
        intro.Child = explanation;
        page.Children.Add(intro);

        var filters = Card(Surface, Outline);
        filters.Margin = new Thickness(0, 13, 0, 0);
        var controls = new StackPanel();
        controls.Children.Add(T("Filtrar actividad", 14, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 9)));
        var filterRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
        _securitySeverityFilter = new ComboBox
        {
            ItemsSource = new[] { "Todas", "DETECCIÓN", "Advertencia", "Error", "Información" },
            SelectedIndex = 0, MinWidth = 142, Margin = new Thickness(0, 0, 9, 7),
            Padding = new Thickness(8, 5, 8, 5), BorderBrush = Outline
        };
        _securitySourceFilter = new ComboBox
        {
            ItemsSource = new[] { "Todos", "Guard personal", "Servicio Windows", "USB Watch", "Anti-ransomware" },
            SelectedIndex = 0, MinWidth = 170, Margin = new Thickness(0, 0, 9, 7),
            Padding = new Thickness(8, 5, 8, 5), BorderBrush = Outline
        };
        _securitySearch = new TextBox
        {
            Width = 220, Margin = new Thickness(0, 0, 9, 7),
            Padding = new Thickness(8, 6, 8, 6), BorderBrush = Outline,
            ToolTip = "Buscar por nombre de archivo, ruta o detalle"
        };
        _securityOnlyNew = new CheckBox
        {
            Content = "Solo sin revisar", VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 7), Foreground = Ink
        };
        filterRow.Children.Add(_securitySeverityFilter);
        filterRow.Children.Add(_securitySourceFilter);
        filterRow.Children.Add(_securitySearch);
        filterRow.Children.Add(_securityOnlyNew);
        controls.Children.Add(filterRow);
        _securitySeverityFilter.SelectionChanged += (_, _) => ApplySecurityCenterFilters();
        _securitySourceFilter.SelectionChanged += (_, _) => ApplySecurityCenterFilters();
        _securitySearch.TextChanged += (_, _) => ApplySecurityCenterFilters();
        _securityOnlyNew.Checked += (_, _) => ApplySecurityCenterFilters();
        _securityOnlyNew.Unchecked += (_, _) => ApplySecurityCenterFilters();
        var actions = new WrapPanel();
        actions.Children.Add(ActionButton("↻  Actualizar historial", false,
            async (_, _) => await RefreshSecurityCenterAsync()));
        actions.Children.Add(ActionButton("✓  Marcar como revisadas", false,
            (_, _) => MarkSecurityCenterReviewed()));
        actions.Children.Add(ActionButton("▤  Exportar selección filtrada", false,
            (_, _) => ExportSecurityCenter()));
        controls.Children.Add(actions);
        filters.Child = controls;
        page.Children.Add(filters);

        var history = Card(Surface, Outline);
        history.Margin = new Thickness(0, 13, 0, 0);
        var content = new StackPanel();
        _securitySummary = T("Sin consultar aún", 13, Purple, FontWeights.SemiBold,
            new Thickness(0, 0, 0, 8));
        _securitySummary.TextWrapping = TextWrapping.Wrap;
        content.Children.Add(_securitySummary);
        _securityEventsView = new ListView
        {
            ItemsSource = _securityCenterVisible,
            MinHeight = 230, MaxHeight = 440,
            Background = Surface, BorderBrush = Outline, BorderThickness = new Thickness(1),
            HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch
        };
        var columns = new GridView();
        columns.Columns.Add(new GridViewColumn { Header = "Fecha", Width = 145,
            DisplayMemberBinding = new System.Windows.Data.Binding(nameof(SecurityNotice.DisplayTime)) });
        columns.Columns.Add(new GridViewColumn { Header = "Nivel", Width = 110,
            DisplayMemberBinding = new System.Windows.Data.Binding(nameof(SecurityNotice.DisplayLevel)) });
        columns.Columns.Add(new GridViewColumn { Header = "Origen", Width = 130,
            DisplayMemberBinding = new System.Windows.Data.Binding(nameof(SecurityNotice.DisplaySource)) });
        columns.Columns.Add(new GridViewColumn { Header = "Archivo / detalle", Width = 490,
            DisplayMemberBinding = new System.Windows.Data.Binding(nameof(SecurityNotice.DisplayDetail)) });
        _securityEventsView.View = columns;
        content.Children.Add(_securityEventsView);
        _securitySelectedDetails = T("Selecciona una fila para consultar sus detalles.",
            11, Muted, FontWeights.Normal, new Thickness(0, 12, 0, 5));
        _securitySelectedDetails.TextWrapping = TextWrapping.Wrap;
        content.Children.Add(_securitySelectedDetails);
        _securityEventsView.SelectionChanged += (_, _) =>
        {
            if (_securitySelectedDetails is null) return;
            if (_securityEventsView.SelectedItem is SecurityNotice notice)
                _securitySelectedDetails.Text = notice.DisplayTime + " · " + notice.DisplaySource +
                    " · " + notice.DisplayLevel + "\n" + notice.DisplayDetail;
            else _securitySelectedDetails.Text = "Selecciona una fila para consultar sus detalles.";
        };
        content.Children.Add(ActionButton("✦  Explicar alerta seleccionada con IA (resumen sin datos privados)", false,
            (_, _) => ExplainSelectedSecurityNoticeWithAi()));
        content.Children.Add(ActionButton("Abrir PalClam Guard", false, (_, _) => Navigate("guard")));
        content.Children.Add(ActionButton("▣  Abrir Remediation Center (cuarentena)", false,
            (_, _) => Navigate("quarantine")));
        _securityLoadInfo = T("Se consultan registros locales limitados; no se envían datos a Internet.",
            10, Muted, FontWeights.Normal, new Thickness(0, 10, 0, 0));
        _securityLoadInfo.TextWrapping = TextWrapping.Wrap;
        content.Children.Add(_securityLoadInfo);
        history.Child = content;
        page.Children.Add(history);
    }

    private async Task RefreshSecurityCenterAsync()
    {
        if (_securityRefreshInProgress || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
        _securityRefreshInProgress = true;
        try
        {
            var result = await Task.Run(() => SecurityNoticeFeed.ReadRecent(
                PersonalGuardFolder, SharedGuardFolder, DateTimeOffset.UtcNow));
            if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
            _securityCenterAll = result.Items;
            _securityReviewedUntil = SecurityNoticeReadMarker.Read(SecurityReviewedPath);
            ApplySecurityCenterFilters();
            if (_securityLoadInfo is not null)
            {
                _securityLoadInfo.Text = "Fuentes: Guard personal, USB Watch, anti-ransomware y servicio compartido, " +
                    "incluidos sus registros rotados. Se muestran hasta 400 " +
                    "eventos recientes. " +
                    (result.Limited ? "Lectura parcial por límite de tamaño o cantidad. " : "") +
                    (result.UnreadableFiles > 0 ?
                        $"Registros inaccesibles: {result.UnreadableFiles}. " : "") +
                    (result.InvalidEntries > 0 ?
                        $"Líneas inválidas: {result.InvalidEntries}. " : "") +
                    "No elimina, analiza ni pone archivos en cuarentena.";
            }
        }
        catch (Exception ex)
        {
            if (_securityLoadInfo is not null)
                _securityLoadInfo.Text = "No fue posible cargar el historial: " + ex.Message;
        }
        finally { _securityRefreshInProgress = false; }
    }

    private void ApplySecurityCenterFilters()
    {
        if (_securityEventsView is null || _securitySummary is null) return;
        var selected = SecurityNoticeFeed.Filter(_securityCenterAll,
            _securitySeverityFilter?.SelectedItem as string,
            _securitySourceFilter?.SelectedItem as string,
            _securitySearch?.Text, _securityOnlyNew?.IsChecked == true, _securityReviewedUntil);
        _securityCenterVisible.Clear();
        foreach (var item in selected) _securityCenterVisible.Add(item);
        var newEvents = _securityCenterAll.Count(item => item.When > _securityReviewedUntil);
        var threats = _securityCenterAll.Count(item => item.Level == SecurityNoticeLevel.Threat);
        var warnings = _securityCenterAll.Count(item => item.Level == SecurityNoticeLevel.Warning);
        var errors = _securityCenterAll.Count(item => item.Level == SecurityNoticeLevel.Error);
        _securitySummary.Text = $"Mostrando {selected.Count} de {_securityCenterAll.Count} · " +
            $"Sin revisar: {newEvents} · Detecciones: {threats} · Advertencias: {warnings} · Errores: {errors}";
    }

    private void MarkSecurityCenterReviewed()
    {
        if (!SecurityNoticeReadMarker.TryMark(SecurityReviewedPath, DateTimeOffset.UtcNow))
        {
            MessageBox.Show(this, "No fue posible guardar la marca de revisión.",
                "Centro de notificaciones", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _securityReviewedUntil = SecurityNoticeReadMarker.Read(SecurityReviewedPath);
        ApplySecurityCenterFilters();
    }

    private void ExportSecurityCenter()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Exportar avisos filtrados de PalClam", Filter = "Texto (*.txt)|*.txt",
            DefaultExt = ".txt", FileName = "PalClam-Avisos-" + DateTime.Now.ToString("yyyyMMdd-HHmm")
        };
        if (PalClam.UI.LocalizedDialogs.Show(dialog, this) != true) return;
        try
        {
            var report = new StringBuilder("PalClam 1.1.0 · Historial local de alertas\n" +
                "No equivale a cobertura completa ni implica bloqueo preventivo.\n" +
                "Contiene rutas y datos locales: revisa el contenido antes de compartir.\n\n");
            foreach (var item in _securityCenterVisible)
                report.AppendLine(item.DisplayTime + " | " + item.DisplayLevel + " | " +
                    item.DisplaySource + " | " + item.DisplayDetail.Replace('\r', ' ').Replace('\n', ' '));
            File.WriteAllText(dialog.FileName, report.ToString());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            MessageBox.Show(this, "No se pudo exportar: " + ex.Message,
                "Centro de notificaciones", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
