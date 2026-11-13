// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.IO;
using System.Runtime.InteropServices;

namespace PalClam.Services;

internal static class ProductLinks
{
    internal const string Website = "https://palclam.com/";
    // El dominio se prepara como web de producto. No se contacta automáticamente hasta publicar un canal firmado.
    internal const string FutureSignedFeed = "https://palclam.com/updates/latest.json";
    internal const string AuthorWebsite = "https://www.palentino.es/";
}

internal sealed record StorageLayout(bool Portable, string Root);
internal static class DistributionPolicy
{
    internal const string Marker = "palclam.portable";
    internal static StorageLayout ResolveStorage(string baseDirectory, string localData, bool markerPresent) =>
        new(markerPresent, Path.GetFullPath(markerPresent ? Path.Combine(baseDirectory, "Data") : Path.Combine(localData, "PalClam")));
    internal static string RuntimeId(Architecture architecture) => architecture switch
    {
        Architecture.X86 => "win-x86", Architecture.X64 => "win-x64",
        _ => throw new PlatformNotSupportedException("Arquitectura no disponible en esta edición.")
    };
    internal static string EngineSuffix(string rid) => rid switch
    { "win-x86" => "win32", "win-x64" => "x64", _ => throw new ArgumentException("Arquitectura no disponible en esta edición.") };
    internal static string EngineSha256(string rid) => rid switch
    {
        "win-x86" => "3b54e5f6b0139f65bffe732fed052d38b351374cb86da395c1cf505494154f89",
        "win-x64" => "0d9e0228b2674137ea1a2853566c98a0278ad52ab2582c3d6dbd75373848c395",
        _ => throw new ArgumentException("Arquitectura no disponible en esta edición.")
    };
    internal static bool MatchesExecutable(string path, string rid)
    {
        try
        {
            using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new BinaryReader(input);
            if (input.Length < 64 || reader.ReadUInt16() != 0x5A4D) return false;
            input.Position = 0x3c; var offset = reader.ReadInt32();
            if (offset < 64 || offset > input.Length - 6) return false;
            input.Position = offset;
            if (reader.ReadUInt32() != 0x4550) return false;
            var machine = reader.ReadUInt16();
            return rid switch { "win-x86" => machine == 0x14c, "win-x64" => machine == 0x8664, _ => false };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { return false; }
    }
}

internal static class AppPaths
{
    private static readonly StorageLayout Layout = DistributionPolicy.ResolveStorage(AppContext.BaseDirectory,
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), File.Exists(Path.Combine(AppContext.BaseDirectory, DistributionPolicy.Marker)));
    internal static bool IsPortable => Layout.Portable;
    internal static string Root => Layout.Root;
    internal static string RuntimeId => DistributionPolicy.RuntimeId(RuntimeInformation.ProcessArchitecture);
    internal static string EngineSuffix => DistributionPolicy.EngineSuffix(RuntimeId);
    internal static string EngineSha256 => DistributionPolicy.EngineSha256(RuntimeId);
    internal static string PrivateTemp => IsPortable ? Path.Combine(Root, "Temp") : Path.GetTempPath();
    internal static void Prepare()
    {
        if (!IsPortable) return;
        Directory.CreateDirectory(Root); Directory.CreateDirectory(PrivateTemp);
        var probe = Path.Combine(Root, ".write-probe-" + Guid.NewGuid().ToString("N"));
        using var file = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
    }
    internal static string InstanceName => "Local\\PalClam-" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(Root.ToUpperInvariant())))[..24];
}
