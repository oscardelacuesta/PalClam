// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private TextBlock? _machineServiceStatus;
    private DispatcherTimer? _machineStatusTimer;
    private MachineServiceSnapshot? _machinePreviousSnapshot;
    private bool _machineActionPending;
    private const string MachineServiceName = "PalClamGuardSvc";

    private void BuildMachineServiceSection(StackPanel page)
    {
        var card = Card(Surface, Outline);
        card.Margin = new Thickness(0, 14, 0, 0);
        var content = new StackPanel();
        content.Children.Add(T("Servicio de Windows · Experimental", 16, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 6)));
        var description = T("Opcional, independiente de la interfaz y de la sesión. Vigila exclusivamente la carpeta " +
            "%ProgramData%\\PalClam\\GuardWatch en una instalación compartida. " +
            "No controla Descargas de otros usuarios, no bloquea ejecuciones y no sustituye Defender. " +
            "Utiliza firmas compartidas en ProgramData, renovables con autorización administrativa. " +
            "Permite sincronización offline y actualización online administrativa explícita. " +
            "No ofrece bloqueo preventivo ni promete cobertura completa.",
            11, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 10));
        description.TextWrapping = TextWrapping.Wrap;
        content.Children.Add(description);
        _machineServiceStatus = T("Consultando estado del servicio…", 12, Blue, FontWeights.SemiBold,
            new Thickness(0, 0, 0, 9));
        _machineServiceStatus.TextWrapping = TextWrapping.Wrap;
        content.Children.Add(_machineServiceStatus);
        var actions = new WrapPanel();
        actions.Children.Add(ActionButton("Consultar estado", false, (_, _) => RefreshMachineServiceStatus()));
        actions.Children.Add(ActionButton("Instalar y activar (admin)", false,
            async (_, _) => await ConfigureMachineServiceAsync("Install")));
        actions.Children.Add(ActionButton("Iniciar servicio (admin)", false,
            async (_, _) => await ConfigureMachineServiceAsync("Start")));
        actions.Children.Add(ActionButton("Detener servicio (admin)", false,
            async (_, _) => await ConfigureMachineServiceAsync("Stop")));
        actions.Children.Add(ActionButton("Reiniciar servicio (admin)", false,
            async (_, _) => await ConfigureMachineServiceAsync("Restart")));
        actions.Children.Add(ActionButton("Sincronizar firmas instaladas (admin)", false,
            async (_, _) => await ConfigureMachineServiceAsync("RefreshSignatures")));
        actions.Children.Add(ActionButton("Actualizar firmas online (admin)", false,
            async (_, _) => await ConfigureMachineServiceAsync("UpdateOnlineSignatures")));
        actions.Children.Add(ActionButton("Retirar servicio (admin)", false,
            async (_, _) => await ConfigureMachineServiceAsync("Remove")));
        content.Children.Add(actions);
        var noticeExplanation = T("Los controles de servicio siempre solicitan UAC; detenerlo no afecta a Guard personal. " +
            "Los avisos del servicio solo aparecen mientras PalClam está ejecutándose, incluso en bandeja. " +
            "Sin sesión gráfica, las incidencias permanecen en el registro local.",
            11, Muted, FontWeights.Normal, new Thickness(0, 8, 0, 0));
        noticeExplanation.TextWrapping = TextWrapping.Wrap;
        content.Children.Add(noticeExplanation);
        content.Children.Add(T("La instalación solicita UAC y solo está disponible si PalClam se instaló " +
            "para todos los usuarios en Program Files. Las alertas aparecen en " +
            "%ProgramData%\\PalClam\\MachineGuard\\eventos.log. La desinstalación conserva esos registros.",
            11, Muted, FontWeights.Normal, new Thickness(0, 9, 0, 0)));
        card.Child = content;
        page.Children.Add(card);
    }

    private void StartMachineStatusMonitoring()
    {
        // Sigue consultando mientras la ventana está oculta en la bandeja.
        // Solo consulta diagnóstico de lectura, nunca envía comandos al servicio.
        RefreshMachineServiceStatus();
        _machineStatusTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        _machineStatusTimer.Tick += OnMachineStatusTick;
        _machineStatusTimer.Start();
    }

    private void OnMachineStatusTick(object? sender, EventArgs e)
    {
        RefreshMachineServiceStatus();
        if (_pages.TryGetValue("alerts", out var page) && ReferenceEquals(_pageHost?.Content, page))
            _ = RefreshSecurityCenterAsync();
    }

    private void RefreshMachineServiceStatus()
    {
        if (_machineServiceStatus is null) return;
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Services\" + MachineServiceName);
            if (key is null)
            {
                _machineServiceStatus.Text = "Servicio no instalado · Guard personal sigue disponible.";
                _machinePreviousSnapshot = null;
                return;
            }
            var statusPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "PalClam", "MachineGuard", "estado.json");
            var snap = MachineServiceSnapshot.TryRead(statusPath);
            if (snap is null)
            {
                _machineServiceStatus.Text = "Servicio registrado, pero sin diagnóstico legible. " +
                    "Comprueba services.msc y los permisos de ProgramData.";
                _machinePreviousSnapshot = null;
                return;
            }
            if (!snap.IsFresh(DateTimeOffset.UtcNow))
            {
                _machineServiceStatus.Text = "Servicio sin latido reciente. No se declara vigilancia activa. " +
                    "Comprueba services.msc; última información: " + snap.Utc.ToLocalTime().ToString("g");
                _machinePreviousSnapshot = null;
                return;
            }
            var health = snap.IsHealthy(DateTimeOffset.UtcNow) ? "Vigilancia reactiva activa" :
                "Estado pendiente o degradado: no se garantiza cobertura";
            _machineServiceStatus.Text = $"{health} · {snap.Mode} · " +
                $"Analizados: {snap.Scanned} · Detecciones: {snap.Detections} · " +
                $"Omitidos: {snap.Skipped} · Errores: {snap.Errors} · " +
                $"Eventos perdidos: {snap.LostEvents} · Pendientes: {snap.Pending} · " +
                $"Revisiones: {snap.Reconciliations} · {snap.Signatures}. " +
                "Sin bloqueo preventivo. " + snap.Detail;
            _machineServiceStatus.Foreground = snap.IsHealthy(DateTimeOffset.UtcNow)
                ? Green : B("#A23E3E");
            NotifyMachineCounterChanges(snap);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            ArgumentException or System.Security.SecurityException)
        {
            _machineServiceStatus.Text = "No se pudo consultar el servicio: " + ex.Message;
        }
    }

    private void NotifyMachineCounterChanges(MachineServiceSnapshot snapshot)
    {
        // Primera lectura = referencia, nunca un aviso de detecciones antiguas.
        var prior = _machinePreviousSnapshot;
        _machinePreviousSnapshot = snapshot;
        if (!_settings.GuardNotifyThreats) return;
        var now = DateTimeOffset.UtcNow;
        var changes = MachineGuardNoticePolicy.Compare(prior, snapshot, now);
        if (changes.NewDetections &&
            _guardNoticeGate.ShouldShow(GuardNoticeKind.Threat, "machine-service", now))
            _desktopNotifications?.Show("PalClam Machine Guard: detecciones nuevas",
                "Consulta el registro de Machine Guard; no se ha bloqueado ningún archivo.", true);
        if (changes.NewCoverageGaps &&
            _guardNoticeGate.ShouldShow(GuardNoticeKind.Warning, "machine-service", now))
            _desktopNotifications?.Show("PalClam Machine Guard: vigilancia incompleta",
                "Hay eventos perdidos o archivos omitidos. Revisa el diagnóstico y analiza manualmente.", true);
        if (changes.NewErrors &&
            _guardNoticeGate.ShouldShow(GuardNoticeKind.Error, "machine-service", now))
            _desktopNotifications?.Show("PalClam Machine Guard: error de análisis",
                "Consulta los registros del servicio. La vigilancia puede estar incompleta.", true);
    }

    private async Task ConfigureMachineServiceAsync(string action)
    {
        if (AppPaths.IsPortable) { MessageBox.Show(this, "El servicio de Windows no está disponible en modo portable.", "PalClam", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        if (_machineActionPending) return;
        if (!StartupLaunchPolicy.IsMachineInstallPath(Environment.ProcessPath ?? ""))
        {
            MessageBox.Show(this, "El servicio necesita instalar PalClam para todos los usuarios " +
                "en Program Files. Esta copia es personal o portátil.", "PalClam Machine Guard",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var helper = Path.Combine(AppContext.BaseDirectory, "Tools", "MachineService.ps1");
        if (!File.Exists(helper) || !File.Exists(Path.Combine(AppContext.BaseDirectory, "PalClam.Service.exe")))
        {
            MessageBox.Show(this, "Falta el servicio o su instalador. Genera el instalador 1.1.0 completo.",
                "PalClam Machine Guard", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var verb = action switch
        {
            "Install" => "instalar y activar",
            "Start" => "iniciar",
            "Stop" => "detener temporalmente",
            "Restart" => "reiniciar",
            "RefreshSignatures" => "restaurar firmas compartidas desde el instalador (reinicia el servicio)",
            "UpdateOnlineSignatures" => "actualizar firmas compartidas con FreshClam oficial (puede reiniciar el servicio)",
            "Remove" => "detener y retirar",
            _ => "modificar"
        };
        if (MessageBox.Show(this, $"¿Quieres {verb} PalClam Machine Guard para el equipo? " +
            "Se solicitará autorización de administrador. No sustituye Microsoft Defender.",
            "Servicio experimental", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        _machineActionPending = true;
        try
        {
            var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                "WindowsPowerShell", "v1.0", "powershell.exe");
            var start = new ProcessStartInfo(powershell)
            {
                UseShellExecute = true,
                Verb = "runas"
            };
            start.ArgumentList.Add("-NoLogo");
            start.ArgumentList.Add("-NoProfile");
            start.ArgumentList.Add("-ExecutionPolicy");
            start.ArgumentList.Add("Bypass");
            start.ArgumentList.Add("-File");
            start.ArgumentList.Add(helper);
            start.ArgumentList.Add("-Action");
            start.ArgumentList.Add(action);
            using var process = Process.Start(start) ?? throw new IOException("No pudo iniciarse la configuración del servicio.");
            await process.WaitForExitAsync();
            RefreshMachineServiceStatus();
            await RefreshSignatureCenterAsync();
            if (process.ExitCode != 0)
                MessageBox.Show(this, "Windows no completó la configuración. " +
                    "Comprueba permisos de administrador y el registro de PowerShell (código " +
                    process.ExitCode + ").", "PalClam Machine Guard", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            if (_machineServiceStatus is not null)
                _machineServiceStatus.Text = "Autorización UAC cancelada: no se ha cambiado el servicio.";
        }
        catch (Exception ex)
        {
            CrashDiagnostics.Record("machine-guard-config", ex);
            MessageBox.Show(this, ex.Message, "PalClam Machine Guard", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { _machineActionPending = false; }
    }
}
