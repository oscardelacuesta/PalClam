// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private TextBlock _sidebarAdviceMessage = null!;
    private TextBlock _adviceEngine = null!;
    private TextBlock _adviceSignatures = null!;
    private TextBlock _adviceGuard = null!;
    private TextBlock _adviceLastScan = null!;
    private int _adviceIndex;

    private static System.Windows.Controls.Image CharacterPortrait(double width, double height) => new()
    {
        Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Oscar-PalClam.gif", UriKind.Absolute)),
        Width = width, Height = height, Stretch = Stretch.Uniform,
        ToolTip = "Personaje de Óscar de la Cuesta, autor de PalClam"
    };

    private Border BuildCharacterAdviceCard()
    {
        var shell = new Border
        {
            Background = B("#F3EEFC"), BorderBrush = B("#E5D9FA"),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(13),
            Padding = new Thickness(9), Margin = new Thickness(2, 10, 2, 10)
        };
        var content = new StackPanel();
        content.Children.Add(T("UN CONSEJO PARA TI", 10, Purple, FontWeights.Bold,
            new Thickness(0, 0, 0, 6)));
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(66) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var portrait = CharacterPortrait(64, 94);
        portrait.VerticalAlignment = VerticalAlignment.Bottom;
        row.Children.Add(portrait);
        var message = new StackPanel { Margin = new Thickness(7, 4, 0, 0) };
        message.Children.Add(T("PalClam te orienta", 11, Ink, FontWeights.SemiBold));
        _sidebarAdviceMessage = T("", 10, Slate);
        _sidebarAdviceMessage.Margin = new Thickness(0, 5, 0, 0);
        message.Children.Add(_sidebarAdviceMessage);
        Grid.SetColumn(message, 1);
        row.Children.Add(message);
        content.Children.Add(row);
        var buttons = new WrapPanel { Margin = new Thickness(0, 7, 0, 0) };
        var next = new Button
        {
            Content = "↻  Otro consejo", Padding = new Thickness(7, 5, 7, 5),
            BorderThickness = new Thickness(0), Background = Brushes.Transparent,
            Foreground = Purple, FontWeight = FontWeights.SemiBold, FontSize = 10,
            Cursor = Cursors.Hand, ToolTip = "Mostrar otro consejo de seguridad"
        };
        RoundButton(next, 7);
        next.Click += (_, _) => ShowNextAdvice();
        buttons.Children.Add(next);
        var all = new Button
        {
            Content = "Ver todos", Padding = new Thickness(7, 5, 7, 5),
            BorderThickness = new Thickness(0), Background = Brushes.Transparent,
            Foreground = Blue, FontWeight = FontWeights.SemiBold, FontSize = 10,
            Cursor = Cursors.Hand, ToolTip = "Abrir el Centro de recomendaciones"
        };
        RoundButton(all, 7);
        all.Click += (_, _) => Navigate("advice");
        buttons.Children.Add(all);
        content.Children.Add(buttons);
        shell.Child = content;
        _adviceIndex = 0;
        ShowAdviceAtCurrentIndex();
        return shell;
    }

    private void ShowNextAdvice()
    {
        if (SecurityAdviceCatalog.Items.Count == 0) return;
        _adviceIndex = (_adviceIndex + 1) % SecurityAdviceCatalog.Items.Count;
        ShowAdviceAtCurrentIndex();
    }

    private void ShowAdviceAtCurrentIndex()
    {
        if (_sidebarAdviceMessage is null || SecurityAdviceCatalog.Items.Count == 0) return;
        _sidebarAdviceMessage.Text = SecurityAdviceCatalog.Items[_adviceIndex].Title + ". " +
            SecurityAdviceCatalog.Items[_adviceIndex].Text;
    }

    private void BuildAdvicePage(StackPanel page)
    {
        var header = Card(B("#F2EDFB"), B("#E5D9FA"));
        var top = new Grid();
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(105) });
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        top.Children.Add(CharacterPortrait(98, 132));
        var intro = new StackPanel { Margin = new Thickness(17, 3, 0, 0) };
        intro.Children.Add(T("Centro de recomendaciones", 23, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 9)));
        intro.Children.Add(T("Consejos prácticos y estado local de PalClam. " +
            "El personaje no toma decisiones, no analiza por su cuenta ni envía datos.", 12, Slate));
        intro.Children.Add(T("F1: abrir consejos   ·   Alt+Inicio: volver al panel principal",
            11, Purple, FontWeights.SemiBold, new Thickness(0, 14, 0, 0)));
        Grid.SetColumn(intro, 1);
        top.Children.Add(intro);
        header.Child = top;
        page.Children.Add(header);

        page.Children.Add(T("Estado comprobable del programa", 18, Ink, FontWeights.Bold,
            new Thickness(0, 20, 0, 10)));
        page.Children.Add(T("Son comprobaciones locales, no una puntuación de protección del equipo.",
            12, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 10)));
        var statusGrid = new Grid();
        statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        statusGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        statusGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _adviceEngine = AddAdviceStatusCard(statusGrid, 0, 0, "MOTOR CLAMAV");
        _adviceSignatures = AddAdviceStatusCard(statusGrid, 0, 1, "FIRMAS LOCALES");
        _adviceGuard = AddAdviceStatusCard(statusGrid, 1, 0, "PALCLAM GUARD");
        _adviceLastScan = AddAdviceStatusCard(statusGrid, 1, 1, "ÚLTIMO ESTADO DE ANÁLISIS");
        page.Children.Add(statusGrid);
        var actions = new WrapPanel { Margin = new Thickness(0, 12, 0, 7) };
        actions.Children.Add(ActionButton("↻  Actualizar estado", true, (_, _) => RefreshAdviceStatus()));
        actions.Children.Add(ActionButton("Motor y firmas", false, (_, _) => Navigate("signatures")));
        actions.Children.Add(ActionButton("Abrir análisis", false, (_, _) => Navigate("scan")));
        actions.Children.Add(ActionButton("Diagnóstico", false, (_, _) => Navigate("diagnostics")));
        page.Children.Add(actions);

        page.Children.Add(T("Consejos y acciones recomendadas", 18, Ink, FontWeights.Bold,
            new Thickness(0, 14, 0, 10)));
        foreach (var advice in SecurityAdviceCatalog.Items)
        {
            var card = Card(Surface, Outline);
            card.Margin = new Thickness(0, 0, 0, 10);
            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var block = new StackPanel { Margin = new Thickness(0, 0, 14, 0) };
            block.Children.Add(T(advice.Title, 13, Ink, FontWeights.Bold,
                new Thickness(0, 0, 0, 5)));
            block.Children.Add(T(advice.Text, 12, Muted));
            body.Children.Add(block);
            var go = ActionButton("Abrir sección", false, (_, _) => Navigate(advice.Page));
            go.ToolTip = "Ir a la sección relacionada con este consejo";
            go.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(go, 1);
            body.Children.Add(go);
            card.Child = body;
            page.Children.Add(card);
        }
        page.Children.Add(T("PalClam no sustituye Microsoft Defender. " +
            "La autoprotección mediante servicio Windows sigue pendiente de desarrollo y validación.",
            11, Amber, FontWeights.Normal, new Thickness(3, 4, 0, 12)));
    }

    private static TextBlock AddAdviceStatusCard(Grid grid, int row, int column, string label)
    {
        var border = Card(Surface, Outline);
        border.Margin = new Thickness(column == 0 ? 0 : 5, row == 0 ? 0 : 5,
            column == 1 ? 0 : 5, row == 1 ? 0 : 5);
        var stack = new StackPanel();
        stack.Children.Add(T(label, 10, Muted, FontWeights.Bold, new Thickness(0, 0, 0, 7)));
        var status = T("Pendiente de comprobar", 13, Ink, FontWeights.SemiBold);
        stack.Children.Add(status);
        border.Child = stack;
        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        grid.Children.Add(border);
        return status;
    }

    private void RefreshAdviceStatus()
    {
        if (_adviceEngine is null || _engineBox is null) return;
        var ready = ClamAvLocator.IsValidScanner(_engineBox.Text);
        _adviceEngine.Text = ready ? "Motor localizado" : "Motor no disponible";
        _adviceEngine.Foreground = ready ? Green : Red;

        var available = SignatureStore.HasSignatures();
        var date = available ? SignatureStore.OfflineDailyDateUtc() : null;
        _adviceSignatures.Text = available
            ? (date.HasValue ? $"Firmas disponibles · base {date.Value:dd/MM/yyyy}" : "Firmas disponibles · fecha desconocida")
            : "Firmas no disponibles";
        _adviceSignatures.Foreground = available ? Green : Amber;

        var guardActive = _guardService?.IsRunning == true;
        _adviceGuard.Text = guardActive ? "Vigilancia de cambios activa (no preventiva)" :
            _guardPaused ? "Vigilancia pausada" : "Vigilancia opcional inactiva";
        _adviceGuard.Foreground = _guardPaused ? Amber : guardActive ? Green : Muted;

        var last = _status?.Text?.Trim();
        _adviceLastScan.Text = string.IsNullOrEmpty(last) ? "No hay información de análisis" : last;
        _adviceLastScan.Foreground = _scanRunning ? Blue :
            last is not null && (last.Contains("Cancel", StringComparison.OrdinalIgnoreCase) ||
                last.Contains("Incompleto", StringComparison.OrdinalIgnoreCase) ||
                last.Contains("Error", StringComparison.OrdinalIgnoreCase)) ? Amber : Ink;
    }
}
