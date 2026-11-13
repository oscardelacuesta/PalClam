// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using Microsoft.Win32;
using PalClam.Localization;

namespace PalClam.UI;

internal static class LocalizedDialogs
{
    internal static bool? Show(Microsoft.Win32.CommonDialog dialog, Window owner)
    {
        if (dialog is Microsoft.Win32.FileDialog file)
        {
            file.Title = DisplayText.Translate(file.Title);
            var filter = file.Filter.Split('|');
            for (var i = 0; i < filter.Length; i += 2) filter[i] = DisplayText.Translate(filter[i]);
            file.Filter = string.Join("|", filter);
        }
        if (dialog is OpenFolderDialog folder) folder.Title = DisplayText.Translate(folder.Title);
        return dialog.ShowDialog(owner);
    }
}
