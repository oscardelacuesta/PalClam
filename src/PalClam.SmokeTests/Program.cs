// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using PalClam.Services;
using PalClam.Infrastructure;

static class SmokeTests
{
    private static int _passed;
    private static void Assert(bool ok, string name)
    {
        if (!ok) throw new InvalidOperationException("Prueba fallida: " + name);
        Console.WriteLine("[OK] " + name);
        _passed++;
    }

    private static async Task<int> Main()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "PalClam-SmokeTest-" + Guid.NewGuid().ToString("N"));
        try
        {
            // Preview 24: lista blanca para AI Security Analyst (sin archivos, rutas ni secretos).
            const string privateValue = "SENSITIVE_SENTINEL_123";
            var aiScan = AiSecurityAnalyst.ForScan(new ScanRecord {
                Target = @"C:\Users\Private\" + privateValue, LogPath = privateValue,
                Threats = new[] { privateValue }, Outcome = privateValue,
                ExitCode = 1, Detections = 2, EngineScannedFiles = 42, Seconds = 4
            });
            Assert(aiScan.Contains("Detecciones reportadas: 2") &&
                !aiScan.Contains(privateValue) && !aiScan.Contains(@"C:\Users"),
                "AI Analyst: resumen de análisis sin rutas, firmas ni texto privado");
            var aiNotice = new SecurityNotice(DateTimeOffset.UtcNow,
                SecurityNoticeLevel.Threat, "guard", privateValue, privateValue, privateValue);
            var aiAlerts = AiSecurityAnalyst.ForNotices(new SecurityNoticeFeedResult(
                new[] { aiNotice }, 1, 2, true));
            Assert(aiAlerts.Contains("Detecciones: 1") && aiAlerts.Contains("Lectura limitada: sí") &&
                !aiAlerts.Contains(privateValue),
                "AI Analyst: Guard agregado sin nombres ni detalles de incidentes");
            Assert(AiSecurityAnalyst.ForNotice(aiNotice).Contains("detección ClamAV") &&
                !AiSecurityAnalyst.ForNotice(aiNotice).Contains(privateValue),
                "AI Analyst: alerta seleccionada usa origen y gravedad de lista blanca");
            var aiHealth = AiSecurityAnalyst.ForHealth(new[] {
                new HealthFinding("Motor ClamAV", HealthLevel.Attention, privateValue),
                new HealthFinding(privateValue, HealthLevel.Ready, privateValue)
            });
            Assert(aiHealth.Contains("Motor ClamAV: requiere revisión") &&
                !aiHealth.Contains(privateValue),
                "AI Analyst: salud no incorpora detalles ni nombres ajenos a catálogo");
            var aiQuarantine = AiSecurityAnalyst.ForQuarantine(new[] {
                new QuarantineEntry { OriginalPath = privateValue, Detection = privateValue,
                    Sha256 = privateValue, IntegrityWarning = true, RequestedManualReview = true }
            });
            Assert(aiQuarantine.Contains("Con advertencia de integridad: 1") &&
                !aiQuarantine.Contains(privateValue),
                "AI Analyst: cuarentena solo contadores, sin SHA ni rutas");
            Assert(AiSecurityAnalyst.ForScan(null).Contains("ausencia de historial") &&
                AiSecurityAnalyst.ForHealth(Array.Empty<HealthFinding>()).Contains("No se ha ejecutado"),
                "AI Analyst: ausencia de datos no produce diagnóstico inventado");
            Assert(AiSecurityAnalyst.MayContainPrivateData(@"C:\Users\nombre\secreto.txt") &&
                AiSecurityAnalyst.MayContainPrivateData("escribe a usuario@example.com") &&
                !AiSecurityAnalyst.MayContainPrivateData("Detecciones: 2; errores: 0"),
                "AI Analyst: advertencia de privacidad para consultas manuales remotas");

            // RC1: la aurora visual permite tres niveles y normaliza valores persistidos.
            Assert(AuroraMotionProfiles.All.Count == 3 &&
                   AuroraMotionProfiles.Find(null).Id == AuroraMotionProfiles.DefaultId &&
                   AuroraMotionProfiles.Find("nivel-desconocido").Id == AuroraMotionProfiles.DefaultId,
                "Aurora: selector acotado, valor por defecto y recuperación de preferencias");
            Assert(AuroraMotionProfiles.DurationSeconds(22, "Equilibrada") < 22 &&
                   AuroraMotionProfiles.DurationSeconds(22, "Dinamica") <
                   AuroraMotionProfiles.DurationSeconds(22, "Equilibrada") &&
                   AuroraMotionProfiles.DurationSeconds(22, "Suave") == 22,
                "Aurora: velocidad configurable y más visible por defecto");

            // Preview 24: panel de lectura, sin cifras inventadas ni escrituras en los registros.
            var dashboardNow = new DateTimeOffset(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);
            var dashboardScans = new[] {
                new ScanRecord { CompletedAt = dashboardNow.AddMinutes(-1),
                    Outcome = "Amenazas detectadas", ExitCode = 1, Detections = 2, Seconds = 12,
                    MetricsEnabled = true, PerformanceSampleCount = 3, AverageEngineCpuPercent = 8.5 },
                new ScanRecord { CompletedAt = dashboardNow.AddMinutes(-8),
                    Outcome = "Incompleto", ExitCode = null, Errors = 1, Seconds = 18 },
                new ScanRecord { CompletedAt = dashboardNow.AddMinutes(12),
                    Outcome = "Futuro", ExitCode = 0, Seconds = 1 }
            };
            var dashboardFeed = new SecurityNoticeFeedResult(new [] {
                new SecurityNotice(dashboardNow.AddMinutes(-3), SecurityNoticeLevel.Threat,
                    "guard", "Guard", "Test", "C:\\temp\\sample.txt"),
                new SecurityNotice(dashboardNow.AddMinutes(-4), SecurityNoticeLevel.Warning,
                    "machine", "Machine Guard", "Revisar cobertura", "")
            }, 1, 2, true);
            var dashboard = DashboardOverviewPolicy.Build(dashboardScans, dashboardFeed,
                dashboardNow, dashboardNow.AddMinutes(-3.5));
            Assert(dashboard.ScanCount == 2 && dashboard.DetectionsInScans == 2 &&
                dashboard.IncompleteScans == 1 && dashboard.LastScanWithMetrics is not null,
                "Dashboard 2.0: recuentos solo de análisis válidos y métricas medidas");
            Assert(dashboard.NoticeThreats == 1 && dashboard.NoticeWarnings == 1 &&
                dashboard.UnreviewedNotices == 1 && dashboard.HasPartialData,
                "Dashboard 2.0: avisos sin revisar y lecturas parciales visibles");
            var dashboardEmpty = DashboardOverviewPolicy.Build(Array.Empty<ScanRecord>(),
                new SecurityNoticeFeedResult(Array.Empty<SecurityNotice>(), 0, 0, false),
                dashboardNow, DateTimeOffset.MinValue);
            Assert(dashboardEmpty.ScanCount == 0 && dashboardEmpty.LastScanWithMetrics is null &&
                !dashboardEmpty.HasPartialData && dashboardEmpty.RecentNotices.Count == 0,
                "Dashboard 2.0: sin registros no se fabrican resultados ni porcentajes");
            var dashboardFixture = Path.Combine(tmp, "dashboard-history.jsonl");
            Directory.CreateDirectory(Path.GetDirectoryName(dashboardFixture)!);
            await File.WriteAllTextAsync(dashboardFixture,
                System.Text.Json.JsonSerializer.Serialize(dashboardScans[0]) + "\n" +
                "{json-incompleto\n" +
                System.Text.Json.JsonSerializer.Serialize(dashboardScans[1]) + "\n");
            var dashboardRead = DashboardOverviewReader.ReadHistoryTail(dashboardFixture,
                out var dashboardTruncated, out var dashboardUnreadable, out var invalidHistory);
            Assert(dashboardRead.Count == 2 && !dashboardTruncated && !dashboardUnreadable &&
                invalidHistory == 1,
                "Dashboard 2.0: historial con JSON dañado mantiene registros válidos");
            Assert(DashboardOverviewPolicy.Build(dashboardRead,
                new SecurityNoticeFeedResult(Array.Empty<SecurityNotice>(), 0, 0, false),
                dashboardNow, DateTimeOffset.MinValue,
                dashboardTruncated, dashboardUnreadable, invalidHistory).HasPartialData,
                "Dashboard 2.0: historia corrupta visible como dato parcial");
            Assert(DashboardOverviewPolicy.Build(dashboardRead,
                new SecurityNoticeFeedResult(Array.Empty<SecurityNotice>(), 0, 0, false),
                dashboardNow, DateTimeOffset.MinValue,
                dashboardTruncated, dashboardUnreadable, invalidHistory).HasPartialData,
                "Dashboard 2.0: historia corrupta visible como dato parcial");
            var missingDashboard = DashboardOverviewReader.ReadHistoryTail(
                Path.Combine(tmp, "missing-dashboard.jsonl"), out _, out var missingUnreadable);
            Assert(missingDashboard.Count == 0 && !missingUnreadable,
                "Dashboard 2.0: ausencia de historial no es un análisis correcto");

            // Preview 20: inventario local real, sin invocar FreshClam ni modificar firmas.
            var sigTest = Path.Combine(tmp, "signature-center-fixtures");
            Directory.CreateDirectory(sigTest);
            var none = SignatureCenterInventory.Read(sigTest);
            Assert(none.Accessible && !none.MainAndDailyPresent &&
                   none.Describe().Contains("Incompleta", StringComparison.OrdinalIgnoreCase),
                "Signature Center: ausencia de bases no equivale a disponibilidad");
            await File.WriteAllBytesAsync(Path.Combine(sigTest, "main.cvd"), new byte[256]);
            await File.WriteAllBytesAsync(Path.Combine(sigTest, "daily.cvd"), new byte[256]);
            await File.WriteAllBytesAsync(Path.Combine(sigTest, "daily.cld"), new byte[256]);
            await File.WriteAllBytesAsync(Path.Combine(sigTest, "bytecode.cvd"), new byte[4]);
            // Una base truncada no debe contar como presente.
            var present = SignatureCenterInventory.Read(sigTest);
            Assert(present.MainAndDailyPresent && present.Files.Count == 2 &&
                   present.Files.Any(x => x.FileName == "daily.cld"),
                "Signature Center: main y daily sin contar dos veces CLD/CVD");
            Assert(present.Describe().Contains("no equivale a firmas validadas"),
                "Signature Center: inventario no declara firmas oficiales sin sigtool");
            var invalidDir = SignatureCenterInventory.Read("ruta-relativa");
            Assert(!invalidDir.Accessible && !invalidDir.MainAndDailyPresent,
                "Signature Center: rechaza rutas relativas o sin directorio absoluto");
            Assert(ControlledScanRunner.BalancedBatchSize == 128 &&
                   ControlledScanRunner.PreciseBatchSize == 1,
                "control: modo equilibrado y preciso no ocultan su coste");
            // Preview 22: perfiles, raíces agregadas y ETA sin garantías ficticias.
            var testRoot = Path.Combine(tmp, "smart-scan");
            var sibling = Path.Combine(tmp, "smart-scan-other");
            Directory.CreateDirectory(Path.Combine(testRoot, "child"));
            Directory.CreateDirectory(sibling);
            await File.WriteAllTextAsync(Path.Combine(testRoot, "file.txt"), "inocuo");
            await File.WriteAllTextAsync(Path.Combine(testRoot, "child", "nested.txt"), "inocuo");
            await File.WriteAllTextAsync(Path.Combine(sibling, "other.txt"), "inocuo");
            var collapsed = SmartScanPlanPolicy.CollapseTargets(new[]
            { testRoot, Path.Combine(testRoot, "child"), testRoot + Path.DirectorySeparatorChar, sibling });
            Assert(collapsed.Count == 2 && collapsed.Contains(testRoot) && collapsed.Contains(sibling),
                "Smart Scan: sin duplicados ni carpetas contenidas en otra raíz");
            var fastPlan = SmartScanPlanPolicy.Create(SmartScanPreset.Quick, collapsed, "Prioritario", "Preciso");
            var full = SmartScanPlanPolicy.Create(SmartScanPreset.Full, collapsed, "Prioritario", "Preciso");
            var custom = SmartScanPlanPolicy.Create(SmartScanPreset.Custom, new[] { testRoot }, "Prioritario", "Preciso");
            Assert(fastPlan.Priority == "Equilibrado" && fastPlan.BatchSize == 128 &&
                   full.Priority == "Económico" && full.BatchSize == 256 &&
                   custom.Priority == "Prioritario" && custom.BatchSize == 1,
                "Smart Scan: prioridades y lotes efectivos sin alterar firmas");
            Assert(ScanFileEstimator.Estimate(collapsed, CancellationToken.None).Count == 3 &&
                   ScanFileEstimator.Estimate(collapsed, CancellationToken.None, new[] { sibling }).Count == 2,
                "Smart Scan: estimación conjunta con exclusiones sin doble recuento");
            Assert(SmartScanPlanPolicy.CollapseTargets(Enumerable.Repeat(testRoot, 80)).Count == 1,
                "Smart Scan: objetivo repetido no amplifica la carga");

            // Preview 19: políticas probadas sin ejecutar motor ni servicios.
            var healthTime = new DateTimeOffset(2026, 11, 11, 11, 0, 0, TimeSpan.Zero);
            var unhealthy = ProtectionHealthPolicy.Evaluate(new HealthInputs(
                false, SignatureHealthState.Missing, false, false,
                true, false, true, false, true, null, false, null, healthTime));
            Assert(unhealthy.Any(x => x.Component == "Motor ClamAV" && x.Level == HealthLevel.Attention) &&
                   unhealthy.Any(x => x.Component == "Firmas locales" && x.Level == HealthLevel.Attention) &&
                   unhealthy.Any(x => x.Component == "Servicio de equipo" && x.Level == HealthLevel.Unknown),
                "salud 360: falta de motor, firmas y servicio no equivale a operativo");
            Assert(unhealthy.Any(x => x.Component == "Download Guard" && x.Level == HealthLevel.Attention) &&
                   unhealthy.Any(x => x.Component == "USB Watch" && x.Level == HealthLevel.Attention),
                "salud 360: preferencias sin observadores no muestran cobertura");
            Assert(ProtectionHealthPolicy.Summary(unhealthy).StartsWith("ATENCIÓN"),
                "salud 360: prioridad a componentes que requieren revisión");
            var healthy = ProtectionHealthPolicy.Evaluate(new HealthInputs(
                true, SignatureHealthState.Available, true, false,
                true, true, true, true, false, null, true, true, healthTime));
            Assert(healthy.Any(x => x.Component == "Servicio de equipo" && x.Level == HealthLevel.Inactive) &&
                   healthy.Any(x => x.Component == "Guard personal" && x.Level == HealthLevel.Ready) &&
                   !ProtectionHealthPolicy.Summary(healthy).Contains("preventiva activa"),
                "salud 360: servicio opcional inactivo no equivale a protección preventiva");
            var degraded = ProtectionHealthPolicy.Evaluate(new HealthInputs(
                true, SignatureHealthState.Available, true, true,
                false, false, false, false, false, null, false, false, healthTime));
            Assert(degraded.Any(x => x.Component == "Guard personal" && x.Level == HealthLevel.Attention),
                "salud 360: Guard degradado se señala");
            var volumeMonitor = new RemovableDriveMonitor();
            var f = new RemovableVolume(@"F:\", "USB F");
            var g = new RemovableVolume(@"G:\", "USB G");
            Assert(volumeMonitor.Observe(new[] { f }).Count == 0 &&
                   volumeMonitor.Observe(new[] { f, g }).Count == 1,
                "USB Watch: no analiza unidades previas al inicio");
            Assert(volumeMonitor.Observe(new[] { f, g }).Count == 0 &&
                   volumeMonitor.Observe(new[] { f }).Count == 0 &&
                   volumeMonitor.Observe(new[] { f, g }).Count == 1,
                "USB Watch: sin duplicados y reinserción detectable");
            Assert(RemovableDriveMonitor.IsDriveRoot(@"F:\") &&
                   !RemovableDriveMonitor.IsDriveRoot(@"F:\Documents\demo.exe"),
                "USB Watch: solo raíces de unidad para análisis");
            Assert(DownloadGuardPolicy.Compose(new[] { @"C:\Users\Prueba\Datos" }, true,
                    @"C:\Users\Prueba\Downloads").Count == 2 &&
                   DownloadGuardPolicy.Compose(new[] { @"C:\Users\Prueba" }, true,
                    @"C:\Users\Prueba\Downloads").Count == 1,
                "Descargas: una sola cola y sin raíces duplicadas bajo padres vigilados");
            var sampleDevice = new DeviceWatchRecord(healthTime, "Information", @"F:\",
                "Unidad detectada; no implica amenaza");
            var parsedDevice = SecurityNoticeFeed.ParseDevice(
                System.Text.Json.JsonSerializer.Serialize(sampleDevice));
            Assert(parsedDevice is not null && parsedDevice.DisplaySource == "USB Watch" &&
                   parsedDevice.Level == SecurityNoticeLevel.Information,
                "alertas: USB se registra como información, no amenaza");
            Assert(SecurityNoticeFeed.ParseDevice("{JSON roto") is null,
                "alertas: JSON USB dañado no rompe el historial");
            var gate = new ScanPauseGate();
            Assert(gate.RequestPause() && !gate.RequestPause() && gate.IsPauseRequested,
                "control: solicitar pausa es idempotente");
            var notified = false;
            var waitPause = gate.WaitIfPausedAsync(() => notified = true, CancellationToken.None);
            Assert(notified && !waitPause.IsCompleted,
                "control: pausa real espera entre lotes");
            Assert(gate.Resume() && !gate.IsPauseRequested,
                "control: reanudar libera trabajos pendientes");
            await waitPause;
            Assert(!gate.Resume(), "control: reanudar sin pausa no hace nada");
            gate.RequestPause();
            using (var canceled = new CancellationTokenSource())
            {
                canceled.Cancel();
                var canceledWait = false;
                try { await gate.WaitIfPausedAsync(() => { }, canceled.Token); }
                catch (OperationCanceledException) { canceledWait = true; }
                Assert(canceledWait, "control: detener libera espera por cancelación");
            }
            gate.Resume();
            var noticeGate = new GuardNoticeGate();
            var noticeNow = new DateTimeOffset(2026, 11, 11, 11, 0, 0, TimeSpan.Zero);
            Assert(noticeGate.ShouldShow(GuardNoticeKind.Threat, @"C:\test\a.exe", noticeNow) &&
                   !noticeGate.ShouldShow(GuardNoticeKind.Threat, @"c:\TEST\A.EXE", noticeNow.AddSeconds(20)) &&
                   noticeGate.ShouldShow(GuardNoticeKind.Threat, @"C:\test\b.exe", noticeNow.AddSeconds(20)) &&
                   noticeGate.ShouldShow(GuardNoticeKind.Threat, @"C:\test\a.exe", noticeNow.AddMinutes(3)),
                "Guard: avisos de amenazas sin duplicados por archivo");
            Assert(noticeGate.ShouldShow(GuardNoticeKind.Warning, "evento", noticeNow) &&
                   !noticeGate.ShouldShow(GuardNoticeKind.Warning, "otro-evento", noticeNow.AddMinutes(1)) &&
                   noticeGate.ShouldShow(GuardNoticeKind.Warning, "evento", noticeNow.AddMinutes(6)),
                "Guard: advertencias agrupadas sin perder historial");
            Assert(noticeGate.ShouldShow(GuardNoticeKind.Error, "motor", noticeNow) &&
                   !noticeGate.ShouldShow(GuardNoticeKind.Error, "motor", noticeNow.AddSeconds(3)) &&
                   noticeGate.ShouldShow(GuardNoticeKind.Error, "motor", noticeNow.AddMinutes(6)),
                "Guard: errores separados de advertencias y limitados");
            var machineNow = new DateTimeOffset(2026, 11, 11, 11, 0, 0, TimeSpan.Zero);
            var first = new MachineServiceSnapshot("Vigilando", machineNow.AddSeconds(-20), "",
                20, 1, 0, 0, 0, 0, 0, "OK");
            var second = first with { Utc = machineNow.AddSeconds(-2), Scanned = 22,
                Detections = 2, Skipped = 1, LostEvents = 1, Errors = 1 };
            var changes = MachineGuardNoticePolicy.Compare(first, second, machineNow);
            Assert(changes.NewDetections && changes.NewCoverageGaps && changes.NewErrors &&
                   MachineGuardNoticePolicy.Compare(null, second, machineNow) == default,
                "servicio: avisos solo tras cambios nuevos y sin historial previo");
            Assert(MachineGuardNoticePolicy.Compare(first with { Utc = machineNow.AddMinutes(-8) },
                    second, machineNow) == default &&
                   MachineGuardNoticePolicy.Compare(second, first, machineNow) == default,
                "servicio: estado viejo o reinicio no genera avisos falsos");
            var fixedNow = new DateTimeOffset(2026, 11, 11, 11, 0, 0, TimeSpan.Zero);
            Assert(SignatureHealthService.Evaluate(false, null, DateTimeOffset.MinValue,
                DateTimeOffset.MinValue, "", fixedNow).State == SignatureHealthState.Missing,
                "firmas: sin bases no equivale a disponible");
            Assert(SignatureHealthService.Evaluate(true, fixedNow.AddDays(-10), DateTimeOffset.MinValue,
                DateTimeOffset.MinValue, "", fixedNow).State == SignatureHealthState.Attention,
                "firmas: base local antigua necesita revisión");
            Assert(SignatureHealthService.Evaluate(true, fixedNow.AddHours(-4), fixedNow.AddHours(-3),
                fixedNow.AddHours(-1), "Error de conexión", fixedNow).State == SignatureHealthState.UpdateFailed,
                "firmas: último fallo conservando firmas locales");
            Assert(SignatureHealthService.AutoCheckDue(fixedNow.AddHours(-14), fixedNow.AddHours(-2),
                12, fixedNow) == false && SignatureHealthService.AutoCheckDue(fixedNow.AddHours(-14),
                fixedNow.AddHours(-13), 12, fixedNow), "firmas: fallo reciente respeta intervalo automático");
            var detectedMail = new MailFinding("DETECCIÓN", "correo.eml", "a.exe", "Firma.Test", "Asunto");
            var suspiciousLink = new MailFinding("Aviso enlace", "correo.eml", "", "Dominio a revisar", "Asunto");
            var unreadMail = new MailFinding("Omitido", "correo.eml", "b.zip", "Límite", "Asunto");
            Assert(MailTriageService.IsDetection(detectedMail) && !MailTriageService.IsDetection(suspiciousLink),
                "correo: heurística de enlaces no es detección ClamAV");
            Assert(MailTriageService.Matches(unreadMail, "No analizados / errores") &&
                MailTriageService.Matches(suspiciousLink, "Avisos de enlaces") &&
                !MailTriageService.Matches(suspiciousLink, "Detecciones"),
                "correo: filtro separa detecciones, incidencias y enlaces");
            Assert(MailTriageService.Describe(new[] { detectedMail, suspiciousLink, unreadMail })
                .Contains("No analizados/incidencias: 1"), "correo: resumen registra elementos no verificados");
            var startExe = Path.Combine(Path.GetTempPath(), "PalClam Suite", "PalClam.exe");
            Assert(StartupLaunchPolicy.IsHiddenStartup(new[] { "--startup-hidden" }) &&
                   StartupLaunchPolicy.IsHiddenStartup(new[] { "--STARTUP-HIDDEN" }) &&
                   !StartupLaunchPolicy.IsHiddenStartup(new[] { "--scan", "archivo" }) &&
                   !StartupLaunchPolicy.IsHiddenStartup(Array.Empty<string>()),
                "inicio Windows: oculto solo con el argumento previsto");
            Assert(StartupLaunchPolicy.BuildRunCommand(startExe) ==
                   "\"" + startExe + "\" --startup-hidden" &&
                   !StartupLaunchPolicy.BuildRunCommand(startExe).Contains("--scan"),
                "inicio Windows: ruta con espacios siempre entrecomillada");
            var invalidStartupRejected = false;
            try { StartupLaunchPolicy.BuildRunCommand("PalClam.dll"); }
            catch (ArgumentException) { invalidStartupRejected = true; }
            Assert(invalidStartupRejected, "inicio Windows: no registrar DLL o rutas relativas");
            var excessivelyLongStartupRejected = false;
            try { StartupLaunchPolicy.BuildRunCommand(Path.Combine(Path.GetTempPath(),
                new string('x', 245), "PalClam.exe")); }
            catch (ArgumentException) { excessivelyLongStartupRejected = true; }
            Assert(excessivelyLongStartupRejected,
                "inicio Windows: rechazar comandos Run de mas de 260 caracteres");
            var machineInstall = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PalClam", "PalClam.exe");
            Assert(StartupLaunchPolicy.IsMachineInstallPath(machineInstall) &&
                   !StartupLaunchPolicy.IsMachineInstallPath(startExe) &&
                   !StartupLaunchPolicy.IsMachineInstallPath(Path.Combine(Path.GetTempPath(), "PalClam.exe")),
                "inicio Windows: todos los usuarios exige instalacion compartida en Program Files");
            Assert((int)StartupScope.Disabled == 0 && (int)StartupScope.CurrentUser == 1 &&
                   (int)StartupScope.AllUsers == 2,
                "inicio Windows: estados desactivado, usuario y todas las cuentas");
            Assert(DashboardLayoutPolicy.ForWidth(0, 1.0) == DashboardWidthMode.Compact &&
                   DashboardLayoutPolicy.ForWidth(double.NaN, 1.0) == DashboardWidthMode.Compact &&
                   DashboardLayoutPolicy.ForWidth(639, 1.0) == DashboardWidthMode.Compact &&
                   DashboardLayoutPolicy.ForWidth(640, 1.0) == DashboardWidthMode.Medium &&
                   DashboardLayoutPolicy.ForWidth(740, 1.0) == DashboardWidthMode.Wide &&
                   DashboardLayoutPolicy.ForWidth(740, 1.3) == DashboardWidthMode.Compact &&
                   DashboardLayoutPolicy.ForWidth(950, 1.3) == DashboardWidthMode.Medium,
                "panel principal: tres anchos y tamaño de texto adaptativo sin recrear controles");
            Assert(AppearanceCatalog.Themes.Count >= 7 &&
                   AppearanceCatalog.Themes.Select(x => x.Id).Distinct().Count() == AppearanceCatalog.Themes.Count,
                "temas: siete paletas distintas, sin identificadores repetidos");
            Assert(AppearanceCatalog.Find("otro-tema").Id == AppearanceCatalog.DefaultId &&
                   AppearanceCatalog.Find(null).Id == AppearanceCatalog.DefaultId,
                "temas: valor inválido recupera morado clásico");
            Assert(AppearanceCatalog.Themes.All(x => AppearanceCatalog.Contrast(x.Ink, x.Surface) >= 7.0 &&
                AppearanceCatalog.Contrast(x.Muted, x.Surface) >= 4.5 &&
                AppearanceCatalog.Contrast(x.Accent, x.Surface) >= 4.5 &&
                AppearanceCatalog.Contrast(x.AccentText, x.AccentPale) >= 4.5),
                "temas: textos y navegación cumplen contraste mínimo");
            Assert(AppearanceCatalog.Themes.Where(x => x.Id != "oscuro").All(x =>
                AppearanceCatalog.MapColor(x.Id, "#C82B42") == "#C82B42" &&
                AppearanceCatalog.MapColor(x.Id, "#9F650A") == "#9F650A" &&
                AppearanceCatalog.MapColor(x.Id, "#16815C") == "#16815C") &&
                AppearanceCatalog.MapColor("oscuro", "#C82B42") !=
                AppearanceCatalog.MapColor("oscuro", "#16815C") &&
                AppearanceCatalog.Contrast(AppearanceCatalog.MapColor("oscuro", "#C82B42"),
                    AppearanceCatalog.MapColor("oscuro", "#FDE9ED")) >= 4.5 &&
                AppearanceCatalog.Contrast(AppearanceCatalog.MapColor("oscuro", "#16815C"),
                    AppearanceCatalog.MapColor("oscuro", "#E5F6EE")) >= 4.5 &&
                AppearanceCatalog.Contrast(AppearanceCatalog.MapColor("oscuro", "#9F650A"),
                    AppearanceCatalog.MapColor("oscuro", "#FFF3D9")) >= 4.5,
                "temas: colores semánticos conservan significado y contraste en oscuro");
            Assert(AppearanceCatalog.Themes.All(x =>
                AppearanceCatalog.MapColor(x.Id, "#6940B3").Equals(x.Accent, StringComparison.OrdinalIgnoreCase) &&
                AppearanceCatalog.MapColor(x.Id, "#F4F6FC").Equals(x.Background, StringComparison.OrdinalIgnoreCase) &&
                AppearanceCatalog.MapColor(x.Id, "#FBFCFF").Equals(x.Sidebar, StringComparison.OrdinalIgnoreCase)),
                "temas: cada sección usa acento, fondo y lateral del tema elegido");
            Assert(AppearanceCatalog.Themes.Any(x => x.Id == "oscuro") &&
                   AppearanceCatalog.Find("oscuro").Name == "Oscuro violeta" &&
                   AppearanceCatalog.MapColor("oscuro", "#FFFFFF") ==
                        AppearanceCatalog.Find("oscuro").Surface &&
                   AppearanceCatalog.MapColor("oscuro", "#172034") ==
                        AppearanceCatalog.Find("oscuro").Ink,
                "tema oscuro: se guarda con id estable y transforma fondos y texto");
            Assert(AppearanceCatalog.NormalizeScaleValue(3.0) == 1.0 &&
                   AppearanceCatalog.NormalizeScaleValue(double.NaN) == 1.0 &&
                   AppearanceCatalog.NormalizeScaleValue(1.20) == 1.20 &&
                   AppearanceCatalog.SupportedScales.Length == 5,
                "accesibilidad: tamaños válidos y recuperación de opciones corruptas");
            Assert(HelpContentCatalog.Advantages.Count >= 5 && HelpContentCatalog.Capabilities.Count >= 10,
                "ayuda: ventajas y características principales documentadas");
            var validHelpPages = new HashSet<string>(StringComparer.Ordinal)
            { "dashboard", "scan", "inspector", "signatures", "updates", "history", "options", "diagnostics",
              "quarantine", "schedule", "guard", "mail", "outlook", "about", "ai", "advice", "selfdefense", "health", "devices", "alerts", "help" };
            Assert(HelpContentCatalog.Advantages.Concat(HelpContentCatalog.Capabilities)
                    .All(x => validHelpPages.Contains(x.Page)),
                "ayuda: todos los accesos apuntan a secciones existentes");
            Assert(HelpContentCatalog.Advantages.Any(x => x.Detail.Contains("gratuito", StringComparison.OrdinalIgnoreCase)) &&
                   HelpContentCatalog.Advantages.Any(x => x.Detail.Contains("Defender")) &&
                   HelpContentCatalog.Limitations.Any(x => x.Contains("no sustituye Microsoft Defender")) &&
                   HelpContentCatalog.Limitations.Any(x => x.Contains("pausa")) &&
                   HelpContentCatalog.Capabilities.Any(x => x.Detail.Contains("confirmación")),
                "ayuda: gratuidad, compatibilidad, consentimiento y límites sin promesas falsas");
            Assert(SecurityAdviceCatalog.Items.Count >= 8, "consejos: catálogo educativo disponible");
            var validAdvicePages = new HashSet<string>(StringComparer.Ordinal)
            { "dashboard", "scan", "inspector", "signatures", "updates", "history", "options", "diagnostics",
              "quarantine", "schedule", "guard", "mail", "outlook", "about", "ai", "advice", "selfdefense", "health", "devices", "alerts" };
            Assert(SecurityAdviceCatalog.Items.All(x => validAdvicePages.Contains(x.Page)),
                "consejos: enlaces limitados a páginas internas existentes");
            Assert(SecurityAdviceCatalog.Items.Any(x => x.Text.Contains("Defender")) &&
                   SecurityAdviceCatalog.Items.Any(x => x.Text.Contains("confirmación")) &&
                   SecurityAdviceCatalog.Items.Any(x => x.Text.Contains("remota")),
                "consejos: límites de Guard, confirmación y privacidad IA");
            // Guard 2.0: pure tests do not need Windows, ClamAV or real file watchers.
            var efficient = GuardTuning.ForMode("Eficiente");
            var balanced = GuardTuning.ForMode("Equilibrado");
            var fast = GuardTuning.ForMode("Rápido");
            Assert(efficient.QueueCapacity < balanced.QueueCapacity &&
                   balanced.QueueCapacity < fast.QueueCapacity &&
                   efficient.MinimumScanGapMs > balanced.MinimumScanGapMs &&
                   fast.MinimumScanGapMs == 0,
                "Guard 2.0: perfiles limitan cola y frecuencia de análisis");
            Assert(efficient.QuietPeriodMs > balanced.QuietPeriodMs &&
                   balanced.QuietPeriodMs > fast.QuietPeriodMs,
                "Guard 2.0: espera tras ráfagas según perfil");
            Assert(GuardTuning.NormalizeMode("desconocido") == "Equilibrado" &&
                   GuardTuning.ForMode(null).QueueCapacity == balanced.QueueCapacity,
                "Guard 2.0: configuración inválida usa perfil seguro");
            long eventTicks = TimeSpan.FromSeconds(10).Ticks;
            Assert(balanced.QuietRemainingMs(eventTicks, eventTicks) == 450 &&
                   balanced.QuietRemainingMs(eventTicks + TimeSpan.FromMilliseconds(200).Ticks, eventTicks) == 250 &&
                   balanced.QuietRemainingMs(eventTicks + TimeSpan.FromMilliseconds(500).Ticks, eventTicks) == 0,
                "Guard 2.0: coalescencia espera solo el periodo restante");
            Assert(balanced.QuietRemainingMs(eventTicks - TimeSpan.FromSeconds(2).Ticks, eventTicks) == 450 &&
                   balanced.QuietRemainingMs(eventTicks, 0) == 450,
                "Guard 2.0: ajuste del reloj y fecha inválida no generan esperas negativas");

            // Preview 13: inspector local; SHA-256 real, errores honestos, cancelación y avisos heurísticos.
            Directory.CreateDirectory(tmp);
            // Preview 18: registros aislados, nunca interferir con los tests de estimación.
            var noticeRoot = Path.Combine(tmp, "centro-notificaciones");
            var personalLog = Path.Combine(noticeRoot, "personal");
            var machineLog = Path.Combine(noticeRoot, "equipo");
            Directory.CreateDirectory(personalLog);
            Directory.CreateDirectory(machineLog);
            var noticeTime = new DateTimeOffset(2026, 11, 11, 13, 0, 0, TimeSpan.Zero);
            var personalRecord = System.Text.Json.JsonSerializer.Serialize(new
            {
                When = noticeTime.AddMinutes(-3), Outcome = 1,
                FilePath = @"C:\\pruebas\\amenaza.txt", Detail = "EICAR de prueba"
            });
            await File.WriteAllTextAsync(Path.Combine(personalLog, "incidencias.jsonl"),
                personalRecord + Environment.NewLine + "línea no válida" + Environment.NewLine);
            await File.WriteAllTextAsync(Path.Combine(machineLog, "eventos.log"),
                noticeTime.AddMinutes(-2).ToString("O") + " [WARNING] eventos perdidos" + Environment.NewLine +
                noticeTime.AddMinutes(-1).ToString("O") + " [ALERTA] ClamAV prueba" + Environment.NewLine);
            var noticeFeed = SecurityNoticeFeed.ReadRecent(personalLog, machineLog, noticeTime);
            Assert(noticeFeed.Items.Count == 3 && noticeFeed.InvalidEntries == 1 &&
                   noticeFeed.Items.Count(n => n.Level == SecurityNoticeLevel.Threat) == 2 &&
                   noticeFeed.Items[0].Source == "machine",
                "alertas: combina registros personales y del servicio, ordenados");
            Assert(SecurityNoticeFeed.Filter(noticeFeed.Items, "DETECCIÓN", "Guard personal", "EICAR",
                       false, DateTimeOffset.MinValue).Count == 1 &&
                   SecurityNoticeFeed.Filter(noticeFeed.Items, "Advertencia", "Servicio Windows", "perdidos",
                       false, DateTimeOffset.MinValue).Count == 1,
                "alertas: filtros por gravedad, origen y texto");
            Assert(SecurityNoticeFeed.Filter(noticeFeed.Items, "Todas", "Todos", "", true,
                       noticeTime.AddMinutes(-2).AddSeconds(-30)).Count == 2,
                "alertas: marca temporal de revisión no elimina eventos");
            var readMarker = Path.Combine(noticeRoot, "marca", "revisado-hasta.txt");
            Assert(SecurityNoticeReadMarker.Read(readMarker) == DateTimeOffset.MinValue &&
                   SecurityNoticeReadMarker.TryMark(readMarker, noticeTime) &&
                   SecurityNoticeReadMarker.Read(readMarker) == noticeTime &&
                   File.Exists(Path.Combine(machineLog, "eventos.log")),
                "alertas: revisar conserva auditorías y persiste marca local");
            Assert(SecurityNoticeFeed.ParseMachine("formato inválido") is null &&
                   SecurityNoticeFeed.ParseGuard("{invalido") is null &&
                   SecurityNoticeFeed.ParseGuard("{\"When\":\"2026-11-11T13:00:00+00:00\",\"Outcome\":-20}") is null,
                "alertas: registros corruptos o niveles desconocidos no se muestran");
            var inspectorTestFile = Path.Combine(tmp, "ejemplo.txt");
            await File.WriteAllTextAsync(inspectorTestFile, "abc", System.Text.Encoding.ASCII);
            var inspection = await FileInspectorService.InspectAsync(inspectorTestFile, CancellationToken.None);
            Assert(inspection.Sha256 ==
                   "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD" &&
                   inspection.SizeBytes == 3 && inspection.Extension == ".txt" &&
                   inspection.Certificate.Status.Length > 0,
                "inspector: SHA-256 en streaming y metadatos reales, sin cargar ClamAV");
            Assert(FileInspectorService.FormatSize(0) == "0 B" &&
                   FileInspectorService.FormatSize(1024).Contains("KiB") &&
                   FileInspectorService.CheckDoubleExtension("factura.pdf.exe").Contains("doble extensión") &&
                   FileInspectorService.CheckDoubleExtension("normal.exe").Length == 0,
                "inspector: tamaño legible y aviso de doble extensión sin atribuir malware");
            using (var cancelledInspector = new CancellationTokenSource())
            {
                cancelledInspector.Cancel();
                var cancelled = false;
                try { await FileInspectorService.InspectAsync(inspectorTestFile, cancelledInspector.Token); }
                catch (OperationCanceledException) { cancelled = true; }
                Assert(cancelled, "inspector: permite cancelar el cálculo del hash");
            }
            var rejectedDirectory = false;
            try { await FileInspectorService.InspectAsync(tmp, CancellationToken.None); }
            catch (ArgumentException) { rejectedDirectory = true; }
            Assert(rejectedDirectory, "inspector: rechaza carpetas, sin generar resultados erróneos");

            // Preview 12: verificaciones de progreso, pausa y cobertura sin falsas certezas.
            var early = ScanProgressPolicy.Evaluate(ScanProgressPhase.Running, 10,
                100, false, 0, false, 4, 1.0);
            Assert(early.Percent == 10 && early.Eta.Length == 0,
                "progreso: porcentaje orientativo, sin ETA prematura");
            var stable = ScanProgressPolicy.Evaluate(ScanProgressPhase.Running, 40,
                100, false, 0, false, 20, 2.0);
            Assert(stable.Percent == 40 && stable.Eta.Contains("00:00:30"),
                "progreso: ETA condicionada a muestras y estimación fiable");
            var pausedProgress = ScanProgressPolicy.Evaluate(ScanProgressPhase.Paused, 40,
                100, false, 0, false, 20, 2.0);
            Assert(pausedProgress.Label == "Pausado" && pausedProgress.Eta == "" &&
                   !pausedProgress.IsIndeterminate,
                "progreso: una pausa efectiva no promete tiempo ni movimiento");
            var pausing = ScanProgressPolicy.Evaluate(ScanProgressPhase.Pausing, 40,
                100, false, 0, false, 20, 2.0);
            Assert(pausing.Label == "Pausando…" && pausing.Eta == "",
                "progreso: diferenciar pausa solicitada de pausa efectiva");
            var stopping = ScanProgressPolicy.Evaluate(ScanProgressPhase.Stopping, 40,
                100, false, 0, false, 20, 2.0);
            Assert(stopping.Label == "Deteniendo…" && stopping.Eta == "",
                "progreso: la cancelación no declara completado");
            Assert(ScanProgressPolicy.Evaluate(ScanProgressPhase.Running, 110,
                100, false, 0, false, 25, 10).Percent is null,
                "progreso: al superar la referencia se oculta el porcentaje inconsistente");
            Assert(ScanProgressPolicy.Evaluate(ScanProgressPhase.Running, 40,
                100, true, 0, false, 25, 10).Percent is null &&
                   ScanProgressPolicy.Evaluate(ScanProgressPhase.Running, 40,
                100, false, 2, false, 25, 10).Percent is null,
                "progreso: rutas inaccesibles y estimación truncada sin porcentajes");
            Assert(ScanProgressPolicy.Evaluate(ScanProgressPhase.Running, 100,
                100, false, 0, false, 25, 10).Percent == 99,
                "progreso: 100% solo al finalizar realmente");
            Assert(ScanProgressPolicy.Evaluate(ScanProgressPhase.Completed, 80,
                -1, false, 0, false, 25, null).Percent == 100 &&
                   ScanProgressPolicy.Evaluate(ScanProgressPhase.Incomplete, 80,
                -1, false, 0, false, 25, null).Percent is null,
                "progreso: una finalización correcta no equivale a interrupción");
            var smoother = new ScanResultRateTracker();
            Assert(smoother.Observe(0, 0, true) is null &&
                   smoother.Observe(50, 12, true) is double,
                "ETA: el suavizado necesita actividad sostenida");
            Assert(smoother.Observe(50, 15, false) is null &&
                   smoother.Observe(70, 17, true) is null,
                "ETA: descartar tasa antigua tras una pausa");
            smoother.Reset();
            Assert(smoother.Observe(15, 1, true) is null,
                "ETA: reiniciar tasa al comenzar otro análisis");

            Assert(ScanOutputParser.ParseFile(@"C:\Temp\test.txt: OK")?.Status == ScanFileStatus.Clean,
                "resultado limpio en ruta Windows");
            var threat = ScanOutputParser.ParseFile(@"C:\Temp\eicar.txt: Win.Test.EICAR_HDB-1 FOUND");
            Assert(threat?.Status == ScanFileStatus.Detected && threat.Detail.Contains("EICAR"),
                "detección y nombre de amenaza");
            Assert(ScanOutputParser.ParseFile(@"C:\Temp\blocked.dat: Access denied ERROR")?.Status == ScanFileStatus.Error,
                "errores no tratados como limpios");
            Assert(ScanOutputParser.ParseFile("----------- SCAN SUMMARY -----------") is null,
                "cabeceras ignoradas");
            Assert(ScanOutputParser.TryReadCount("Scanned files: 42", "Scanned files", out var scanned) && scanned == 42,
                "contador oficial ClamAV");
            Assert(!ScanOutputParser.TryReadCount("Scanned files: -4", "Scanned files", out _),
                "contador negativo rechazado");

            // La URL de descarga procede del manifiesto firmado y debe coincidir
            // con el nombre de versión, incluso al invocar el descargador por separado.
            var release = new ReleaseManifest
            {
                Channel = "stable", Version = "1.0.1", SizeBytes = 12_000_000,
                Sha256 = new string('a', 64),
                DownloadUrl = "https://www.palentino.es/palclam/updates/PalClam_1.0.1_Setup_win-x64.exe"
            };
            Assert(AppUpdateService.TryValidateReleaseMetadata(release, out _),
                "manifiesto válido: URL oficial, versión y hash");
            var offsite = new ReleaseManifest
            {
                Channel = release.Channel, Version = release.Version, SizeBytes = release.SizeBytes,
                Sha256 = release.Sha256, DownloadUrl = "https://example.net/palclam.exe"
            };
            Assert(!AppUpdateService.TryValidateReleaseMetadata(offsite, out _),
                "rechazar instalador de otro dominio");
            var withQuery = new ReleaseManifest
            {
                Channel = release.Channel, Version = release.Version, SizeBytes = release.SizeBytes,
                Sha256 = release.Sha256, DownloadUrl = release.DownloadUrl + "?source=external"
            };
            Assert(!AppUpdateService.TryValidateReleaseMetadata(withQuery, out _),
                "rechazar parámetros añadidos a URL de descarga");
            var wrongVersion = new ReleaseManifest
            {
                Channel = release.Channel, Version = "1.0.2", SizeBytes = release.SizeBytes,
                Sha256 = release.Sha256, DownloadUrl = release.DownloadUrl
            };
            Assert(!AppUpdateService.TryValidateReleaseMetadata(wrongVersion, out _),
                "rechazar URL que no corresponde a versión firmada");
            var weakHash = new ReleaseManifest
            {
                Channel = release.Channel, Version = release.Version, SizeBytes = release.SizeBytes,
                Sha256 = "1234", DownloadUrl = release.DownloadUrl
            };
            Assert(!AppUpdateService.TryValidateReleaseMetadata(weakHash, out _),
                "rechazar SHA-256 incompleto");
            var preview = new ReleaseManifest
            {
                Channel = "preview", Version = release.Version, SizeBytes = release.SizeBytes,
                Sha256 = release.Sha256, DownloadUrl = release.DownloadUrl
            };
            Assert(!AppUpdateService.TryValidateReleaseMetadata(preview, out _),
                "no aceptar canal preview desde actualizador estable");
            Assert(ScanOutputParser.ParseFile(@"C:\Temp\file.txt:Zone.Identifier: Win.Test.Example FOUND")?.Status
                == ScanFileStatus.Detected, "rutas Windows con dos puntos en nombre notificadas correctamente");
            Assert(ScanOutputParser.ParseFile("Scanned files: 46") is null,
                "la línea del resumen no es un evento de archivo");

            // Pruebas de criterio comunes a análisis manual y programado.
            Assert(ScanVerdictPolicy.Evaluate(0, 0, 0, -1).Status == ScanVerdictStatus.Clean,
                "salida 0 y sin errores: sin detecciones");
            var exitOneWithoutFile = ScanVerdictPolicy.Evaluate(1, 0, 0, -1);
            Assert(exitOneWithoutFile.Status == ScanVerdictStatus.Detected && exitOneWithoutFile.Detections == 1,
                "salida 1 sin detalle: no perder detección");
            Assert(ScanVerdictPolicy.Evaluate(1, 0, 0, 3).Detections == 3,
                "resumen ClamAV conserva cantidad de detecciones");
            Assert(ScanVerdictPolicy.Evaluate(0, 0, 1, 0).Status == ScanVerdictStatus.Inconclusive,
                "errores de lectura impiden resultado limpio");
            Assert(ScanVerdictPolicy.Evaluate(2, 0, 0, 0).Status == ScanVerdictStatus.Inconclusive,
                "salida 2 es error y no limpio");
            var partialThreat = ScanVerdictPolicy.Evaluate(2, 2, 1, 0);
            Assert(partialThreat.Status == ScanVerdictStatus.DetectedWithIssues && !partialThreat.IsComplete,
                "amenazas con errores: incompleto y detecciones conservadas");
            Assert(ScanVerdictPolicy.Evaluate(null, 0, 0, -1).Status == ScanVerdictStatus.Inconclusive,
                "sin código del motor: análisis inconcluso");
            Assert(ScanVerdictPolicy.Evaluate(0, 3, 0, 1).Detections == 3,
                "se respeta el mayor contador de amenazas");

            Assert(ScanPerformanceMath.Rate(120, 4) == 30,
                "rendimiento: archivos por segundo");
            Assert(ScanPerformanceMath.Rate(-1, 0) == 0,
                "rendimiento: divisiones o diferencias inválidas");
            Assert(Math.Abs(ScanPerformanceMath.CpuShareOfMachine(TimeSpan.FromSeconds(2), 1, 4) - 50) < 0.001,
                "rendimiento: CPU del proceso respecto al equipo");
            Assert(ScanPerformanceMath.CpuShareOfMachine(TimeSpan.FromSeconds(-2), 1, 4) == 0,
                "rendimiento: rechazar tiempos de CPU negativos");
            Assert(ScanPerformanceMath.QuietStatus(25, true).Contains("archivo grande"),
                "estabilidad: pausa larga sin resultados no implica cuelgue");
            Assert(ScanPerformanceMath.QuietStatus(95, true).Contains("revisar"),
                "estabilidad: actividad sin respuestas prolongada");
            Assert(ScanPerformanceMath.TryGetLogicalFileBytes(@"\\servidor\documento.txt") == 0,
                "rendimiento: no consultar archivos de red para métricas");
            var series = new ScanPerformanceSeries();
            for (var i = 0; i < 140; i++) series.Add(new PerformancePoint(i, i, 40, 80));
            Assert(series.Samples.Count == 120 && series.Samples[0].Seconds == 20,
                "gráficas: ventana limitada a 120 muestras");
            Assert(ScanPerformanceSeries.Normalize(new double[]{ 0, 5, 10 })[1] == 0.5,
                "gráficas: escala relativa reproducible");
            Assert(ScanPerformanceSeries.Normalize(new double[]{ double.NaN, -2, 0 }).All(x => x == 0),
                "gráficas: valores no válidos no se representan");
            var profileId = ScanComparisonService.ConfigurationId(true, false, "Equilibrado", new[] { @"C:\tmp\ex1" });
            var before = new ScanRecord { Target = @"C:\Test", Seconds = 100, EngineScannedFiles = 1000,
                Outcome = "Sin detecciones", ExitCode = 0, ScanConfigurationId = profileId,
                SignatureSnapshot = "signatures1", EngineVersion = "1.5.4", EngineMode = "clamscan" };
            var after = new ScanRecord { Target = @"C:\Test", Seconds = 80, EngineScannedFiles = 1000,
                Outcome = "Sin detecciones", ExitCode = 0, ScanConfigurationId = profileId,
                SignatureSnapshot = "signatures1", EngineVersion = "1.5.4", EngineMode = "clamscan" };
            var comparison = ScanComparisonService.Compare(before, after);
            Assert(comparison.Status == Comparability.Comparable &&
                Math.Abs(comparison.SecondsDeltaPercent!.Value - (-20)) < 0.001,
                "comparación: misma configuración, tiempo un 20% inferior");
            after.SignatureSnapshot = "signatures2";
            Assert(ScanComparisonService.Compare(before, after).Status == Comparability.DifferentSignatures,
                "comparación: firma diferente impide resultado comparable");
            after.SignatureSnapshot = "signatures1";
            after.EngineScannedFiles = 900;
            Assert(ScanComparisonService.Compare(before, after).Status == Comparability.InsufficientData,
                "comparación: distinta cantidad de archivos no es comparable");
            after.EngineScannedFiles = 1000;
            after.Outcome = "Cancelado";
            Assert(ScanComparisonService.Compare(before, after).Status == Comparability.Incomplete,
                "comparación: cancelación se excluye");
            after.Outcome = "Detecciones";
            after.Detections = 1;
            Assert(ScanComparisonService.Compare(before, after).Status == Comparability.DifferentResults,
                "comparación: no equiparar ejecución limpia con detección");
            after.Outcome = "Sin detecciones";
            Assert(ScanComparisonService.Compare(before, after).Status == Comparability.DifferentResults,
                "comparación: no equiparar contadores de detecciones diferentes");
            after.Detections = 0;
            Assert(ScanComparisonService.Compare(before, after).Status == Comparability.Comparable,
                "comparación: se recupera al igualar resultado y configuración");
            Assert(ScanComparisonService.ConfigurationId(true, false, "Equilibrado", Array.Empty<string>()) !=
                ScanComparisonService.ConfigurationId(true, true, "Equilibrado", Array.Empty<string>()),
                "comparación: PUA cambia identificador de opciones");
            // FIX1: usar una carpeta de pruebas exclusiva para la estimación.
            // El Inspector crea antes 'ejemplo.txt' en tmp. Contar tmp introducía
            // un tercer archivo legítimo y provocaba un falso fallo de SmokeTests.
            var estimationRoot = Path.Combine(tmp, "estimacion-exclusiones");
            Directory.CreateDirectory(estimationRoot);
            var excluded = Path.Combine(estimationRoot, "excluir");
            var neighbor = Path.Combine(estimationRoot, "excluir-otro");
            Directory.CreateDirectory(excluded);
            Directory.CreateDirectory(neighbor);
            var includedFile = Path.Combine(estimationRoot, "ok.txt");
            var excludedFile = Path.Combine(excluded, "oculto.txt");
            var neighborFile = Path.Combine(neighbor, "visible.txt");
            await File.WriteAllTextAsync(includedFile, "contenido inofensivo");
            await File.WriteAllTextAsync(excludedFile, "contenido inofensivo");
            await File.WriteAllTextAsync(neighborFile, "contenido inofensivo");
            Assert(ScanExclusions.TryNormalize(new[] { excluded }, out var normalized, out _) &&
                   normalized.Count == 1, "normalizar exclusiones");
            Assert(ScanExclusions.IsExcluded(excludedFile, normalized),
                "excluir descendientes");
            Assert(!ScanExclusions.IsExcluded(neighborFile, normalized),
                "no excluir rutas que comparten prefijo");
            var estimate = ScanFileEstimator.Estimate(estimationRoot, CancellationToken.None, normalized);
            Assert(estimate.Count == 2 && !estimate.Truncated && estimate.InaccessibleDirectories == 0,
                $"estimación respeta exclusiones (esperados: 2; obtenidos: {estimate.Count}; " +
                $"inaccesibles: {estimate.InaccessibleDirectories}; truncado: {estimate.Truncated})");
            Assert(ScanFileEstimator.Estimate(estimationRoot, CancellationToken.None).Count == 3,
                "estimación: sin exclusiones cuenta los tres archivos de la prueba aislada");
            Assert(ScanFileEstimator.Estimate(excludedFile, CancellationToken.None, normalized).Count == 0,
                "estimación: un archivo expresamente excluido no se cuenta");
            Assert(ScanFileEstimator.Estimate(neighborFile, CancellationToken.None, normalized).Count == 1,
                "estimación: carpeta vecina de prefijo parecido no se excluye");
            Assert(File.Exists(inspectorTestFile) &&
                   ScanPerformanceMath.TryGetLogicalFileBytes(includedFile) > 0,
                "rendimiento: tamaño lógico de archivo local completado");
            Assert(!ScanExclusions.TryNormalize(new[] { Path.GetPathRoot(tmp)! }, out _, out _),
                "rechazar exclusión de una raíz");
            var hash = await QuarantineStore.HashFileAsync(includedFile);
            Assert(hash.Length == 64 && hash.All(Uri.IsHexDigit), "huella SHA-256 de archivo");
            // Remediación: la prueba crea su PROPIO archivo inofensivo y elimina solamente
            // la copia que ella misma aísla. No toca ficheros existentes del usuario.
            var synthetic = Path.Combine(tmp, "palclam-remediation-test.txt");
            await File.WriteAllTextAsync(synthetic, "PalClam synthetic harmless sample");
            var isolatedTest = await QuarantineStore.IsolateAsync(synthetic, "PRUEBA_SIN_VIRUS");
            Assert(!File.Exists(synthetic) && QuarantineStore.List().Any(e => e.Id == isolatedTest.Id),
                "remediación: cuarentena de muestra inofensiva");
            Assert(QuarantineReviewPolicy.Classify(0, false, false) == QuarantineRecheckVerdict.NoDetections &&
                   QuarantineReviewPolicy.Classify(1, true, false) == QuarantineRecheckVerdict.Detected &&
                   QuarantineReviewPolicy.Classify(1, false, false) == QuarantineRecheckVerdict.Inconclusive &&
                   QuarantineReviewPolicy.Classify(0, false, true) == QuarantineRecheckVerdict.Inconclusive &&
                   QuarantineReviewPolicy.Classify(2, true, false) == QuarantineRecheckVerdict.Inconclusive,
                "Remediation PRO: códigos ambiguos y errores nunca se presentan como limpios");
            var physicalIsolated = await QuarantineStore.VerifyIsolatedAsync(isolatedTest);
            Assert(File.Exists(physicalIsolated) &&
                   Path.GetExtension(physicalIsolated).Equals(".pclam", StringComparison.OrdinalIgnoreCase) &&
                   !File.Exists(synthetic),
                "Remediation PRO: reanálisis solo del objeto aislado con nombre opaco");
            await QuarantineStore.FlagManualReviewAsync(isolatedTest);
            Assert(QuarantineStore.List().Any(e => e.Id == isolatedTest.Id && e.RequestedManualReview &&
                   e.RestoredAt is null && e.DeletedAt is null),
                "Remediation PRO: marcar falso positivo no restaura ni altera el veredicto");
            var simulatedResult = new QuarantineRecheckResult(QuarantineRecheckVerdict.NoDetections,
                "Resultado simulado para prueba de persistencia (NO es ejecución de ClamAV)",
                DateTimeOffset.Now, 0);
            await QuarantineStore.SaveRecheckAsync(isolatedTest, simulatedResult);
            Assert(QuarantineStore.List().Any(e => e.Id == isolatedTest.Id &&
                   e.LastRecheckAt is not null && e.LastRecheckVerdict.Contains("no garantiza seguridad")),
                "Remediation PRO: registro de revisión no declara archivo seguro");
            Assert(RemediationAuditLog.ReadRecent().Any(a => a.EntryId == isolatedTest.Id &&
                   a.Action == "Revisión manual"),
                "Remediation PRO: historial auditable de acciones de revisión");
            // Prueba de corrupción: el hash debe bloquear el reanálisis sin tocar la ruta original.
            var corruptOriginal = Path.Combine(tmp, "quarantine-corrupt-test.txt");
            await File.WriteAllTextAsync(corruptOriginal, "harmless test before tampering");
            var corruptedEntry = await QuarantineStore.IsolateAsync(corruptOriginal, "PRUEBA_SIN_VIRUS");
            var corruptPhysical = await QuarantineStore.VerifyIsolatedAsync(corruptedEntry);
            await File.AppendAllTextAsync(corruptPhysical, " changed after isolation");
            var corruptionBlocked = false;
            try { await QuarantineStore.VerifyIsolatedAsync(corruptedEntry); }
            catch (IOException) { corruptionBlocked = true; }
            Assert(corruptionBlocked && !File.Exists(corruptOriginal),
                "Remediation PRO: cambio físico o SHA-256 bloquea reanálisis");
            await QuarantineStore.DeleteIsolatedAsync(isolatedTest);
            Assert(QuarantineStore.List().Any(e => e.Id == isolatedTest.Id && e.DeletedAt is not null && e.State == "Eliminado"),
                "remediación: eliminación aislada con registro histórico");
            bool noSecondDelete = false;
            try { await QuarantineStore.DeleteIsolatedAsync(isolatedTest); }
            catch (IOException) { noSecondDelete = true; }
            Assert(noSecondDelete, "remediación: impide segunda eliminación");
            bool noRestoreDeleted = false;
            try { await QuarantineStore.RestoreAsync(isolatedTest); }
            catch (IOException) { noRestoreDeleted = true; }
            Assert(noRestoreDeleted, "remediación: impide restaurar después de eliminar");
            Assert(!QuarantineReviewPolicy.CanRecheck(QuarantineStore.List().Single(e => e.Id == isolatedTest.Id)),
                "Remediation PRO: elemento eliminado no puede reanalizarse");
            // PalClam AI: probar adaptadores sin acceso a Internet ni claves reales.
            Assert(AiProviderCatalog.All.Count >= 13 &&
                   AiProviderCatalog.All.Any(p => p.Id == "xai" && p.Protocol == "responses") &&
                   AiProviderCatalog.All.Any(p => p.Id == "anthropic" && p.Protocol == "anthropic"),
                "IA: catalogo ampliado, Grok y Claude con protocolos propios");
            var ollamaProvider = AiProviderCatalog.Find("ollama");
            Assert(AiGateway.ValidateBaseUrl(ollamaProvider, ollamaProvider.BaseUrl).IsLoopback,
                "IA: Ollama restringido a loopback");
            bool invalidRemote = false;
            try { AiGateway.ValidateBaseUrl(AiProviderCatalog.Find("compatible"), "http://api.example.com/v1"); }
            catch (ArgumentException) { invalidRemote = true; }
            Assert(invalidRemote, "IA: rechazar HTTP sin cifrar en proveedores remotos");
            bool fakeOfficial = false;
            try { AiGateway.ValidateBaseUrl(AiProviderCatalog.Find("xai"), "https://ejemplo.invalid/v1"); }
            catch (ArgumentException) { fakeOfficial = true; }
            Assert(fakeOfficial, "IA: impedir redirigir claves de Grok a servidores no oficiales");
            bool externalLocal = false;
            try { AiGateway.ValidateBaseUrl(ollamaProvider, "http://192.168.1.33:11434/api"); }
            catch (ArgumentException) { externalLocal = true; }
            Assert(externalLocal, "IA: sin Ollama remoto por error");
            using var chatExample = System.Text.Json.JsonDocument.Parse("{\"choices\":[{\"message\":{\"content\":\"Respuesta segura\"}}]}");
            Assert(AiGateway.ParseResponse("chat", chatExample.RootElement) == "Respuesta segura",
                "IA: lector de respuesta Chat Completions");
            using var anthropicExample = System.Text.Json.JsonDocument.Parse("{\"content\":[{\"type\":\"text\",\"text\":\"Claude respondió\"}]}");
            Assert(AiGateway.ParseResponse("anthropic", anthropicExample.RootElement) == "Claude respondió",
                "IA: lector de respuesta nativa de Claude");
            using var grokExample = System.Text.Json.JsonDocument.Parse("{\"output\":[{\"type\":\"message\",\"content\":[{\"type\":\"output_text\",\"text\":\"Grok respondió\"}]}]}");
            Assert(AiGateway.ParseResponse("responses", grokExample.RootElement) == "Grok respondió",
                "IA: lector de respuesta Responses de Grok");
            using var ollamaExample = System.Text.Json.JsonDocument.Parse("{\"message\":{\"content\":\"Modelo local\"}}");
            Assert(AiGateway.ParseResponse("ollama", ollamaExample.RootElement) == "Modelo local",
                "IA: lector de respuesta de Ollama");
            bool emptyModel = false;
            try { AiGateway.ValidateRequest(new AiRequest(ollamaProvider, ollamaProvider.BaseUrl, "", "Hola", "")); }
            catch (ArgumentException) { emptyModel = true; }
            Assert(emptyModel, "IA: modelo obligatorio antes de enviar");
            bool largePrompt = false;
            try { AiGateway.ValidateRequest(new AiRequest(ollamaProvider, ollamaProvider.BaseUrl, "modelo", new string('A', 5001), "")); }
            catch (ArgumentException) { largePrompt = true; }
            Assert(largePrompt, "IA: limite de caracteres del contenido enviado");
            // Preview 16: estados servicio de solo lectura, sin interacción privilegiada.
            var machineStatus = Path.Combine(tmp, "machine-status.json");
            await File.WriteAllTextAsync(machineStatus,
                "{\"Mode\":\"Vigilando\",\"Utc\":\"" +
                DateTimeOffset.UtcNow.ToString("O") +
                "\",\"Detail\":\"Prueba local\",\"Scanned\":7,\"Detections\":0," +
                "\"LostEvents\":0,\"Pending\":3,\"Reconciliations\":2," +
                "\"Signatures\":\"Firmas compartidas\"}");
            var machine = MachineServiceSnapshot.TryRead(machineStatus);
            Assert(machine is not null && !machine.IsHealthy(DateTimeOffset.UtcNow) &&
                   machine.Pending == 3 && machine.Reconciliations == 2,
                "servicio: cola pendiente no equivale a vigilancia saludable");
            Assert(machine is not null &&
                   (machine with { Pending = 0 }).IsHealthy(DateTimeOffset.UtcNow),
                "servicio: sin errores, latido reciente y cola vacía sí es saludable");
            await File.WriteAllTextAsync(machineStatus,
                "{\"Mode\":\"Vigilando\",\"Utc\":\"" +
                DateTimeOffset.UtcNow.ToString("O") + "\",\"Errors\":-1}");
            Assert(MachineServiceSnapshot.TryRead(machineStatus) is null,
                "servicio: contador negativo es registro inválido, nunca cero errores");
            await File.WriteAllTextAsync(machineStatus,
                "{\"Mode\":\"Degradado\",\"Utc\":\"" +
                DateTimeOffset.UtcNow.ToString("O") + "\",\"LostEvents\":4}");
            machine = MachineServiceSnapshot.TryRead(machineStatus);
            Assert(machine is not null && !machine.IsHealthy(DateTimeOffset.UtcNow) &&
                   machine.LostEvents == 4, "servicio: eventos perdidos nunca se muestran saludables");
            await File.WriteAllTextAsync(machineStatus,
                "{\"Mode\":\"Vigilando\",\"Utc\":\"" +
                DateTimeOffset.UtcNow.AddMinutes(-2).ToString("O") + "\"}");
            machine = MachineServiceSnapshot.TryRead(machineStatus);
            Assert(machine is not null && !machine.IsFresh(DateTimeOffset.UtcNow),
                "servicio: latido antiguo no implica vigilancia activa");
            await File.WriteAllTextAsync(machineStatus, "{no es json}");
            Assert(MachineServiceSnapshot.TryRead(machineStatus) is null,
                "servicio: datos incompletos o corruptos no causan excepción WPF");
            await File.WriteAllTextAsync(machineStatus, "{\"Mode\":\"Vigilando\"}");
            Assert(MachineServiceSnapshot.TryRead(machineStatus) is null,
                "servicio: falta de fecha no se confunde con protección activa");
            Console.WriteLine($"PRUEBAS CORRECTAS: {_passed}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[ERROR] " + ex);
            return 1;
        }
        finally
        {
            try { if (Directory.Exists(tmp)) Directory.Delete(tmp, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            try { QuarantineStore.CleanupTestRoot(); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            try { RemediationAuditLog.CleanupTestRoot(); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
