// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PalClam.Infrastructure;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private SelfDefenseMonitor.Session? _selfDefenseSession;
    private TextBlock _selfDefenseStatus = null!;
    private TextBlock _selfDefenseHealth = null!;
    private TextBlock _selfDefenseEvents = null!;
    private CheckBox _selfDefenseOptIn = null!;

    private void InitializeSelfDefense()
    {
        if (_settings.SelfDefenseObserveUnexpectedExit)
            StartSelfDefense();
    }

    private void StartSelfDefense()
    {
        if (_selfDefenseSession?.IsAlive == true) return;
        _selfDefenseSession?.Dispose();
        _selfDefenseSession = SelfDefenseMonitor.TryStart(out var reason);
        AppendLog("[SELF-DEFENSE] " + reason);
        RefreshSelfDefenseUi();
    }

    private void StopSelfDefense()
    {
        _selfDefenseSession?.Dispose();
        _selfDefenseSession = null;
        RefreshSelfDefenseUi();
    }

    private void BuildSelfDefensePage(StackPanel page)
    {
        page.Children.Add(T("PalClam Self-Defense", 23, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 7)));
        page.Children.Add(T("Supervisión de interrupciones · Monitor", 12, Purple,
            FontWeights.SemiBold, new Thickness(0, 0, 0, 12)));
        var notice = Card(AmberPale, AmberPale);
        notice.Child = T("Fase 1: monitor auxiliar voluntario. Detecta un cierre no confirmado de PalClam y lo registra " +
            "para el siguiente inicio. No impide que malware con tus permisos cierre ambos procesos, " +
            "no es un servicio protegido ni reemplaza Microsoft Defender. Tampoco reinicia PalClam automáticamente.",
            12, Ink, FontWeights.Normal);
        page.Children.Add(notice);

        var settingsCard = Card(Surface, Outline);
        settingsCard.Margin = new Thickness(0, 14, 0, 0);
        var settingsStack = new StackPanel();
        settingsStack.Children.Add(T("Monitor independiente", 17, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 10)));
        _selfDefenseOptIn = new CheckBox
        {
            Content = "Supervisar cierres inesperados (opcional)",
            IsChecked = _settings.SelfDefenseObserveUnexpectedExit,
            FontSize = 13, Foreground = Ink, Margin = new Thickness(0, 0, 0, 10)
        };
        _selfDefenseOptIn.Checked += (_, _) => ApplySelfDefenseOption(true);
        _selfDefenseOptIn.Unchecked += (_, _) => ApplySelfDefenseOption(false);
        settingsStack.Children.Add(_selfDefenseOptIn);
        _selfDefenseStatus = T("Comprobando monitor…", 12, Muted,
            FontWeights.SemiBold, new Thickness(0, 0, 0, 9));
        settingsStack.Children.Add(_selfDefenseStatus);
        settingsStack.Children.Add(T("El monitor arranca como un segundo proceso de PalClam, sin permisos de administrador. " +
            "Solo observa el proceso principal. Una salida desde el icono de la bandeja se respeta y no genera alarma.",
            11, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 11)));
        var buttons = new WrapPanel();
        buttons.Children.Add(ActionButton("↻  Revisar estado", false, (_, _) => RefreshSelfDefenseUi()));
        buttons.Children.Add(ActionButton("▤  Abrir registro", false, (_, _) => OpenSelfDefenseLog()));
        settingsStack.Children.Add(buttons);
        settingsCard.Child = settingsStack;
        page.Children.Add(settingsCard);

        var diagnosticCard = Card(Surface, Outline);
        diagnosticCard.Margin = new Thickness(0, 13, 0, 0);
        var diagnosticStack = new StackPanel();
        diagnosticStack.Children.Add(T("Salud de los componentes", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 8)));
        _selfDefenseHealth = T("—", 12, Ink, FontWeights.Normal);
        _selfDefenseHealth.TextWrapping = TextWrapping.Wrap;
        diagnosticStack.Children.Add(_selfDefenseHealth);
        diagnosticStack.Children.Add(ActionButton("Abrir diagnóstico técnico", false,
            (_, _) => Navigate("diagnostics")));
        diagnosticCard.Child = diagnosticStack;
        page.Children.Add(diagnosticCard);

        var eventsCard = Card(Surface, Outline);
        eventsCard.Margin = new Thickness(0, 13, 0, 0);
        var eventsStack = new StackPanel();
        eventsStack.Children.Add(T("Últimas incidencias de cierre", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 8)));
        _selfDefenseEvents = T("Sin incidencias registradas.", 12, Muted,
            FontWeights.Normal);
        _selfDefenseEvents.TextWrapping = TextWrapping.Wrap;
        eventsStack.Children.Add(_selfDefenseEvents);
        eventsCard.Child = eventsStack;
        page.Children.Add(eventsCard);
        RefreshSelfDefenseUi();
    }

    private void ApplySelfDefenseOption(bool enabled)
    {
        _settings.SelfDefenseObserveUnexpectedExit = enabled;
        try { SettingsStore.Save(_settings); }
        catch (Exception ex) { AppendLog("[AVISO] No se guardó la preferencia de Self-Defense: " + ex.GetType().Name); }
        if (enabled) StartSelfDefense(); else StopSelfDefense();
    }

    private void RefreshSelfDefenseUi()
    {
        if (_selfDefenseStatus is null) return;
        var active = _selfDefenseSession?.IsAlive == true;
        _selfDefenseStatus.Text = active
            ? "Monitor auxiliar en ejecución (PID " + _selfDefenseSession!.WatchdogPid + ")."
            : _settings.SelfDefenseObserveUnexpectedExit
                ? "Monitor solicitado, pero no disponible. No se garantiza supervisión."
                : "Desactivado por el usuario.";
        _selfDefenseStatus.Foreground = active ? Green : _settings.SelfDefenseObserveUnexpectedExit ? Red : Muted;
        if (_selfDefenseHealth is not null)
        {
            var motor = ClamAvLocator.IsValidScanner(ClamAvLocator.FindScanner())
                ? "Motor ClamAV localizado" : "Motor ClamAV no disponible";
            var firmas = SignatureStore.HasSignatures()
                ? "firmas locales disponibles" : "firmas locales pendientes";
            var guard = _guardService?.IsRunning == true ? "Guard vigilando cambios" : "Guard inactivo";
            _selfDefenseHealth.Text = motor + " · " + firmas + " · " + guard +
                ". Esta comprobación no equivale a protección preventiva ni prueba que el sistema esté libre de malware.";
        }
        if (_selfDefenseEvents is not null)
        {
            var items = SelfDefenseMonitor.ReadRecent();
            _selfDefenseEvents.Text = items.Count == 0
                ? "Sin incidencias registradas."
                : string.Join(Environment.NewLine, items.Select(x =>
                    x.Utc.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") + "  ·  " + x.Message));
        }
    }

    private void OpenSelfDefenseLog()
    {
        var path = SelfDefenseMonitor.AuditPath;
        try
        {
            if (!File.Exists(path))
            {
                MessageBox.Show(this, "Todavía no existen incidencias registradas.",
                    "PalClam Self-Defense", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "notepad.exe", UseShellExecute = false,
                ArgumentList = { path }
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo abrir el registro: " + ex.GetType().Name,
                "PalClam Self-Defense", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
