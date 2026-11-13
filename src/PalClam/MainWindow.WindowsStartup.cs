// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using System.Windows.Controls;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private ComboBox? _startWithWindowsOption;
    private CheckBox? _auroraOption;
    private ComboBox? _auroraSpeedOption;
    private TextBlock? _windowsStartupStatus;
    private bool _updatingWindowsStartupOption;

    private void RefreshWindowsStartupPreference()
    {
        _updatingWindowsStartupOption = true;
        try
        {
            if (_startWithWindowsOption is not null)
                _startWithWindowsOption.SelectedIndex = (int)WindowsStartupRegistration.GetScope();
        }
        finally { _updatingWindowsStartupOption = false; }
        if (_windowsStartupStatus is not null)
            _windowsStartupStatus.Text = WindowsStartupRegistration.Status();
    }

    private async void SaveWindowsStartupPreference()
    {
        if (_updatingWindowsStartupOption || _startWithWindowsOption is null ||
            _startWithWindowsOption.SelectedIndex < 0) return;

        var desired = (StartupScope)_startWithWindowsOption.SelectedIndex;
        var previous = WindowsStartupRegistration.GetScope();
        if (desired == previous) return;

        _startWithWindowsOption.IsEnabled = false;
        try
        {
            // El diálogo UAC y la escritura al Registro no bloquean el Dispatcher WPF.
            var result = await Task.Run(() =>
            {
                var ok = WindowsStartupRegistration.TrySet(desired, out var error);
                return (ok, error);
            });
            if (result.ok)
            {
                _settings.LaunchAtWindowsStartup = desired != StartupScope.Disabled;
                SaveSettings();
                if (_optionsStatus is not null)
                    _optionsStatus.Text = desired switch
                    {
                        StartupScope.AllUsers => "Inicio en cada sesión de Windows habilitado. Guard se configura por usuario.",
                        StartupScope.CurrentUser => "PalClam se iniciará en la bandeja de tu cuenta.",
                        _ => "Inicio con Windows desactivado."
                    };
            }
            else
            {
                if (_optionsStatus is not null)
                    _optionsStatus.Text = "No se pudo cambiar el inicio automático: " + result.error;
                MessageBox.Show(this, result.error, "PalClam · Inicio automático",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            PalClam.Services.CrashDiagnostics.Record("opciones-inicio-windows", ex);
            if (_optionsStatus is not null)
                _optionsStatus.Text = "Error al cambiar el inicio: " + ex.Message;
        }
        finally
        {
            _startWithWindowsOption.IsEnabled = true;
            RefreshWindowsStartupPreference();
        }
    }
}
