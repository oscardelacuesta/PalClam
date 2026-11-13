// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Text.Json;

namespace PalClam.Infrastructure;

internal sealed class AppSettings
{
    public string ClamScanPath { get; set; } = "";
    // Solo metadatos de IA; las claves nunca forman parte de settings.json.
    public string AiProviderId { get; set; } = "ollama";
    public string AiModelId { get; set; } = "";
    public string AiCustomBaseUrl { get; set; } = "";
    public string LastTarget { get; set; } = "";
    // Preferencias visuales de usuario. No afectan al motor antivirus.
    public string UiLanguage { get; set; } = PalClam.Localization.TextCatalog.AutomaticLanguage;
    public string UiTheme { get; set; } = PalClam.Services.AppearanceCatalog.DefaultId;
    public double UiTextScale { get; set; } = 1.00;
    public bool AnimateHeaderAurora { get; set; } = true;
    public string HeaderAuroraSpeed { get; set; } = PalClam.Services.AuroraMotionProfiles.DefaultId;
    public bool LaunchAtWindowsStartup { get; set; } = false;
    public bool CheckAppUpdatesOnStartup { get; set; } = true;
    public bool AutoInstallAppUpdates { get; set; } = false;
    public bool AutoUpdateSignatures { get; set; } = true;
    public bool AutoUpdateEngine { get; set; } = true;
    public bool ScanArchives { get; set; } = true;
    public bool ShowScanPerformance { get; set; } = true;
    // Cambia la prioridad de UN proceso clamscan; no crea hilos ni multiplica motores.
    public string ScanPriority { get; set; } = "Equilibrado";
    // "Equilibrado": 128 archivos/lote. "Preciso": 1 archivo/lote, mucho más lento.
    public string ScanControlMode { get; set; } = "Equilibrado";
    public bool ShowFloatingScanBar { get; set; } = false;
    public bool FloatingScanTopmost { get; set; } = true;
    public bool FloatingScanExpanded { get; set; } = false;
    public bool DetectPua { get; set; } = false;
    public bool ShowCleanResults { get; set; } = true;
    public bool ShowTechnicalConsole { get; set; } = false;
    public bool NotifyOnScanEnd { get; set; } = true;
    // Guard opt-in, solo funciona mientras PalClam está abierto; no hay bloqueo de archivos.
    public bool GuardStartWithApp { get; set; } = false;
    // Solo observación de cierres inesperados, nunca bloqueo ni reinicio automático.
    public bool SelfDefenseObserveUnexpectedExit { get; set; } = false;
    public bool GuardNotifyThreats { get; set; } = true;
    public int GuardMaxFileMb { get; set; } = 100;
    public string GuardProcessingMode { get; set; } = "Equilibrado";
    public List<string> GuardFolders { get; set; } = new();
    // Preview 19: estas dos funciones son voluntarias y NO bloquean la ejecución.
    public bool DownloadGuardEnabled { get; set; } = false;
    public bool UsbWatchEnabled { get; set; } = false;
    public bool UsbAutoScanEnabled { get; set; } = false;
    // PalClam Mail: no conecta a cuentas ni consulta buzones sin permiso.
    public bool MailIncludeSubdirectories { get; set; } = true;
    public bool MailExtractAttachments { get; set; } = true;
    public bool MailIncludeInline { get; set; } = true;
    public bool MailPhishingHeuristics { get; set; } = true;
    public bool MailCheckLinks { get; set; } = true;
    public bool MailSaveReport { get; set; } = false;
    public int MailMaxMessageMb { get; set; } = 50;
    public int MailMaxAttachmentMb { get; set; } = 25;
    public int MailMaxMessages { get; set; } = 200;
    public string MailLastPath { get; set; } = "";
    // Only public OAuth client id; NEVER store passwords or access tokens.
    public string OutlookClientId { get; set; } = "";
    public int OutlookMessageLimit { get; set; } = 10;
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public PalClam.Services.RansomwareOptions Ransomware { get; set; } = new();
    public PalClam.Services.DiskLedOptions DiskLed { get; set; } = new();
    public string ScheduledTarget { get; set; } = "Descargas";
    public int UpdateIntervalHours { get; set; } = 12;
    public List<string> ExcludedPaths { get; set; } = new();
    public DateTimeOffset LastEngineCheckUtc { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset LastSignatureCheckUtc { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset LastSignatureAttemptUtc { get; set; } = DateTimeOffset.MinValue;
    public string LastSignatureUpdateError { get; set; } = "";
}

internal static class SettingsStore
{
    private static readonly string Folder = Path.Combine(
        PalClam.Services.AppPaths.Root);
    private static readonly string FilePath = Path.Combine(Folder, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
                loaded.ExcludedPaths ??= new();
                loaded.LastSignatureUpdateError = (loaded.LastSignatureUpdateError ?? "").Trim();
                if (loaded.LastSignatureUpdateError.Length > 200) loaded.LastSignatureUpdateError = loaded.LastSignatureUpdateError[..200];
                loaded.UiTheme = PalClam.Services.AppearanceCatalog.Find(loaded.UiTheme).Id;
                loaded.UiLanguage = PalClam.Localization.TextCatalog.NormalizePreference(loaded.UiLanguage);
                loaded.UiTextScale = PalClam.Services.AppearanceCatalog.NormalizeScaleValue(loaded.UiTextScale);
                loaded.HeaderAuroraSpeed = PalClam.Services.AuroraMotionProfiles.Find(loaded.HeaderAuroraSpeed).Id;
                loaded.AiProviderId = PalClam.Services.AiProviderCatalog.Find(loaded.AiProviderId).Id;
                loaded.AiModelId ??= "";
                loaded.AiCustomBaseUrl ??= "";
                if (loaded.ScanPriority is not ("Económico" or "Equilibrado" or "Prioritario"))
                    loaded.ScanPriority = "Equilibrado";
                if (loaded.ScanControlMode is not ("Equilibrado" or "Preciso"))
                    loaded.ScanControlMode = "Equilibrado";
                loaded.GuardFolders ??= new();
                loaded.Ransomware = PalClam.Services.RansomwareOptions.Normalize(loaded.Ransomware);
                loaded.DiskLed = PalClam.Services.DiskLedOptions.Normalize(loaded.DiskLed);
                if (!loaded.UsbWatchEnabled) loaded.UsbAutoScanEnabled = false;
                if (loaded.OutlookMessageLimit is not (5 or 10 or 25 or 50)) loaded.OutlookMessageLimit = 10;
                if (loaded.MailMaxMessageMb is not (10 or 25 or 50 or 100)) loaded.MailMaxMessageMb = 50;
                if (loaded.MailMaxAttachmentMb is not (5 or 10 or 25 or 50)) loaded.MailMaxAttachmentMb = 25;
                if (loaded.MailMaxMessages is not (50 or 200 or 500)) loaded.MailMaxMessages = 200;
                if (loaded.GuardMaxFileMb is not (50 or 100 or 250)) loaded.GuardMaxFileMb = 100;
                loaded.GuardProcessingMode = PalClam.Services.GuardTuning.NormalizeMode(loaded.GuardProcessingMode);
                if (loaded.UpdateIntervalHours is not (6 or 12 or 24)) loaded.UpdateIntervalHours = 12;
                if (PalClam.Services.AppPaths.IsPortable) { loaded.AutoInstallAppUpdates = false; loaded.LaunchAtWindowsStartup = false; }
                return loaded;
            }
        }
        catch (Exception) { /* Configuracion corrupta o inaccesible: usar valores seguros. */ }
        return new();
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Folder);
        var destination = FilePath;
        var temp = destination + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, destination, overwrite: true);
    }
}
