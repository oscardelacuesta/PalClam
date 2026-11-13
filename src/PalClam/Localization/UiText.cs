// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.ComponentModel;

namespace PalClam.Localization;

// Las vistas se suscriben mediante Binding (eventos débiles de WPF).
// Toda mutación se realiza desde el Dispatcher, nunca desde el motor/servicio.
internal sealed class UiText : INotifyPropertyChanged
{
    internal static UiText Current { get; } = new();
    private readonly TextCatalog _catalog = new();
    public string LanguageId { get; private set; } = TextCatalog.DefaultLanguage;
    public string this[string key] => _catalog.Get(LanguageId, key);
    public event PropertyChangedEventHandler? PropertyChanged;

    internal void SetLanguage(string? languageId)
    {
        var normalized = TextCatalog.NormalizeLanguage(languageId);
        if (LanguageId == normalized) return;
        LanguageId = normalized;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LanguageId)));
    }

    internal void SaveAndSetLanguage(string? languageId, Action<string> persist)
    {
        var normalized = TextCatalog.NormalizeLanguage(languageId);
        // No anunciar éxito ni cambiar la UI si no se pudo guardar la preferencia.
        persist(normalized);
        SetLanguage(normalized);
    }

    internal void SaveAndSetPreference(string? preference, Action<string> persist, string? windowsUiCulture = null)
    {
        var normalized = TextCatalog.NormalizePreference(preference);
        persist(normalized);
        SetLanguage(TextCatalog.ResolvePreference(normalized, windowsUiCulture));
    }
}
