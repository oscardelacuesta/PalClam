// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using System.Windows.Controls;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private SmartScanPlan? _lastScanPlan;
    private ComboBox _smartScanPriority = null!;

    private Border BuildSmartScanPanel()
    {
        var card = Card(Surface, Outline);
        var stack = new StackPanel();
        stack.Children.Add(T("Smart Scan PRO · perfiles de análisis", 17, Ink, FontWeights.Bold,
            new Thickness(0,0,0,6)));
        stack.Children.Add(T("Cada perfil define dónde analizar y con qué prioridad. ClamAV utiliza las mismas firmas, límites y exclusiones configuradas en PalClam.",
            12, Muted, FontWeights.Normal, new Thickness(0,0,0,12)));
        var actions = new WrapPanel { Margin = new Thickness(0,0,0,12) };
        _quickButton = ActionButton("⚡  Rápido", true, (_, _) => _ = StartPresetScanAsync(SmartScanPreset.Quick));
        _profileButton = ActionButton("◉  Completo", false, (_, _) => _ = StartPresetScanAsync(SmartScanPreset.Full));
        _selectedButton = ActionButton("⌕  Personalizado", false, (_, _) => _ = StartPresetScanAsync(SmartScanPreset.Custom));
        SetSemanticButton(_quickButton,"scan");
        _quickButton.MinWidth = 145;
        _profileButton.MinWidth = 145;
        _selectedButton.MinWidth = 145;
        actions.Children.Add(_quickButton);
        actions.Children.Add(_profileButton);
        actions.Children.Add(_selectedButton);
        stack.Children.Add(actions);
        stack.Children.Add(T("Rápido: Descargas, Escritorio, Documentos e Inicio del usuario (si existen). " +
            "Completo: unidades locales fijas disponibles; puede tardar horas y no incluye unidades de red. " +
            "Personalizado: ruta del cuadro inferior o Explorador. El modo completo requiere confirmación.",
            11, Muted, FontWeights.Normal, new Thickness(0,0,0,12)));
        var priority = new WrapPanel { Margin = new Thickness(0,0,0,2) };
        priority.Children.Add(T("Rendimiento del personalizado:", 12, Ink,
            FontWeights.SemiBold, new Thickness(0,7,10,0)));
        _smartScanPriority = new ComboBox { MinWidth=150, Height=33,
            ItemsSource = new[] { "Económico", "Equilibrado", "Prioritario" },
            SelectedItem = _settings.ScanPriority };
        _smartScanPriority.SelectionChanged += (_, _) =>
        {
            if (_smartScanPriority.SelectedItem is not string selected) return;
            _settings.ScanPriority = selected;
            if (_scanPriorityOption is not null && !Equals(_scanPriorityOption.SelectedItem, selected))
                _scanPriorityOption.SelectedItem = selected;
            SaveSettings();
        };
        priority.Children.Add(_smartScanPriority);
        stack.Children.Add(priority);
        stack.Children.Add(T("Económico reduce la prioridad del proceso; Prioritario puede afectar a otras aplicaciones. " +
            "No altera las firmas, no asegura mayor velocidad y no añade bloqueo preventivo.",
            11, Muted, FontWeights.Normal, new Thickness(6,5,0,0)));
        card.Child = stack;
        return card;
    }

    private async Task StartPresetScanAsync(SmartScanPreset preset)
    {
        if (_busy || _installingAppUpdate) return;
        try
        {
            var paths = preset switch
            {
                SmartScanPreset.Quick => SmartScanPlanPolicy.QuickCandidates(),
                SmartScanPreset.Full => SmartScanPlanPolicy.LocalFixedDrives(),
                _ => new[] { _targetBox.Text }
            };
            var plan = SmartScanPlanPolicy.Create(preset, paths,
                _settings.ScanPriority, _settings.ScanControlMode);
            if (preset == SmartScanPreset.Full)
            {
                var confirm = MessageBox.Show(this,
                    "Se recorrerán todas las unidades locales fijas detectadas. El análisis puede durar horas " +
                    "y generar errores de acceso. No cubre unidades de red ni garantiza protección preventiva.\n\n" +
                    "¿Iniciar análisis completo?", "PalClam · Smart Scan PRO",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;
            }
            await StartScanPlanAsync(plan);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, "Smart Scan · Objetivos", MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
