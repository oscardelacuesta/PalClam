// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.MachineService;

/// <summary>Pure validation: a directory prefix is not enough for containment.</summary>
internal static class MachineGuardPolicy
{
    internal static bool IsInside(string root, string candidate)
    {
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(candidate)) return false;
        try
        {
            var basePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            var full = Path.GetFullPath(candidate);
            return full.StartsWith(basePath + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        { return false; }
    }

    // "Vigilando" requiere tanto el escáner como las bases compartidas presentes.
    // No afirma que estén actualizadas ni que la vigilancia sea preventiva.
    internal static string? CoreIssue(bool scannerAvailable, bool signaturesAvailable)
    {
        if (!scannerAvailable) return "Motor ClamAV no disponible: comprueba el instalador.";
        if (!signaturesAvailable) return "Firmas compartidas ausentes o incompletas.";
        return null;
    }

    internal static bool CanScan(long length, bool isLink) =>
        length >= 0 && length <= 100L * 1024 * 1024 && !isLink;

    internal static int SelfTest()
    {
        const string root = @"C:\ProgramData\PalClam\GuardWatch";
        var checks = new[]
        {
            ("hijo permitido", IsInside(root, root + @"\archivo.txt")),
            ("carpeta vecina rechazada", !IsInside(root, root + "Extra\\archivo.txt")),
            ("raiz no analizada", !IsInside(root, root)),
            ("rutas ajenas rechazadas", !IsInside(root, @"C:\Users\Public\archivo.exe")),
            ("archivo grande omitido", !CanScan(101L * 1024 * 1024, false)),
            ("enlace omitido", !CanScan(200, true)),
            ("motor ausente no operativo", CoreIssue(false, true) is not null),
            ("firmas ausentes no operativo", CoreIssue(true, false) is not null),
            ("componentes presentes sin fallo de presencia", CoreIssue(true, true) is null)
        };
        foreach (var (name, passed) in checks)
        {
            Console.WriteLine((passed ? "[OK] " : "[ERROR] ") + "servicio: " + name);
            if (!passed) return 1;
        }
        return 0;
    }
}
