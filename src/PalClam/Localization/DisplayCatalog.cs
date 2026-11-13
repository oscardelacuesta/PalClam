// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PalClam.Localization;

// Traduce exclusivamente la presentación. Modelos, estados, opciones y argumentos
// permanecen en su idioma/identificador original. Los parámetros son datos opacos.
internal sealed class DisplayCatalog
{
    private static readonly Regex Parameter = new(@"(?<!\{)\{(\d+)(?:,[^}:]+)?(?::[^}]+)?\}(?!\})",
        RegexOptions.CultureInvariant);
    private readonly TextCatalog _catalog;
    private readonly Dictionary<string, string> _exact = new(StringComparer.Ordinal);
    private readonly List<Template> _templates = new();
    private readonly Dictionary<string, int[]> _labelArguments;

    internal DisplayCatalog(TextCatalog? catalog = null)
    {
        _catalog = catalog ?? new TextCatalog();
        using var metadata = typeof(DisplayCatalog).Assembly.GetManifestResourceStream("PalClam.Localization.DisplayMetadata.json");
        _labelArguments = metadata is null ? new() : JsonSerializer.Deserialize<Dictionary<string, int[]>>(metadata) ?? new();
        foreach (var entry in _catalog.DisplaySources())
        {
            var matches = Parameter.Matches(entry.Value);
            if (matches.Count == 0)
            {
                _exact[Literal(entry.Value)] = entry.Key;
                continue;
            }
            var regex = new StringBuilder(@"\A");
            var position = 0;
            var indices = new List<int>();
            var literalLength = 0;
            foreach (Match match in matches)
            {
                var part = Literal(entry.Value[position..match.Index]);
                literalLength += part.Length;
                regex.Append(Regex.Escape(part));
                var index = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                indices.Add(index);
                regex.Append("(?<a").Append(index).Append(@">[\s\S]*?)");
                position = match.Index + match.Length;
            }
            var tail = Literal(entry.Value[position..]);
            literalLength += tail.Length;
            if (literalLength < 6) continue; // Evitar patrones que puedan ser datos arbitrarios.
            regex.Append(Regex.Escape(tail)).Append(@"\z");
            _templates.Add(new Template(entry.Key, Literal(entry.Value[..matches[0].Index]), literalLength,
                indices.Max() + 1, indices.ToArray(), new Regex(regex.ToString(),
                    RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking,
                    TimeSpan.FromMilliseconds(40))));
        }
        _templates.Sort((a, b) => b.LiteralLength.CompareTo(a.LiteralLength));
    }

    internal string Translate(string? source, string? languageId)
    {
        if (string.IsNullOrEmpty(source)) return source ?? "";
        var language = TextCatalog.NormalizeLanguage(languageId);
        if (language == "es") return source;
        if (_exact.TryGetValue(source, out var key)) return Literal(_catalog.Get(language, key));
        foreach (var template in _templates)
        {
            if (!source.StartsWith(template.Prefix, StringComparison.Ordinal)) continue;
            Match match;
            try { match = template.Pattern.Match(source); }
            catch (RegexMatchTimeoutException) { continue; }
            if (!match.Success) continue;
            var args = new object[template.ArgumentCount];
            Array.Fill(args, "");
            foreach (var index in template.Indices)
            {
                var value = match.Groups["a" + index].Value;
                // Solo argumentos marcados como etiquetas por el inventario sintáctico.
                // Rutas, nombres, hashes, mensajes externos y contenido de usuario no se traducen.
                if (_labelArguments.TryGetValue(template.Key, out var labels) && Array.IndexOf(labels, index) >= 0 &&
                    _exact.TryGetValue(value, out var labelKey)) value = Literal(_catalog.Get(language, labelKey));
                args[index] = value;
            }
            return string.Format(CultureInfo.GetCultureInfo(language), _catalog.Get(language, template.Key), args);
        }
        // Informes de varias líneas: las líneas técnicas que no sean mensajes conocidos quedan intactas.
        if (source.Contains('\n'))
            return string.Concat(Regex.Split(source, @"(\r?\n)").Select(line =>
                line is "\n" or "\r\n" ? line : Translate(line, language)));
        return source;
    }

    private static string Literal(string value) => value.Replace("{{", "{").Replace("}}", "}");
    private sealed record Template(string Key, string Prefix, int LiteralLength, int ArgumentCount,
        int[] Indices, Regex Pattern);
}

internal static class DisplayText
{
    private static readonly Lazy<DisplayCatalog> Catalog = new(() => new DisplayCatalog());
    internal static string Translate(string? source)
    {
        try { return Catalog.Value.Translate(source, UiText.Current.LanguageId); }
        catch (Exception ex) when (ex is FormatException or RegexMatchTimeoutException or System.Resources.MissingManifestResourceException)
        {
            // Un fallo de presentación no debe detener un análisis ni ocultar el mensaje original.
            return source ?? "";
        }
    }
}
