// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PalClam.Services;

// Informacion local del disco. La mera presencia de un CVD/CLD no demuestra que
// ClamAV lo acepte ni que corresponda a la ultima version publicada.
internal sealed record SignatureFamilyFile(string Family, string FileName, long Bytes,
    DateTimeOffset ModifiedUtc);

internal sealed record SignatureFolderSnapshot(string Folder,
    IReadOnlyList<SignatureFamilyFile> Files, bool Accessible, string Detail)
{
    public bool MainAndDailyPresent => Accessible &&
        Files.Any(x => x.Family == "main") && Files.Any(x => x.Family == "daily");
    public string Describe()
    {
        if (!Accessible) return "No verificable: " + Detail;
        if (!MainAndDailyPresent) return "Incompleta: falta main o daily. " + Detail;
        var daily = Files.First(x => x.Family == "daily");
        var age = DateTimeOffset.UtcNow - daily.ModifiedUtc;
        var dateNote = age > TimeSpan.FromDays(7) ? " · fecha local de daily antigua" : "";
        return "main y daily presentes" + dateNote +
            " · no equivale a firmas validadas ni actualizadas";
    }
}

internal static class SignatureCenterInventory
{
    internal static SignatureFolderSnapshot Read(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Path.IsPathFullyQualified(folder))
            return new(folder ?? "", Array.Empty<SignatureFamilyFile>(), false,
                "La carpeta no es una ruta absoluta.");
        try
        {
            if (!Directory.Exists(folder))
                return new(folder, Array.Empty<SignatureFamilyFile>(), true,
                    "Todavia no existe una carpeta de firmas.");
            var files = new List<SignatureFamilyFile>();
            foreach (var family in new[] { "main", "daily", "bytecode" })
            {
                // FreshClam puede generar CLD y CVD; se usa una sola de cada familia.
                foreach (var extension in new[] { ".cld", ".cvd" })
                {
                    var path = Path.Combine(folder, family + extension);
                    if (!File.Exists(path)) continue;
                    var attributes = File.GetAttributes(path);
                    if ((attributes & FileAttributes.ReparsePoint) != 0) continue;
                    var info = new FileInfo(path);
                    if (info.Length < 128) continue;
                    files.Add(new(family, info.Name, info.Length,
                        new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero)));
                    break;
                }
            }
            return new(folder, files, true, files.Count == 0 ? "Sin bases locales." :
                "Archivos leidos sin modificar el directorio.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            ArgumentException or System.Security.SecurityException)
        {
            return new(folder, Array.Empty<SignatureFamilyFile>(), false, ex.GetType().Name);
        }
    }
}
