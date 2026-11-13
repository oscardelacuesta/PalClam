// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using PalClam.Localization;

namespace PalClam.UI;

internal static class LocalizedMessageBox
{
    internal static MessageBoxResult Show(Window owner, string text, string caption,
        MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None,
        MessageBoxResult defaultResult = MessageBoxResult.None) =>
        System.Windows.MessageBox.Show(owner, DisplayText.Translate(text), DisplayText.Translate(caption), buttons, image, defaultResult);

    internal static MessageBoxResult Show(Window owner, string text) =>
        System.Windows.MessageBox.Show(owner, DisplayText.Translate(text));

    internal static MessageBoxResult Show(string text, string caption,
        MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None) =>
        System.Windows.MessageBox.Show(DisplayText.Translate(text), DisplayText.Translate(caption), buttons, image);
}
