// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using PalClam.Infrastructure;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private RansomwareMonitor? _ransomwareMonitor;
    private int _ransomwareGeneration;
    private bool _ransomwareAlert;
    private readonly ObservableCollection<RansomwareAlert> _ransomwareAlerts = new();
    private TextBox _ransomwareFolders = null!, _ransomwareExclusions = null!;
    private TextBlock _ransomwareStatus = null!, _ransomwareTelemetry = null!;
    private ComboBox _ransomwareWindow = null!, _ransomwareCount = null!, _ransomwareSignals = null!;
    private CheckBox _ransomwareStartup = null!, _ransomwareEntropy = null!, _ransomwareMass = null!;

    private void BuildRansomwarePage(StackPanel page)
    {
        var value = RansomwareOptions.Normalize(_settings.Ransomware);
        page.Children.Add(T("Anti-ransomware · Monitor de cifrado", 24, Ink, FontWeights.Bold, new Thickness(0, 0, 0, 7)));
        page.Children.Add(T("Alerta ante patrones compatibles con cifrado masivo en las carpetas que elijas. Combina archivos distintos, renombrados y muestras locales de contenido. Se inicia manualmente o al abrir PalClam si activas esa opción.", 12, Muted));
        var note = Card(AmberPale, AmberPale); note.Margin = new Thickness(0, 12, 0, 12);
        note.Child = T("Es una sospecha de comportamiento, no una detección confirmada ni un bloqueo preventivo. Puede avisar después de que se hayan dañado archivos. No detecta el cifrado de volumen de BitLocker, no descifra datos y no sustituye copias de seguridad. Las sincronizaciones, compresiones y cambios legítimos también pueden generar avisos.", 12, Ink);
        page.Children.Add(note);
        var card = Card(Surface, Outline); var stack = new StackPanel(); card.Child = stack; page.Children.Add(card);
        stack.Children.Add(T("Carpetas de datos que quieres vigilar", 16, Ink, FontWeights.Bold));
        stack.Children.Add(T("Una ruta por línea. Elige Documentos, Escritorio u otras carpetas locales concretas; evita la raíz del disco y carpetas de Windows. Funciona mientras PalClam esté abierto, también en la bandeja.", 11, Muted, FontWeights.Normal, new Thickness(0, 6, 0, 8)));
        _ransomwareFolders = new TextBox { Text = string.Join(Environment.NewLine, value.Folders), AcceptsReturn = true,
            MinHeight = 85, MaxHeight = 140, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(7) };
        stack.Children.Add(_ransomwareFolders);
        var folders = new WrapPanel { Margin = new Thickness(0, 9, 0, 8) };
        folders.Children.Add(ActionButton("Añadir carpeta", false, (_, _) =>
        {
            var picker = new Microsoft.Win32.OpenFolderDialog { Title = "Elegir carpeta para el monitor anti-ransomware" };
            if (PalClam.UI.LocalizedDialogs.Show(picker, this) == true)
                _ransomwareFolders.Text = _ransomwareFolders.Text.TrimEnd() + (_ransomwareFolders.Text.Length == 0 ? "" : Environment.NewLine) + picker.FolderName;
        }));
        folders.Children.Add(ActionButton("Añadir Documentos y Escritorio", false, (_, _) =>
        {
            var paths = Lines(_ransomwareFolders.Text).Concat(new[] { Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) }).Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase);
            _ransomwareFolders.Text = string.Join(Environment.NewLine, paths);
        }));
        stack.Children.Add(folders);
        stack.Children.Add(T("Carpetas excluidas (una por línea)", 12, Ink));
        _ransomwareExclusions = new TextBox { Text = string.Join(Environment.NewLine, value.ExcludedFolders), AcceptsReturn = true,
            Height = 55, Padding = new Thickness(7), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 6, 0, 8) };
        stack.Children.Add(_ransomwareExclusions);
        CheckBox Check(string text, bool selected)
        { var box = new CheckBox { Content = T(text, 12, Ink), HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch, IsChecked = selected, Margin = new Thickness(0, 7, 0, 0) }; stack.Children.Add(box); return box; }
        _ransomwareStartup = Check("Iniciar este monitor al abrir PalClam", value.StartWithApp);
        _ransomwareEntropy = Check("Muestrear contenido local para buscar señales de cifrado", value.SampleEntropy);
        _ransomwareMass = Check("Avisar también de cambios o eliminaciones masivos", value.WarnMassActivity);
        var limits = new WrapPanel { Margin = new Thickness(0, 14, 0, 9) }; stack.Children.Add(limits);
        ComboBox Limit(string label, int[] choices, int selected)
        {
            var group = new StackPanel { Margin = new Thickness(0, 0, 18, 0) }; group.Children.Add(T(label, 12, Ink));
            var box = new ComboBox { ItemsSource = choices.Append(selected).Distinct().Order().ToArray(), SelectedItem = selected,
                Width = 135, Margin = new Thickness(0, 5, 0, 0) };
            group.Children.Add(box); limits.Children.Add(group); return box;
        }
        _ransomwareWindow = Limit("Ventana de observación (s)", new[] { 5, 10, 15, 30, 60 }, value.WindowSeconds);
        _ransomwareCount = Limit("Archivos distintos mínimos", new[] { 10, 20, 50, 100, 200 }, value.MinFiles);
        _ransomwareSignals = Limit("Señales mínimas de cifrado", new[] { 2, 5, 10, 20 }, value.MinSignals);
        stack.Children.Add(T("El muestreo lee hasta 64 KB por archivo y 20 archivos por segundo. Se buscan datos muy aleatorios en formatos de texto y nombres compatibles con cifrado. Los documentos ya comprimidos y el cifrado lento o parcial pueden pasar inadvertidos. No se envía contenido a Internet.", 11, Muted));
        var actions = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        actions.Children.Add(ActionButton("Guardar e iniciar monitor", true, (_, _) => SaveAndStartRansomware()));
        actions.Children.Add(ActionButton("Detener monitor", false, (_, _) =>
        {
            StopRansomware(); _ransomwareStatus.Text = "Monitor detenido; no se observan cambios. El inicio automático conserva su preferencia.";
        }));
        actions.Children.Add(ActionButton("Guardar opciones", false, (_, _) => SaveRansomwareOptions()));
        stack.Children.Add(actions);
        _ransomwareStatus = T("Monitor desactivado.", 12, Muted, FontWeights.SemiBold, new Thickness(0, 9, 0, 6));
        stack.Children.Add(_ransomwareStatus); _ransomwareTelemetry = T("", 11, Muted); stack.Children.Add(_ransomwareTelemetry);
        var results = Card(Surface, Outline); results.Margin = new Thickness(0, 13, 0, 0); var resultStack = new StackPanel(); results.Child = resultStack; page.Children.Add(results);
        resultStack.Children.Add(T("Alertas de comportamiento", 16, Ink, FontWeights.Bold, new Thickness(0, 0, 0, 8)));
        var list = new ListView { ItemsSource = _ransomwareAlerts, MinHeight = 140, MaxHeight = 300 };
        var columns = new GridView();
        columns.Columns.Add(new GridViewColumn { Header = "Fecha", Width = 155, DisplayMemberBinding = new Binding(nameof(RansomwareAlert.DisplayTime)) });
        columns.Columns.Add(new GridViewColumn { Header = "Estado", Width = 160, DisplayMemberBinding = new Binding(nameof(RansomwareAlert.Status)) });
        columns.Columns.Add(new GridViewColumn { Header = "Detalle", Width = 450, DisplayMemberBinding = new Binding(nameof(RansomwareAlert.Detail)) });
        list.View = columns; resultStack.Children.Add(list);
        var selected = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, MinHeight = 55, Margin = new Thickness(0, 8, 0, 8) };
        list.SelectionChanged += (_, _) => selected.Text = list.SelectedItem is RansomwareAlert alert ? alert.ExamplePath : "";
        resultStack.Children.Add(selected);
        resultStack.Children.Add(ActionButton("Ver historial de notificaciones", false, (_, _) => Navigate("alerts")));
        resultStack.Children.Add(T("Ante una sospecha, comprueba la actividad y aísla el equipo de la red si confirmas que los archivos se están cifrando. Los avisos se guardan localmente con rutas y contadores; no incluyen contenido de los archivos. Reconocer el LED no borra este historial.", 11, Muted, FontWeights.Normal, new Thickness(0, 8, 0, 0)));
    }
    private static string[] Lines(string text) => text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private bool SaveRansomwareOptions()
    {
        var folders = Lines(_ransomwareFolders.Text); var excluded = Lines(_ransomwareExclusions.Text);
        if (folders.Length > 32 || excluded.Length > 32 || folders.Concat(excluded).Any(p => !Path.IsPathFullyQualified(p)))
        { _ransomwareStatus.Text = "Usa rutas absolutas y un máximo de 32 carpetas y 32 exclusiones."; _ransomwareStatus.Foreground = Red; return false; }
        var count = (int)(_ransomwareCount.SelectedItem ?? 20); var signals = (int)(_ransomwareSignals.SelectedItem ?? 5);
        if (signals > count) { _ransomwareStatus.Text = "Las señales mínimas no pueden superar el número de archivos."; return false; }
        var old = _settings.Ransomware;
        _settings.Ransomware = RansomwareOptions.Normalize(new()
        {
            Folders = folders.ToList(), ExcludedFolders = excluded.ToList(), StartWithApp = _ransomwareStartup.IsChecked == true,
            SampleEntropy = _ransomwareEntropy.IsChecked == true, WarnMassActivity = _ransomwareMass.IsChecked == true,
            WindowSeconds = (int)(_ransomwareWindow.SelectedItem ?? 15), MinFiles = count, MinSignals = signals
        });
        try { SettingsStore.Save(_settings); _ransomwareStatus.Text = "Opciones guardadas. Inicia o reinicia el monitor para aplicarlas."; _ransomwareStatus.Foreground = Muted; return true; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { _settings.Ransomware = old; _ransomwareStatus.Text = "No se pudo guardar la configuración: " + ex.Message; return false; }
    }
    private void SaveAndStartRansomware() { if (SaveRansomwareOptions()) StartRansomware(); }
    private void StartRansomware()
    {
        StopRansomware(); var generation = _ransomwareGeneration;
        _ransomwareMonitor = new RansomwareMonitor(_settings.Ransomware, alert =>
        {
            if (Dispatcher.HasShutdownStarted) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (generation != _ransomwareGeneration) return;
                if (_ransomwareAlerts.Count >= 200) _ransomwareAlerts.RemoveAt(0);
                _ransomwareAlerts.Add(alert);
                if (alert.Kind == "suspected") { _ransomwareAlert = true; MarkThreatForTray(); RenderDiskLed(); }
                _ransomwareStatus.Text = alert.Kind == "suspected" ? "Posible ransomware: revisa las alertas de comportamiento." : "Actividad masiva: revisa las alertas de comportamiento.";
                _ransomwareStatus.Foreground = alert.Kind == "suspected" ? Red : Amber;
                _desktopNotifications?.Show("PalClam · Anti-ransomware", PalClam.Localization.DisplayText.Translate(alert.Status) + ": " + PalClam.Localization.DisplayText.Translate(alert.Detail), true);
                try { RansomwareAudit.Alert(alert); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { AppendLog("No se pudo guardar la alerta anti-ransomware: " + ex.Message); }
            }));
        }, detail =>
        {
            if (Dispatcher.HasShutdownStarted) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (generation != _ransomwareGeneration) return;
                _ransomwareStatus.Text = detail; _ransomwareStatus.Foreground = Amber;
                _desktopNotifications?.Show("PalClam · Anti-ransomware", _ransomwareStatus.Text, true);
                try { RansomwareAudit.Health(detail); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { AppendLog(ex.Message); }
            }));
        });
        _ransomwareMonitor.Start();
        _ransomwareStatus.Text = _ransomwareMonitor.IsRunning ? "Monitor activo en las carpetas disponibles. No bloquea escrituras." : "No hay carpetas operativas; el monitor no está vigilando.";
        _ransomwareStatus.Foreground = _ransomwareMonitor.IsDegraded || !_ransomwareMonitor.IsRunning ? Amber : Green;
        RefreshRansomwareTelemetry();
    }
    private void RefreshRansomwareTelemetry()
    {
        if (_ransomwareTelemetry is null) return;
        var monitor = _ransomwareMonitor;
        _ransomwareTelemetry.Text = monitor is null ? "Monitor detenido." : monitor.IsDegraded
            ? $"Carpetas: {monitor.WatchedFolders} · Eventos procesados: {monitor.Processed} · Perdidos en cola: {monitor.Lost} · Muestras no disponibles: {monitor.SampleFailures} · VIGILANCIA INCOMPLETA"
            : $"Carpetas: {monitor.WatchedFolders} · Eventos procesados: {monitor.Processed} · Perdidos en cola: {monitor.Lost} · Muestras no disponibles: {monitor.SampleFailures}";

    }
    private void StopRansomware() { _ransomwareGeneration++; _ransomwareMonitor?.Dispose(); _ransomwareMonitor = null; RefreshRansomwareTelemetry(); }
}
