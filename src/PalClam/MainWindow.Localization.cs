// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using System.Windows.Controls;
using PalClam.Infrastructure;
using PalClam.Localization;
using PalClam.Services;
using PalClam.UI;

namespace PalClam;

public sealed partial class MainWindow
{
    private ComboBox? _languageOption;
    private TextBlock? _languageStatus;
    private bool _applyingLanguage;

    private static TextBlock LT(string key, double size, Brush color) =>
        LT(key, size, color, FontWeights.Normal);

    private static TextBlock LT(string key, double size, Brush color, FontWeight weight, Thickness? margin = null)
    {
        var text = T("", size, color, weight, margin);
        LocalizedUi.Bind(text, TextBlock.TextProperty, key);
        return text;
    }

    private static Button LocalizedAction(string key, bool primary, RoutedEventHandler click)
    {
        var button = ActionButton("", primary, click);
        // TextBlock permite envolver textos largos; conserva los estilos existentes.
        var label = new TextBlock { TextWrapping = TextWrapping.Wrap };
        LocalizedUi.Bind(label, TextBlock.TextProperty, key);
        button.Content = label;
        return button;
    }

    private UIElement BuildLanguageSelector()
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        panel.Children.Add(LT("Language.Label", 12, Ink, FontWeights.SemiBold));
        _languageOption = new ComboBox
        {
            MinHeight = 36, MaxWidth = 300,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            ItemsSource = new[] { new UiLanguage("auto", "Automático (idioma de Windows)") }.Concat(TextCatalog.Languages).ToArray(),
            DisplayMemberPath = nameof(UiLanguage.NativeName),
            SelectedValuePath = nameof(UiLanguage.Id),
            SelectedValue = _settings.UiLanguage,
            Margin = new Thickness(0, 7, 0, 7)
        };
        LocalizedUi.Bind(_languageOption, System.Windows.Automation.AutomationProperties.NameProperty, "Language.Label");
        _languageOption.SelectionChanged += (_, _) => OnUiLanguageChanged();
        panel.Children.Add(_languageOption);
        panel.Children.Add(LT("Language.Description", 11, Muted));
        panel.Children.Add(LT("Language.Notice", 11, Muted, FontWeights.Normal, new Thickness(0, 7, 0, 0)));
        _languageStatus = T("", 11, Purple, FontWeights.Normal, new Thickness(0, 7, 0, 0));
        panel.Children.Add(_languageStatus);
        return panel;
    }

    private void OnUiLanguageChanged()
    {
        if (_applyingLanguage || _languageOption?.SelectedItem is not UiLanguage selected ||
            selected.Id == _settings.UiLanguage) return;
        Dispatcher.VerifyAccess();
        var previous = _settings.UiLanguage;
        _applyingLanguage = true;
        try
        {
            UiText.Current.SaveAndSetPreference(selected.Id, language =>
            {
                _settings.UiLanguage = language;
                SettingsStore.Save(_settings);
            });
            if (_languageStatus is not null)
                LocalizedUi.Bind(_languageStatus, TextBlock.TextProperty, "Language.Saved");
        }
        catch (Exception ex)
        {
            _settings.UiLanguage = previous;
            _languageOption.SelectedValue = previous;
            CrashDiagnostics.Record("idioma-guardar", ex);
            if (_languageStatus is not null)
                LocalizedUi.Bind(_languageStatus, TextBlock.TextProperty, "Language.SaveFailed");
        }
        finally { _applyingLanguage = false; }
        // No BuildLayout, Navigate, SaveGeneralOptions, cancelación ni cambios de cultura global.
    }
}
