// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PalClam.Localization;

namespace PalClam.UI;

internal static class LocalizedUi
{
    internal static void Bind(DependencyObject target, DependencyProperty property, string key,
        params object[] arguments)
    {
        var existing = BindingOperations.GetBinding(target, property);
        // Los indicadores de análisis se refrescan frecuentemente: no reinstalar
        // un Binding cuando su clave no ha cambiado.
        if (arguments.Length == 0 && existing?.Source == UiText.Current && existing.Converter is null &&
            existing.Path?.Path == "[" + key + "]") return;
        BindingOperations.SetBinding(target, property, new System.Windows.Data.Binding("[" + key + "]")
        {
            Source = UiText.Current,
            Mode = BindingMode.OneWay,
            Converter = arguments.Length == 0 ? null : new FormatText(arguments)
        });
    }

    private sealed class FormatText(object[] arguments) : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            string.Format(CultureInfo.GetCultureInfo(UiText.Current.LanguageId), (string)value, arguments);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
