// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Globalization;
using System.Resources;
using System.Collections;

namespace PalClam.Localization;

internal sealed record UiLanguage(string Id, string NativeName);
internal sealed record TextResolution(string Value, string LanguageId, bool UsedSpanishFallback);

// Solo presentación: no modifica CurrentCulture, estados, rutas ni argumentos del motor.
internal sealed class TextCatalog
{
    internal const string DefaultLanguage = "es";
    internal const string AutomaticLanguage = "auto";
    internal static IReadOnlyList<UiLanguage> Languages { get; } = Array.AsReadOnly(new[]
    {
        new UiLanguage("es", "Español"), new UiLanguage("en", "English"),
        new UiLanguage("fr", "Français"), new UiLanguage("de", "Deutsch"),
        new UiLanguage("pt", "Português"), new UiLanguage("it", "Italiano")
    });

    private readonly ResourceManager _resources;

    internal TextCatalog(ResourceManager? resources = null) =>
        _resources = resources ?? new ResourceManager("PalClam.Localization.Strings", typeof(TextCatalog).Assembly);

    internal static string NormalizeLanguage(string? id)
    {
        // Admitir culturas regionales guardadas, sin depender del idioma de Windows.
        var language = (id ?? "").Trim().Split('-', '_')[0].ToLowerInvariant();
        return Languages.Any(x => x.Id == language) ? language : DefaultLanguage;
    }

    // Se persiste la preferencia; "auto" vuelve a consultar el idioma de la sesión.
    internal static string NormalizePreference(string? id) =>
        string.IsNullOrWhiteSpace(id) || string.Equals(id.Trim(), AutomaticLanguage, StringComparison.OrdinalIgnoreCase)
            ? AutomaticLanguage : NormalizeLanguage(id);

    internal static string ResolvePreference(string? preference, string? windowsUiCulture = null) =>
        NormalizePreference(preference) == AutomaticLanguage
            ? NormalizeLanguage(windowsUiCulture ?? CultureInfo.CurrentUICulture.Name)
            : NormalizeLanguage(preference);

    internal TextResolution Resolve(string? languageId, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var language = NormalizeLanguage(languageId);
        if (language != DefaultLanguage)
        {
            var translated = _resources.GetResourceSet(CultureInfo.GetCultureInfo(language), true, false)?.GetString(key);
            if (!string.IsNullOrWhiteSpace(translated)) return new(translated, language, false);
        }
        // Recurso neutro en español; fallback explícito, nunca la cultura del SO.
        var spanish = _resources.GetResourceSet(CultureInfo.InvariantCulture, true, false)?.GetString(key);
        return new(string.IsNullOrWhiteSpace(spanish) ? "⟦" + key + "⟧" : spanish,
            DefaultLanguage, language != DefaultLanguage);
    }

    internal string Get(string? languageId, string key) => Resolve(languageId, key).Value;

    internal IReadOnlyDictionary<string, string> DisplaySources() =>
        (_resources.GetResourceSet(CultureInfo.InvariantCulture, true, false)
            ?? throw new MissingManifestResourceException("Spanish resources missing"))
        .Cast<DictionaryEntry>().Where(x => ((string)x.Key).StartsWith("Display.", StringComparison.Ordinal))
        .ToDictionary(x => (string)x.Key, x => (string)x.Value!, StringComparer.Ordinal);

    internal string Format(string? languageId, string key, params object[] args) =>
        string.Format(CultureInfo.GetCultureInfo(NormalizeLanguage(languageId)), Get(languageId, key), args);

    // Extensión para futuros contadores; cada idioma define sus formas completas.
    internal string Count(string? languageId, string key, long count) =>
        Format(languageId, key + (count == 1 ? ".One" : ".Other"), count);
}
