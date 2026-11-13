// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PalClam.Services;

// Paletas de UI. La semántica de alarma/estado permanece en todos los temas;
// las variantes oscuras ajustan los tonos para mantener el contraste.
// Sin referencias WPF: contrastes, persistencia y valores desconocidos son comprobables en SmokeTests.
internal sealed record AppearancePalette(string Id, string Name,
    string Accent, string AccentPale, string AccentBorder, string AccentText,
    string Background, string Surface, string Sidebar, string Outline,
    string Ink, string Muted, string Slate,
    string HeroFrom, string HeroTo, string HeroTile);

internal static class AppearanceCatalog
{
    public const string DefaultId = "morado";
    public static readonly IReadOnlyList<AppearancePalette> Themes = new[]
    {
        new AppearancePalette("morado", "Morado clásico", "#6940B3", "#F0EAFB", "#E1D9F5", "#382678", "#F4F6FC", "#FFFFFF", "#FBFCFF", "#E5E9F2", "#172034", "#68738C", "#293552", "#182C55", "#6843BE", "#304A82"),
        new AppearancePalette("oceano", "Azul océano", "#205D9A", "#E7F1FC", "#C9DFF4", "#174A7A", "#F2F7FC", "#FFFFFF", "#F7FBFE", "#DBE7F2", "#192A3F", "#53677C", "#2B4865", "#153853", "#277BA8", "#2B5B7F"),
        new AppearancePalette("esmeralda", "Verde esmeralda", "#176C5C", "#E5F4EF", "#CAE5D9", "#155A4E", "#F3F8F5", "#FFFFFF", "#F8FCF9", "#DDEAE3", "#1B302B", "#536E63", "#314D43", "#154837", "#278E76", "#245E52"),
        new AppearancePalette("cobre", "Cobre elegante", "#924B30", "#FAEEE8", "#F0D7CB", "#743820", "#FBF7F4", "#FFFFFF", "#FEFAF7", "#ECE0D8", "#322722", "#765D52", "#5B4038", "#59352F", "#A76B49", "#6A483A"),
        new AppearancePalette("crema", "Crema y ciruela", "#754A8F", "#F5EDF9", "#E6D8F0", "#603B76", "#FFF9F1", "#FFFEFC", "#FFFAF4", "#EDE2D8", "#332940", "#766779", "#51435D", "#493257", "#9067A6", "#655074"),
        new AppearancePalette("grafito", "Grafito claro", "#3B536B", "#E9EEF4", "#D4DFE9", "#304357", "#F1F4F7", "#FFFFFF", "#F8FAFC", "#DCE3EA", "#1D2C3C", "#526478", "#35495F", "#263747", "#516B82", "#364D62"),
        // Fondo oscuro auténtico: gris azulado + violeta suave, no un filtro de pantalla.
        new AppearancePalette("oscuro", "Oscuro violeta", "#C5A8F7", "#302641", "#55416B", "#F1E8FF", "#121826", "#1B2032", "#171E2E", "#38435C", "#F4F0FF", "#AFBBD0", "#DBE3F5", "#10172A", "#3A285F", "#263554")
    };

    public static AppearancePalette Find(string? id) =>
        Themes.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)) ?? Themes[0];

    public static string NormalizeScale(double scale) =>
        SupportedScales.Any(x => Math.Abs(x - scale) < 0.001)
            ? scale.ToString("0.00", CultureInfo.InvariantCulture) : "1.00";

    public static readonly double[] SupportedScales = { 0.90, 1.00, 1.10, 1.20, 1.30 };
    public static double NormalizeScaleValue(double scale) =>
        SupportedScales.FirstOrDefault(x => Math.Abs(x - scale) < 0.001, 1.00);

    // Mapeo de colores base a tokens dinámicos. Todas las pantallas consumen
    // el mismo objeto SolidColorBrush para que un cambio de tema se refleje sin recrear vistas.
    public static string MapColor(string? themeId, string original)
    {
        var p = Find(themeId);
        if (p.Id == DefaultId) return original;
        // La paleta oscura necesita también cambiar fondos semánticos (éxito,
        // alerta, información). Conservar el color claro original en un fondo
        // oscuro rompería contraste, por eso se trata de modo separado.
        if (p.Id == "oscuro") return MapDarkColor(p, original);
        return original.ToUpperInvariant() switch
        {
            "#6940B3" => p.Accent,
            "#F0EAFB" or "#F2EDFB" or "#F3EEFC" => p.AccentPale,
            "#E1D9F5" or "#E5D8FF" or "#E5D9FA" => p.AccentBorder,
            "#382678" => p.AccentText,
            "#F4F6FC" => p.Background,
            "#FFFFFF" => p.Surface,
            "#FBFCFF" => p.Sidebar,
            "#E5E9F2" => p.Outline,
            "#172034" => p.Ink,
            "#68738C" => p.Muted,
            "#293552" => p.Slate,
            "#182C55" => p.HeroFrom,
            "#6843BE" => p.HeroTo,
            "#304A82" or "#415D93" => p.HeroTile,
            "#F0F3FA" or "#F8F9FD" or "#FCFCFE" => p.Sidebar,
            "#E7EDF9" => p.Outline,
            "#D0DBFF" => "#E4ECF8",
            "#E7ECFF" => "#F3F7FC",
            "#93A7D5" => p.AccentBorder,
            "#F7F9FF" => p.Sidebar,
            _ => original
        };
    }

    private static string MapDarkColor(AppearancePalette p, string original) =>
        original.ToUpperInvariant() switch
        {
            "#6940B3" => p.Accent,
            "#F0EAFB" or "#F2EDFB" or "#F3EEFC" => p.AccentPale,
            "#E1D9F5" or "#E5D8FF" or "#E5D9FA" => p.AccentBorder,
            "#382678" => p.AccentText,
            "#F4F6FC" => p.Background,
            "#FFFFFF" => p.Surface,
            "#FBFCFF" or "#F0F3FA" or "#F8F9FD" or "#FCFCFE" or "#F7F9FF" => p.Sidebar,
            "#E5E9F2" or "#E7EDF9" => p.Outline,
            "#172034" or "#182238" => p.Ink,
            "#68738C" => p.Muted,
            "#293552" or "#334466" => p.Slate,
            "#182C55" => p.HeroFrom,
            "#6843BE" => p.HeroTo,
            "#304A82" or "#415D93" => p.HeroTile,
            "#D0DBFF" => "#C9D7F8",
            "#E7ECFF" => "#DDE7FF",
            "#93A7D5" => "#8799BB",
            // Conservan su significado semántico con contraste suficiente.
            "#2167D3" => "#92BFFF",
            "#EAF2FF" => "#233852",
            "#16815C" or "#207454" => "#80D9AC",
            "#E5F6EE" => "#203B31",
            "#9F650A" or "#966023" => "#F6C770",
            "#FFF3D9" => "#392D1B",
            "#C82B42" or "#A23E3E" or "#AE1C30" => "#F0B0C6",
            "#FDE9ED" or "#FDE8E8" => "#422432",
            "#F4B8C1" => "#D58DA6",
            "#EDC979" => "#C4A25B",
            _ => original
        };

    // WCAG 2.x: contraste relativo. Test automatizado, también comprueba texto de navegación.
    public static double Contrast(string foreground, string background)
    {
        static double Luminance(string hex)
        {
            var rgb = Enumerable.Range(0, 3).Select(i =>
            {
                var v = int.Parse(hex.Substring(1 + i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
                return v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4);
            }).ToArray();
            return .2126 * rgb[0] + .7152 * rgb[1] + .0722 * rgb[2];
        }
        var a = Luminance(foreground); var b = Luminance(background);
        return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
    }
}
