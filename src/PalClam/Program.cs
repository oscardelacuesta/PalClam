// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using PalClam.Services;

namespace PalClam;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Auxiliar privilegiado para el inicio global: no crea la ventana WPF.
        // Solo se utiliza tras la confirmación UAC del usuario.
        if (args.Length == 2 && args[0].Equals("--set-machine-startup", StringComparison.OrdinalIgnoreCase)
            && (args[1].Equals("enable", StringComparison.OrdinalIgnoreCase)
                || args[1].Equals("disable", StringComparison.OrdinalIgnoreCase)))
        {
            Environment.ExitCode = WindowsStartupRegistration.ApplyMachineChange(
                args[1].Equals("enable", StringComparison.OrdinalIgnoreCase));
            return;
        }
        // Instancia auxiliar independiente: sin interfaz, sin análisis, sin privilegios.
        if (args.Length > 0 && args[0] == "--self-defense-watch")
        {
            Environment.ExitCode = PalClam.Services.SelfDefenseMonitor.RunHelper(args);
            return;
        }
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception ex)
            {
                PalClam.Services.SelfDefenseMonitor.RecordUnhandled(ex);
                PalClam.Services.CrashDiagnostics.Record("excepcion-no-controlada", ex);
            }
        };
        // Solo se acepta el parámetro explícito de Explorer. El objetivo
        // se comprueba en StartScanAsync, antes de iniciar ClamAV.
        if (args.Length == 1 && args[0].Equals("--scheduled-scan", StringComparison.OrdinalIgnoreCase))
        {
            // Ejecutado por el Programador de tareas: proceso sin ventana WPF.
            Environment.ExitCode = PalClam.Services.ScheduledScanRunner.RunAsync().GetAwaiter().GetResult();
            return;
        }
        PalClam.Localization.UiText.Current.SetLanguage(PalClam.Localization.TextCatalog.ResolvePreference(PalClam.Infrastructure.SettingsStore.Load().UiLanguage));
        try { AppPaths.Prepare(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show("No se puede escribir en la carpeta Data de la edición portable. Extrae el ZIP en una ubicación con permisos de escritura.\n" + ex.Message, "PalClam", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.ExitCode = 2; return;
        }
        var target = args.Length == 2 &&
            args[0].Equals("--scan", StringComparison.OrdinalIgnoreCase)
            ? args[1] : null;
        // Una carpeta portable solo admite una ventana a la vez. Los auxiliares anteriores quedan fuera del bloqueo.
        using var portableInstance = AppPaths.IsPortable ? new Mutex(false, AppPaths.InstanceName) : null;
        if (portableInstance is not null)
        {
            var acquired = false;
            try { acquired = portableInstance.WaitOne(0); } catch (AbandonedMutexException) { acquired = true; }
            if (!acquired) { MessageBox.Show("Ya hay una instancia usando los datos de esta edición portable.", "PalClam"); return; }
        }
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        PalClam.UI.PresentationLocalization.Install(app);
        // Apariencia global de ScrollViewer, ListView y TextBox. Si el diccionario
        // no carga, se mantienen las barras nativas en lugar de impedir el inicio.
        try { PalClam.UI.ModernScrollBarTheme.Install(app); }
        catch (Exception ex) { CrashDiagnostics.Record("scrollbar-tema", ex); }
        app.DispatcherUnhandledException += (_, e) =>
        {
            // Registrar excepciones del Dispatcher. No marcar Handled: continuar
            // tras un fallo arbitrario de WPF podría comprometer el estado de la UI.
            PalClam.Services.CrashDiagnostics.Record("dispatcher-wpf", e.Exception);
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
            PalClam.Services.CrashDiagnostics.Record("tarea-no-observada", e.Exception);
        app.Run(new MainWindow(target, StartupLaunchPolicy.IsHiddenStartup(args)));
    }
}
