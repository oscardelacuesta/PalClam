// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PalClam.Infrastructure;
using PalClam.Localization;

namespace PalClam.LocalizationTests;

internal static class Program
{
    private static int _checks;

    private static int Main()
    {
        var folder = Path.Combine(Path.GetTempPath(), "PalClam-l10n-" + Guid.NewGuid().ToString("N"));
        try
        {
            var catalog = new TextCatalog();
            var manager = new ResourceManager("PalClam.Localization.Strings", typeof(TextCatalog).Assembly);
            var neutral = ReadSet(manager, CultureInfo.InvariantCulture);
            Check(neutral.Count >= 30, "Recursos neutros españoles presentes");
            foreach (var language in TextCatalog.Languages)
            {
                var culture = CultureInfo.GetCultureInfo(language.Id);
                var values = ReadSet(manager, language.Id == "es" ? CultureInfo.InvariantCulture : culture);
                Check(values.Keys.Order().SequenceEqual(neutral.Keys.Order()), "Cobertura completa: " + language.Id);
                foreach (var entry in neutral)
                {
                    var translated = values[entry.Key];
                    Check(!string.IsNullOrWhiteSpace(translated), "Texto no vacío: " + entry.Key);
                    var parsed = CompositeFormat.Parse(translated);
                    Check(Placeholders(entry.Value) == Placeholders(translated), "Parámetros preservados: " + entry.Key);
                    var args = Enumerable.Range(0, parsed.MinimumArgumentCount).Select(_ => (object)"TEST").ToArray();
                    _ = string.Format(culture, translated, args);
                    Check(!catalog.Resolve(language.Id, entry.Key).UsedSpanishFallback,
                        "Traducción directa: " + language.Id + "/" + entry.Key);
                }
                Check(catalog.Format(language.Id, "Help.GoTo", "ClamAV --scan C:\\prueba.txt").Contains("ClamAV --scan C:\\prueba.txt"),
                    "Argumentos técnicos conservados");
            }

            var fixture = new TextCatalog(new ResourceManager("PalClam.Fixtures.Fallback", typeof(Program).Assembly));
            var fallback = fixture.Resolve("en", "SpanishOnly");
            Check(fallback.Value == "Solo español" && fallback.LanguageId == "es" && fallback.UsedSpanishFallback,
                "Clave ausente usa español explícito");
            Check(fixture.Resolve("fr", "SpanishOnly").Value == "Solo español", "Satélite ausente usa español");
            Check(fixture.Get("en", "Unknown") == "⟦Unknown⟧", "Clave desconocida es visible");
            Check(fixture.Count("en", "Files", 1) == "1 file" && fixture.Count("en", "Files", 2) == "2 files",
                "Singular y plural parametrizados");
            foreach (var invalid in new string?[] { null, "", "zz", "../../fr", "ar" })
                Check(TextCatalog.NormalizeLanguage(invalid) == "es", "Cultura no soportada segura");
            Check(TextCatalog.NormalizeLanguage(" DE-de ") == "de" && TextCatalog.NormalizeLanguage("pt_BR") == "pt",
                "Culturas regionales normalizadas");

            // Serialización REAL de AppSettings; no escribir sobre el perfil del usuario.
            Directory.CreateDirectory(folder);
            var settingsPath = Path.Combine(folder, "settings.json");
            var settings = new AppSettings { ClamScanPath = "C:\\ClamAV\\clamscan.exe", ScanPriority = "Equilibrado", UiTheme = "oscuro" };
            var state = new UiText();
            var notifications = 0;
            state.PropertyChanged += (_, e) => { if (e.PropertyName == "Item[]") notifications++; };
            var oldCulture = CultureInfo.CurrentCulture;
            var oldUiCulture = CultureInfo.CurrentUICulture;
            foreach (var language in TextCatalog.Languages)
            {
                state.SaveAndSetLanguage(language.Id, id =>
                {
                    settings.UiLanguage = id;
                    File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings));
                });
                var restored = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(settingsPath))!;
                var restarted = new UiText();
                restarted.SetLanguage(restored.UiLanguage);
                Check(restarted["Header.Settings"] == catalog.Get(language.Id, "Header.Settings"), "Idioma tras reinicio");
                Check(restored.ClamScanPath == settings.ClamScanPath && restored.ScanPriority == "Equilibrado" && restored.UiTheme == "oscuro",
                    "Configuración técnica intacta");
            }
            Check(notifications == 5, "Notificación de las cinco transiciones de idioma");
            var before = state.LanguageId;
            try
            {
                state.SaveAndSetLanguage("de", _ => throw new IOException("disco no disponible"));
                throw new InvalidOperationException("Debió fallar la persistencia");
            }
            catch (IOException) { }
            Check(state.LanguageId == before && notifications == 5, "Persistencia fallida no aplica ni notifica");
            var legacy = JsonSerializer.Deserialize<AppSettings>("{\"UiTheme\":\"oscuro\"}")!;
            Check(legacy.UiLanguage == "auto" && legacy.UiTheme == "oscuro", "Migración sin idioma explícito usa detección Windows");
            Check(TextCatalog.ResolvePreference("auto", "en-US") == "en" &&
                TextCatalog.ResolvePreference("auto", "fr-CA") == "fr" &&
                TextCatalog.ResolvePreference("auto", "de-DE") == "de" &&
                TextCatalog.ResolvePreference("auto", "pt-BR") == "pt" &&
                TextCatalog.ResolvePreference("auto", "it-IT") == "it" &&
                TextCatalog.ResolvePreference("auto", "ca-ES") == "es", "Detección y fallback del idioma Windows");
            Check(TextCatalog.ResolvePreference("fr", "de-DE") == "fr" &&
                TextCatalog.ResolvePreference("es", "en-US") == "es", "Preferencia manual prioritaria");
            Check(TextCatalog.ResolvePreference(null, "it-IT") == "it" &&
                TextCatalog.NormalizePreference("") == "auto", "Perfil nuevo automático");
            var automatic = new UiText();
            string? storedPreference = null;
            automatic.SaveAndSetPreference("auto", id => storedPreference = id, "fr-FR");
            Check(storedPreference == "auto" && automatic.LanguageId == "fr", "Se persiste auto, no el idioma resuelto");

            var display = new DisplayCatalog(catalog);
            foreach (var source in catalog.DisplaySources())
            {
                var parsed = CompositeFormat.Parse(source.Value);
                var args = Enumerable.Range(0, parsed.MinimumArgumentCount).Select(i => (object)("__PC_ARG_" + i + "__")).ToArray();
                var input = string.Format(CultureInfo.InvariantCulture, source.Value, args);
                var literals = Regex.Replace(source.Value, @"\{\d+\}", "").Replace("{{", "{").Replace("}}", "}");
                if (args.Length > 0 && literals.Length < 6) continue;
                foreach (var language in TextCatalog.Languages)
                {
                    var expected = string.Format(CultureInfo.GetCultureInfo(language.Id), catalog.Get(language.Id, source.Key), args);
                    var actual = display.Translate(input, language.Id);
                    Check(actual == expected, "Presentación: " + source.Key + "/" + language.Id + " EXPECTED=" + expected + " ACTUAL=" + actual);
                }
            }
            foreach (var language in TextCatalog.Languages)
            {
                const string path = "C:\\Error\\Estado\\résumé_测试.txt";
                Check(display.Translate("Archivo inaccesible: " + path, language.Id).Contains(path), "Ruta opaca preservada");
                Check(display.Translate("https://api.example.test/v1?key=Error", language.Id) == "https://api.example.test/v1?key=Error",
                    "URL técnica intacta");
                Check(settings.ScanPriority == "Equilibrado" && settings.UiTheme == "oscuro", "Los valores internos no se traducen");
            }
            Check(display.Translate("PROTECCIÓN", "en") == "PROTECTION" &&
                display.Translate("CORREO", "fr") == "COURRIER" &&
                display.Translate("HERRAMIENTAS", "de") == "WERKZEUGE", "Menús completos en idiomas destino");
            Check(ReferenceEquals(CultureInfo.CurrentCulture, oldCulture) && ReferenceEquals(CultureInfo.CurrentUICulture, oldUiCulture),
                "Culturas de proceso no modificadas");
            Console.WriteLine($"[OK] {_checks} comprobaciones de recursos, fallback, formatos y persistencia.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[ERROR] Localización: " + ex);
            return 1;
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    private static Dictionary<string, string> ReadSet(ResourceManager manager, CultureInfo culture)
    {
        var set = manager.GetResourceSet(culture, true, false) ?? throw new InvalidOperationException("Falta satélite: " + culture.Name);
        return set.Cast<DictionaryEntry>().ToDictionary(x => (string)x.Key, x => (string)x.Value!);
    }

    private static string Placeholders(string value) => string.Join("|",
        Regex.Matches(value, @"(?<!\{)\{(\d+)(?:[, :][^{}]*)?\}(?!\})")
            .Select(x => x.Groups[1].Value).Order());

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        _checks++;
    }
}
