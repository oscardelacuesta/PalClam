// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PalClam.Infrastructure;

/// <summary>Claves cifradas con DPAPI CurrentUser; nunca se guardan en settings.json.</summary>
internal static class AiSecretsStore
{
    private static readonly string FilePath = Path.Combine(
        PalClam.Services.AppPaths.Root, "ai-secrets.dpapi.json");
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("PalClam AI API Secrets v1");

    public static string? Load(string providerId)
    {
        try
        {
            var all = Read();
            if (!all.TryGetValue(providerId, out var blob)) return null;
            var bytes = ProtectedData.Unprotect(Convert.FromBase64String(blob), Entropy,
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception) { return null; } // Clave de otro usuario o almacén inaccesible.
    }

    public static bool HasSavedKey(string providerId) => !string.IsNullOrWhiteSpace(Load(providerId));

    public static void Save(string providerId, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret)) throw new ArgumentException("Clave vacía.");
        if (secret.Length > 8192) throw new ArgumentException("Clave excesivamente larga.");
        var all = Read();
        var plaintext = Encoding.UTF8.GetBytes(secret.Trim());
        try
        {
            var protectedBytes = ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser);
            all[providerId] = Convert.ToBase64String(protectedBytes);
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
        Write(all);
    }

    public static void Delete(string providerId)
    {
        var all = Read();
        if (all.Remove(providerId)) Write(all);
    }

    private static Dictionary<string, string> Read()
    {
        if (!File.Exists(FilePath)) return new(StringComparer.Ordinal);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(FilePath))
            ?? new(StringComparer.Ordinal);
    }

    private static void Write(Dictionary<string, string> all)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(all));
        File.Move(temp, FilePath, overwrite: true);
    }
}
