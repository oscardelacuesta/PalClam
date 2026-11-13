// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using System.Windows.Controls;
using PalClam.Infrastructure;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private bool _aiAnalystLoading;

    private void BuildAiAnalystShortcuts(StackPanel page)
    {
        var card = Card(Surface, Outline);
        card.Margin = new Thickness(0, 0, 0, 14);
        var stack = new StackPanel();
        stack.Children.Add(T("AI Security Analyst · Consultas guiadas", 17, Ink,
            FontWeights.Bold, new Thickness(0, 0, 0, 8)));
        var introduction = T(
            "Prepara resúmenes estadísticos de PalClam sin incluir rutas, nombres, " +
            "detalles de incidentes, certificados ni datos de archivos. " +
            "Puedes revisar y editar el texto antes de decidir si lo envías.",
            11.5, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 9));
        introduction.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(introduction);
        var row = new WrapPanel();
        row.Children.Add(ActionButton("Último análisis", false,
            (_, _) => OpenAiAnalystWithPrompt(
                AiSecurityAnalyst.ForScan(ScanHistory.ReadRecent(1).FirstOrDefault()))));
        row.Children.Add(ActionButton("Guard y avisos", false,
            async (_, _) => await LoadAiAnalystNoticesAsync()));
        row.Children.Add(ActionButton("Security Health 360", false,
            (_, _) => OpenAiAnalystWithPrompt(AiSecurityAnalyst.ForHealth(_healthFindings.ToArray()))));
        row.Children.Add(ActionButton("Cuarentena", false,
            (_, _) => OpenAiAnalystWithPrompt(AiSecurityAnalyst.ForQuarantine(QuarantineStore.List()))));
        stack.Children.Add(row);
        var info = T("Las plantillas funcionan localmente y no consultan ninguna API. " +
            "Nunca se envía información al seleccionarlas. Los proveedores remotos requieren consentimiento por consulta.",
            11, Muted, FontWeights.Normal, new Thickness(0, 8, 0, 0));
        info.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(info);
        card.Child = stack;
        page.Children.Add(card);
    }

    private void OpenAiAnalystWithPrompt(string prompt)
    {
        // El usuario controla el envío. Jamás invocar AiGateway aquí.
        if (_aiPrompt is null) return;
        Navigate("ai");
        _aiPrompt.Text = PalClam.Localization.DisplayText.Translate(prompt);
        _aiAnswer?.Clear();
        ResetAiConsent();
        if (_aiStatus is not null)
            _aiStatus.Text = "Resumen de lista blanca preparado localmente. " +
                "Revísalo; no se ha enviado ningún dato.";
    }

    private async Task LoadAiAnalystNoticesAsync()
    {
        if (_aiAnalystLoading) return;
        _aiAnalystLoading = true;
        if (_aiStatus is not null) _aiStatus.Text = "Consultando registros locales limitados…";
        try
        {
            var result = await Task.Run(() => SecurityNoticeFeed.ReadRecent(
                PersonalGuardFolder, SharedGuardFolder, DateTimeOffset.UtcNow, 200));
            if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
                OpenAiAnalystWithPrompt(AiSecurityAnalyst.ForNotices(result));
        }
        catch (Exception)
        {
            if (_aiStatus is not null)
                _aiStatus.Text = "No se pudo preparar el resumen. No se ha enviado ningún dato.";
        }
        finally { _aiAnalystLoading = false; }
    }

    private void ExplainSelectedSecurityNoticeWithAi()
    {
        var selected = _securityEventsView?.SelectedItem as SecurityNotice;
        if (selected is null)
        {
            MessageBox.Show(this, "Selecciona una alerta para preparar su contexto anónimo.",
                "PalClam AI", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        OpenAiAnalystWithPrompt(AiSecurityAnalyst.ForNotice(selected));
    }
}
