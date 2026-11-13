# SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
# SPDX-License-Identifier: GPL-3.0-or-later
# This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#requires -Version 5.1
# Verificación sin red y sin efectos secundarios sobre los archivos del usuario.
param([string]$ProjectRoot)
# FIX1: resolve paths after parameter binding (Windows PowerShell 5.1 -File).
$palClamScriptPath = $PSCommandPath
if ([string]::IsNullOrWhiteSpace($palClamScriptPath)) { $palClamScriptPath = $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($palClamScriptPath)) { throw 'No se puede determinar la ubicacion del script.' }
$palClamToolsDirectory = [IO.Path]::GetDirectoryName($palClamScriptPath)
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = [IO.Path]::GetFullPath((Join-Path $palClamToolsDirectory '..'))
}
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Stop-Build([string]$Message) { throw ('[ERROR PREVIO] ' + $Message) }
try {
    $root = (Resolve-Path -LiteralPath $ProjectRoot).Path
    $project = Join-Path $root 'PalClam\PalClam.csproj'
    $dashboardSource = Join-Path $root 'PalClam\MainWindow.Dashboard.cs'
    $mainSource = Join-Path $root 'PalClam\MainWindow.cs'
    if (-not (Test-Path -LiteralPath $dashboardSource -PathType Leaf)) {
        Stop-Build 'Falta MainWindow.Dashboard.cs (pantalla principal PRO).'
    }
    if (-not (Select-String -LiteralPath $dashboardSource -Pattern 'BuildProfessionalDashboard' -Quiet) -or
        -not (Select-String -LiteralPath $mainSource -Pattern 'BuildProfessionalDashboard\(content\)' -Quiet)) {
        Stop-Build 'La nueva pantalla principal no esta correctamente conectada.'
    }
    # Preview9: asegurar que Ajustes son independientes y que la barra flotante
    # es una ventana funcional (no una imagen o un control decorativo).
    foreach ($relative in @('PalClam\MainWindow.OptionsTabs.cs',
                            'docs\PRUEBAS_1.1.0_PREVIEW9.md',
                            'CAMBIOS_1.1.0_PREVIEW9.md')) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
            Stop-Build "Falta $relative (Opciones RC1)."
        }
    }
    if (-not (Select-String -LiteralPath $mainSource -Pattern 'OrganizeOptionsIntoTabs\(page\)' -Quiet) -or
        -not (Select-String -LiteralPath $mainSource -Pattern 'settingsShortcut = Nav' -Quiet) -or
        -not (Select-String -LiteralPath (Join-Path $root 'PalClam\FloatingScanWindow.cs') -Pattern 'DragMove\(\)' -Quiet)) {
        Stop-Build 'Los ajustes por pestañas o la barra flotante no estan conectados.'
    }
    # FIX2: bloquear referencias a miembros enum WPF y ColorConverter sin cualificar.
    # Con UseWindowsForms=true puede haber CS0104; en MainWindow, al heredar de
    # Window, "HorizontalAlignment.Center" puede resolver la propiedad de instancia
    # y fallar con CS0176/CS0120. Calificar ambos nombres evita las colisiones.
    foreach ($sourceFile in @(Get-ChildItem -LiteralPath (Join-Path $root 'PalClam') -Filter '*.cs' -Recurse -File)) {
        $sourceText = Get-Content -LiteralPath $sourceFile.FullName -Raw -Encoding UTF8
        if ($sourceText -match '(?<![\w.])HorizontalAlignment\.(Left|Right|Center|Stretch)\b' -or
            $sourceText -match '(?<![\w.])ColorConverter\.ConvertFromString\b' -or
            $sourceText -match '(?<![\w.])new\s+Point\s*\(') {
            Stop-Build ("Referencia WPF sin calificar en {0}; usar System.Windows.HorizontalAlignment, System.Windows.Media.ColorConverter o System.Windows.Point." -f $sourceFile.FullName)
        }
    }
    # RC1 FIX1: detectar la ambiguedad WPF / Windows Forms ANTES de dotnet.
    $layoutPolicy = Join-Path $root 'PalClam\Services\DashboardLayoutPolicy.cs'
    $dashboardText = Get-Content -LiteralPath $dashboardSource -Raw -Encoding UTF8
    if (-not (Test-Path -LiteralPath $layoutPolicy) -or
        $dashboardText -notmatch 'ConfigureDashboardResponsiveLayout' -or
        $dashboardText -notmatch 'CreateDashboardModuleIcon' -or
        $dashboardText -notmatch 'ArrangeDashboardRow' -or
        (Get-Content -LiteralPath (Join-Path $root 'PalClam.SmokeTests\Program.cs') -Raw -Encoding UTF8) -notmatch 'DashboardLayoutPolicy.ForWidth') {
        Stop-Build 'RC1: faltan panel adaptativo, iconos vectoriales o sus pruebas de ancho.'
    }
    $optionsTabsSource = Join-Path $root 'PalClam\MainWindow.OptionsTabs.cs'
    $optionsTabsText = Get-Content -LiteralPath $optionsTabsSource -Raw -Encoding UTF8
    if ($optionsTabsText -notmatch 'private System\.Windows\.Controls\.TabControl\?' -or
        $optionsTabsText -notmatch 'new System\.Windows\.Controls\.TabControl') {
        Stop-Build 'TabControl debe estar cualificado como WPF para evitar CS0104.'
    }
    # RC1: quitar pinceles vivos de los estilos sellados de WPF.
    $tabStyle = Join-Path $root 'PalClam\UI\OptionsTabAppearance.cs'
    $uiThemeTest = Join-Path $root 'PalClam.ThemeTests\PalClam.ThemeTests.csproj'
    if (-not (Test-Path -LiteralPath $tabStyle -PathType Leaf) -or
        -not (Test-Path -LiteralPath $uiThemeTest -PathType Leaf)) {
        Stop-Build 'Falta el estilo aislado de pestañas o su prueba WPF real.'
    }
    $tabStyleText = Get-Content -LiteralPath $tabStyle -Raw -Encoding UTF8
    if ($tabStyleText -notmatch 'using WpfControl = System\.Windows\.Controls\.Control;' -or
        $tabStyleText -match '(?<![\w.])Control\.') {
        Stop-Build 'El estilo nuevo debe usar alias WpfControl sin ambiguedad de Windows Forms.'
    }
    if ($optionsTabsText -notmatch 'OptionsTabAppearance\.CreateStyle\(\)' -or
        $optionsTabsText -notmatch 'OptionsTabAppearance\.Apply\(' -or
        $optionsTabsText -match 'tab\.Style\s*=\s*newStyle' -or
        $tabStyleText -notmatch 'snapshot\.Freeze\(\)' -or
        $tabStyleText -notmatch 'SelectedIndicator' -or
        $tabStyleText -notmatch 'IsKeyboardFocusWithinProperty' -or
        $tabStyleText -match 'new Setter\(WpfControl\.(?:Background|Foreground|BorderBrush)Property') {
        Stop-Build 'RC1: las pestañas deben mantener estilo y usar pinceles congelados independientes.'
    }
    if ($optionsTabsText -notmatch 'BuildOptionTabHeader' -or
        $optionsTabsText -notmatch 'Geometry\.Parse' -or
        $optionsTabsText -notmatch 'Style = tabs\.ItemContainerStyle' -or
        ([regex]::Matches($optionsTabsText, '(?m)^\s*Add\("')).Count -ne 6) {
        Stop-Build 'RC1: faltan las seis pestañas con iconos vectoriales.'
    }
    $appearanceSource = Join-Path $root 'PalClam\MainWindow.Appearance.cs'
    $appearanceText = Get-Content -LiteralPath $appearanceSource -Raw -Encoding UTF8
    if ($appearanceText -notmatch 'pair\.Value\.IsFrozen' -or
        $appearanceText -notmatch 'ParseThemeColor' -or
        $appearanceText -notmatch 'if \(selected\.Id != oldTheme\)' -or
        $appearanceText -notmatch 'Math\.Abs\(scale - _appliedTextScale\)' -or
        $appearanceText -notmatch 'CrashDiagnostics\.Record') {
        Stop-Build 'RC1: falta validacion atomica de paleta, aislamiento de escala o diagnostico.'
    }
    $testWrapper=Join-Path $root 'PRUEBAS.bat'
    if (((Test-Path -LiteralPath $testWrapper) -and -not (Select-String -LiteralPath $testWrapper -Pattern 'ConstruirDistribucion.ps1.*-Mode Test' -Quiet)) -or
        -not (Select-String -LiteralPath (Join-Path $root 'Tools\ConstruirDistribucion.ps1') -Pattern 'PalClam.ThemeTests' -Quiet)) {
        Stop-Build 'RC1: las pruebas WPF deben estar conectadas a PRUEBAS.bat.'
    }
    $diagnostics = Join-Path $root 'PalClam\Services\CrashDiagnostics.cs'
    $programText = Get-Content -LiteralPath (Join-Path $root 'PalClam\Program.cs') -Raw -Encoding UTF8
    if (-not (Test-Path -LiteralPath $diagnostics -PathType Leaf) -or
        $programText -notmatch 'DispatcherUnhandledException' -or
        $programText -notmatch 'CrashDiagnostics\.Record') {
        Stop-Build 'FIX3: falta el registro local de excepciones de WPF.'
    }
    $quickThemeSource = Join-Path $root 'PalClam\MainWindow.QuickThemes.cs'
    if (-not (Test-Path -LiteralPath $quickThemeSource -PathType Leaf) -or
        -not (Select-String -LiteralPath $quickThemeSource -Pattern 'private void ShowQuickThemeMenu' -Quiet) -or
        -not (Select-String -LiteralPath $mainSource -Pattern 'headRight.Children.Add\(CreateQuickThemeButton\(\)\)' -Quiet)) {
        Stop-Build 'Falta el selector rapido de color de RC1 FIX1.'
    }
    foreach ($relative in @('PalClam\MainWindow.ScanUX.cs',
        'PalClam\FloatingScanWindow.cs', 'PalClam\Services\ControlledScanRunner.cs',
        'docs\PRUEBAS_1.1.0_PREVIEW9.md', 'CAMBIOS_1.1.0_PREVIEW9.md', 'docs\PRUEBAS_1.1.0_PREVIEW11.md', 'CAMBIOS_1.1.0_PREVIEW11.md', 'docs\PRUEBAS_1.1.0_PREVIEW14.md', 'CAMBIOS_1.1.0_PREVIEW14.md')) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
            Stop-Build "Falta $relative (Scan Control RC1)."
        }
    }
    # RC1: Guard 2.0 conserva vigilancia durante pausa y regula la cola.
    $guardServicePath = Join-Path $root 'PalClam\Services\GuardService.cs'
    $guardPolicyPath = Join-Path $root 'PalClam\Services\GuardTuning.cs'
    $guardWindowPath = Join-Path $root 'PalClam\MainWindow.Guard.cs'
    $guardTrayPath = Join-Path $root 'PalClam\MainWindow.Tray.cs'
    $smokePath = Join-Path $root 'PalClam.SmokeTests\Program.cs'
    foreach ($path in @($guardServicePath, $guardPolicyPath, $guardWindowPath, $guardTrayPath)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Stop-Build "Falta $path" }
    }
    $guardServiceText = Get-Content -LiteralPath $guardServicePath -Raw -Encoding UTF8
    $guardUiText = Get-Content -LiteralPath $guardWindowPath -Raw -Encoding UTF8
    $guardTrayText = Get-Content -LiteralPath $guardTrayPath -Raw -Encoding UTF8
    $guardPolicyText = Get-Content -LiteralPath $guardPolicyPath -Raw -Encoding UTF8
    if ($guardServiceText -notmatch 'PauseProcessing\(' -or
        $guardServiceText -notmatch 'ResumeProcessing\(' -or
        $guardServiceText -notmatch 'WaitForQuietPeriodAsync' -or
        $guardUiText -notmatch 'GuardTuning.NormalizeMode' -or
        $guardTrayText -notmatch 'guard.PauseProcessing\(' -or
        $guardTrayText -match 'await StopGuardAsync\(\);\s*_guardPaused = true' -or
        $guardPolicyText -notmatch 'QuietRemainingMs' -or
        (Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8) -notmatch 'Guard 2.0: perfiles limitan cola') {
        Stop-Build 'RC1: faltan cola/coalescencia, pausa sin desactivar watchers o pruebas de Guard.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $root 'CAMBIOS_1.1.0_PREVIEW14.md')) -or
        -not (Test-Path -LiteralPath (Join-Path $root 'docs\PRUEBAS_1.1.0_PREVIEW14.md'))) {
        Stop-Build 'RC1: falta documentacion de los cambios y sus pruebas manuales.'
    }
    $installer = Join-Path $root 'Installer\PalClam.iss'
    $build = Join-Path $root 'Tools\Distribution.psm1'
    $validation = Join-Path $root 'Tools\ValidarDistribucion.ps1'
    $publisher = Join-Path $root 'Tools\PublicacionBloqueada.ps1'
    [xml]$xml = Get-Content -LiteralPath $project -Raw -Encoding UTF8
    $version = [string]$xml.Project.PropertyGroup.Version
    if ($version -ne '1.2.2') { Stop-Build "La version del proyecto es '$version', no 1.2.2." }
    if ([string]$xml.Project.PropertyGroup.TargetFramework -ne 'net10.0-windows') {
        Stop-Build 'El proyecto no utiliza .NET 10 para Windows.'
    }
    $issText = Get-Content -LiteralPath $installer -Raw -Encoding UTF8
    if ($issText -notmatch '(?m)^AppId=\{\{C74FA674-9A6D-471B-B1D0-195437635D29\}$') {
        Stop-Build 'El identificador Inno Setup ha cambiado y podria romper la actualizacion anterior.'
    }
    if ($issText -notmatch '(?m)^AppVerName=\{#MyAppName\} 1\.2\.2\s*$') {
        Stop-Build 'El nombre visual del instalador no corresponde a 1.2.2.'
    }
    if ($issText -notmatch '(?m)^OutputBaseFilename=\{#SetupBaseName\}\s*$') {
        Stop-Build 'El nombre del instalador no corresponde a 1.2.2.'
    }
    foreach ($file in @($build,$validation)) {
        if (-not (Select-String -LiteralPath $file -Pattern '1.2.2' -Quiet)) {
            Stop-Build "Referencia antigua a la version: $file"
        }
    }
    $publishText = Get-Content -LiteralPath $publisher -Raw -Encoding UTF8
    if ($publishText -notmatch '\[BLOQUEADO\]' -or $publishText -notmatch 'exit\s+1') {
        Stop-Build 'Las versiones Preview deben impedir la publicacion estable.'
    }
    # RC1: centro de firmas y actualizacion online del servicio.
    $sigUi = Join-Path $root 'PalClam\MainWindow.SignatureCenter.cs'
    $sigPolicy = Join-Path $root 'PalClam\Services\SignatureCenterInventory.cs'
    $machineHelper = Join-Path $root 'Tools\MachineService.ps1'
    foreach ($sourcePath in @($sigUi, $sigPolicy, $machineHelper,
                              (Join-Path $root 'CAMBIOS_1.1.0_PREVIEW20.md'),
                              (Join-Path $root 'docs\PRUEBAS_1.1.0_PREVIEW20.md'))) {
        if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            Stop-Build "RC1: falta $sourcePath"
        }
    }
    $sigText = Get-Content -LiteralPath $sigUi -Raw -Encoding UTF8
    $sigPolicyText = Get-Content -LiteralPath $sigPolicy -Raw -Encoding UTF8
    $machineHelperText = Get-Content -LiteralPath $machineHelper -Raw -Encoding UTF8
    if ($sigText -notmatch 'BuildSignatureCenterPanel' -or
        (Get-Content -LiteralPath $mainSource -Raw -Encoding UTF8) -notmatch 'BuildSignatureCenterPanel\(signaturePage\)' -or
        $sigPolicyText -notmatch 'MainAndDailyPresent' -or
        $machineHelperText -notmatch 'Update-SharedSignaturesOnline' -or
        $machineHelperText -notmatch 'Get-SignatureVersion' -or
        $machineHelperText -notmatch 'Require-Admin' -or
        $machineHelperText -notmatch 'Check-Ownership' -or
        $machineHelperText -notmatch 'Set-SecureSignatureAcl' -or
        (Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8) -notmatch 'Signature Center: main y daily') {
        Stop-Build 'RC1: integración de firmas incompleta o validación administrativa ausente.'
    }
    $privateFiles = @(Get-ChildItem -LiteralPath $root -File -Recurse -Force -ErrorAction Stop |
        Where-Object { $_.Name -match '(?i)^(release-private\.pem|id_rsa|private\.key)$' })
    if ($privateFiles.Count -ne 0) { Stop-Build 'La entrega contiene un archivo de clave privada.' }
    foreach ($relative in @('LICENSE-GPL-3.0.txt','THIRD-PARTY-NOTICES.md',
        'CAMBIOS_1.0.0.md','CAMBIOS_1.1.0_PREVIEW5.md','CAMBIOS_1.1.0_PREVIEW9.md','docs\PRUEBAS_1.0.0_ESTABLE.md',
        'docs\ACEPTACION_1.0.0.ini','docs\PRUEBAS_1.1.0_PREVIEW5.md','Tools\ValidarAceptacion.ps1',
        'PalClam\Assets\PalClam.ico','PalClam\Assets\Oscar-PalClam.gif',
        'PalClam\MainWindow.Advice.cs','PalClam\Services\SecurityAdviceCatalog.cs',
        'PalClam\MainWindow.Appearance.cs','PalClam\Services\AppearanceCatalog.cs',
        'PalClam\MainWindow.Help.cs','PalClam\Services\HelpContentCatalog.cs',
        'docs\AYUDA_FUNCIONES_VENTAJAS.md',
        'PalClam\MainWindow.SelfDefense.cs','PalClam\Services\SelfDefenseMonitor.cs',
        'PalClam\Services\SignatureHealthService.cs','PalClam\Services\MailTriageService.cs','PalClam\Services\MailFinding.cs',
        'CAMBIOS_1.1.0_PREVIEW5.md','CAMBIOS_1.1.0_PREVIEW6.md','CAMBIOS_1.1.0_PREVIEW8.md','CAMBIOS_1.1.0_PREVIEW9.md','CAMBIOS_1.1.0_PREVIEW11.md','CAMBIOS_1.1.0_PREVIEW14.md','docs\PRUEBAS_1.1.0_PREVIEW5.md','docs\PRUEBAS_1.1.0_PREVIEW6.md','docs\PRUEBAS_1.1.0_PREVIEW9.md','docs\PRUEBAS_1.1.0_PREVIEW11.md','docs\PRUEBAS_1.1.0_PREVIEW14.md')) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
            Stop-Build "Falta $relative"
        }
    }
    # RC1: no crear progreso falso ni una ETA durante pausa o cancelación.
    $progressPolicy = Join-Path $root 'PalClam\Services\ScanProgressPolicy.cs'
    $progressText = if (Test-Path -LiteralPath $progressPolicy) { Get-Content -LiteralPath $progressPolicy -Raw -Encoding UTF8 } else { '' }
    if ($progressText -notmatch 'internal static class ScanProgressPolicy' -or
        $progressText -notmatch 'ScanResultRateTracker' -or
        $progressText -notmatch 'ScanProgressPhase.Stopping' -or
        (Get-Content -LiteralPath $mainSource -Raw -Encoding UTF8) -notmatch 'ScanProgressPolicy.Evaluate' -or
        (Get-Content -LiteralPath $mainSource -Raw -Encoding UTF8) -notmatch 'FinishScanTelemetry\(finalStage, outcome\)' -or
        (Get-Content -LiteralPath (Join-Path $root 'PalClam.SmokeTests\Program.cs') -Raw -Encoding UTF8) -notmatch 'ScanProgressPolicy.Evaluate') {
        Stop-Build 'RC1: falta politica de progreso, resumen o pruebas de fase.'
    }
    # RC1: el inspector es una pantalla funcional, usa hash real en streaming
    # y reutiliza el analisis ClamAV que ya registra el historial del programa.
    $inspectorUi = Join-Path $root 'PalClam\MainWindow.Inspector.cs'
    $inspectorService = Join-Path $root 'PalClam\Services\FileInspectorService.cs'
    if (-not (Test-Path -LiteralPath $inspectorUi -PathType Leaf) -or
        -not (Test-Path -LiteralPath $inspectorService -PathType Leaf) -or
        -not (Test-Path -LiteralPath (Join-Path $root 'docs\PRUEBAS_1.1.0_PREVIEW14.md') -PathType Leaf)) {
        Stop-Build 'RC1: faltan los archivos del Inspector y su plan de pruebas.'
    }
    $inspectUi = Get-Content -LiteralPath $inspectorUi -Raw -Encoding UTF8
    $inspectSrv = Get-Content -LiteralPath $inspectorService -Raw -Encoding UTF8
    $smoke = Get-Content -LiteralPath (Join-Path $root 'PalClam.SmokeTests\Program.cs') -Raw -Encoding UTF8
    $main = Get-Content -LiteralPath $mainSource -Raw -Encoding UTF8
    if ($main -notmatch 'BuildInspectorPage\(inspectorPage\)' -or
        $main -notmatch '_pages.Add\("inspector", inspectorPage\)' -or
        $main -notmatch 'InspectSelectedFileAsync\(paths\[0\]\)' -or
        $inspectUi -notmatch 'StartScanAsync\(report.FullPath\)' -or
        $inspectUi -notmatch 'ScanHistory.ReadRecent' -or
        $inspectSrv -notmatch 'SHA256.HashDataAsync' -or
        $inspectSrv -notmatch 'X509Certificate.CreateFromSignedFile' -or
        $inspectSrv -notmatch 'no verifica' -or
        $smoke -notmatch 'FileInspectorService.InspectAsync' -or
        (Get-Content -LiteralPath (Join-Path $root 'PalClam.SmokeTests\PalClam.SmokeTests.csproj') -Raw -Encoding UTF8) -notmatch 'FileInspectorService.cs') {
        Stop-Build 'RC1: Inspector desconectado, veredicto no reutilizado o pruebas insuficientes.'
    }
    # RC1 final: aurora, tarjeta de estado y arranque opcional con Windows.
    $auroraFile = Join-Path $root 'PalClam\MainWindow.HeaderAurora.cs'
    $startupFile = Join-Path $root 'PalClam\Services\WindowsStartupRegistration.cs'
    $startupPolicy = Join-Path $root 'PalClam\Services\StartupLaunchPolicy.cs'
    $startupUi = Join-Path $root 'PalClam\MainWindow.WindowsStartup.cs'
    foreach ($newFile in @($auroraFile, $startupFile, $startupPolicy, $startupUi,
        (Join-Path $root 'docs\PRUEBAS_1.1.0_PREVIEW14_AURORA_INICIO.md'))) {
        if (-not (Test-Path -LiteralPath $newFile -PathType Leaf)) {
            Stop-Build "Falta componente de aurora o inicio Windows: $newFile"
        }
    }
    $auroraText = Get-Content -LiteralPath $auroraFile -Raw -Encoding UTF8
    $startupText = Get-Content -LiteralPath $startupFile -Raw -Encoding UTF8
    if ($main -notmatch 'CreateHeaderAurora\(\)' -or
        $main -notmatch 'WindowsStartupRegistration.GetScope' -or
        ($dashboardText -notmatch 'readiness\.ClearValue\(FrameworkElement\.WidthProperty\)' -or
         $dashboardText -notmatch 'readiness\.HorizontalAlignment = System\.Windows\.HorizontalAlignment\.Stretch' -or
         $dashboardText -notmatch 'heroGrid\.ColumnDefinitions\[1\]\.Width = stackedHero') -or
        $auroraText -notmatch 'UpdateHeaderAuroraPalette' -or
        $auroraText -notmatch 'using WpfPath = System\.Windows\.Shapes\.Path;' -or
        $auroraText -notmatch 'RefreshHeaderAuroraMotion' -or
        $startupText -notmatch 'Registry.CurrentUser' -or
        $startupText -notmatch 'TrySet\(StartupScope scope' -or
        $programText -notmatch 'StartupLaunchPolicy.IsHiddenStartup' -or
        $smoke -notmatch 'StartupLaunchPolicy.BuildRunCommand' -or
        $startupText -notmatch 'StartupScope.AllUsers' -or
        $startupText -notmatch 'RegistryHive.LocalMachine' -or
        $startupText -notmatch 'IsMachineInstallPath' -or
        $programText -notmatch '\-\-set-machine-startup' -or
        $issText -notmatch 'PrivilegesRequiredOverridesAllowed=dialog' -or
        $issText -notmatch 'DefaultDirName=\{autopf\}\\PalClam') {
        Stop-Build 'RC1 FIX2: faltan aurora WPF, diseño flexible o inicio individual/global e instalador.'
    }
    if ($issText -notmatch 'CurUninstallStepChanged' -or
        $issText -notmatch 'RegDeleteValue\(HKCU') {
        Stop-Build 'RC1: la desinstalacion debe limpiar el valor de inicio opcional.'
    }
    # RC1: servicio separado, instalado solo con autorización administrativa.
    foreach ($relative in @('PalClam.Service\PalClam.Service.csproj',
      'PalClam.Service\WindowsServiceHost.cs', 'PalClam.Service\MachineGuard.cs',
      'PalClam.Service\MachineGuardPolicy.cs', 'Tools\MachineService.ps1',
      'PalClam\MainWindow.MachineService.cs', 'docs\PRUEBAS_1.1.0_PREVIEW20.md',
      'CAMBIOS_1.1.0_PREVIEW20.md')) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
          Stop-Build ("RC1: falta componente del servicio experimental: {0}" -f $relative)
        }
    }
    $serviceProject = Get-Content -LiteralPath (Join-Path $root 'PalClam.Service\PalClam.Service.csproj') -Raw -Encoding UTF8
    $serviceCode = Get-Content -LiteralPath (Join-Path $root 'PalClam.Service\MachineGuard.cs') -Raw -Encoding UTF8
    $machineUi = Get-Content -LiteralPath (Join-Path $root 'PalClam\MainWindow.MachineService.cs') -Raw -Encoding UTF8
    # FIX1: separar las comprobaciones del ejecutable de las garantías de salud.
    # El mensaje «No se declara vigilancia activa» pertenece a la pantalla WPF;
    # MachineGuard informa mediante estados Vigilando/Degradado/Reconciliando.
    if ($serviceProject -notmatch 'net10\.0-windows' -or
      $serviceCode -notmatch 'Channel\.CreateBounded' -or
      $serviceCode -notmatch 'FileSystemWatcher' -or
      $machineUi -notmatch 'ConfigureMachineServiceAsync' -or
      $issText -notmatch 'PalClam\.Service\.exe' -or
      $issText -notmatch 'Tools\\MachineService\.ps1') {
      Stop-Build 'RC1: faltan el proyecto de servicio, la cola limitada, los observadores, la administración o el empaquetado Inno Setup.'
    }
    # RC1 FIX1: la salud puede priorizar un problema del motor/firmas antes
    # de Reconciliando/Degradado/Vigilando. Comprobar las garantías por separado,
    # no una expresión obsoleta que empieza directamente por "recovering".
    if ($serviceCode -notmatch 'ServicePaths\.SignaturesPresent\(' -or
        $serviceCode -notmatch 'MachineGuardPolicy\.CoreIssue\(' -or
        $serviceCode -notmatch 'File\.Exists\(ServicePaths\.ScannerPath\)') {
        Stop-Build 'RC1: Machine Guard debe comprobar el motor y las firmas durante su ejecución.'
    }
    if ($serviceCode -notmatch 'Volatile\.Read\(ref _degraded\)' -or
        $serviceCode -notmatch 'Interlocked\.Read\(ref _skipped\)' -or
        $serviceCode -notmatch 'Interlocked\.Read\(ref _errors\)' -or
        $serviceCode -notmatch 'Report\(\s*coreIssue\s+is\s+not\s+null\s*\?\s*"Error"' -or
        $serviceCode -notmatch 'recovering\s*\?\s*"Reconciliando"' -or
        $serviceCode -notmatch 'degraded\s*\?\s*"Degradado"\s*:\s*"Vigilando"') {
        Stop-Build 'RC1: Machine Guard debe reflejar Error, Reconciliando y Degradado sin declarar vigilancia sana.'
    }
    if ($machineUi -notmatch 'snap\.IsFresh\(' -or
        $machineUi -notmatch 'No se declara vigilancia activa' -or
        $machineUi -notmatch 'snap\.IsHealthy\(') {
        Stop-Build 'RC1: WPF debe avisar del latido antiguo y comprobar el estado saludable del servicio.'
    }
    # RC1: sin canal privilegiado desde WPF; comprobaciones estáticas
    # adicionales a la compilación y a los SmokeTests reales en Windows.
    $sharedStatus = Join-Path $root 'PalClam\Services\MachineServiceSnapshot.cs'
    $machinePaths = Join-Path $root 'PalClam.Service\ServicePaths.cs'
    $machineScript = Join-Path $root 'Tools\MachineService.ps1'
    $machineScreen = Join-Path $root 'PalClam\MainWindow.MachineService.cs'
    foreach ($file in @($sharedStatus, $machinePaths, $machineScript, $machineScreen)) {
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { Stop-Build "RC1: falta $file" }
    }
    if ((Get-Content -LiteralPath $sharedStatus -Raw -Encoding UTF8) -notmatch 'IsFresh\(' -or
        (Get-Content -LiteralPath $machineScreen -Raw -Encoding UTF8) -notmatch 'OnMachineStatusTick' -or
        (Get-Content -LiteralPath $machinePaths -Raw -Encoding UTF8) -notmatch 'StateFolder, "Signatures"' -or
        (Get-Content -LiteralPath $machineScript -Raw -Encoding UTF8) -notmatch 'RefreshSignatures' -or
        $serviceCode -notmatch 'ReconcileLoopAsync' -or
        $smoke -notmatch 'servicio: latido antiguo') {
        Stop-Build 'RC1: faltan estado vivo, firmas compartidas, recuperación o pruebas.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $root 'CAMBIOS_1.1.0_PREVIEW20.md'))) {
        Stop-Build 'RC1: falta su registro de cambios.'
    }
    # RC1: administración UAC explícita y notificaciones limitadas.
    foreach ($relative in @('PalClam\Services\GuardNoticeGate.cs',
                            'PalClam\Services\MachineGuardNoticePolicy.cs',
                            'docs\PRUEBAS_1.1.0_PREVIEW20.md',
                            'CAMBIOS_1.1.0_PREVIEW20.md')) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
            Stop-Build "RC1: falta $relative"
        }
    }
    $machineText = Get-Content -LiteralPath (Join-Path $root 'Tools\MachineService.ps1') -Raw -Encoding UTF8
    $machineUi = Get-Content -LiteralPath (Join-Path $root 'PalClam\MainWindow.MachineService.cs') -Raw -Encoding UTF8
    $notices = Get-Content -LiteralPath (Join-Path $root 'PalClam\Services\GuardNoticeGate.cs') -Raw -Encoding UTF8
    if ($machineText -notmatch "'Start','Stop','Restart'" -or
        $machineUi -notmatch 'ConfigureMachineServiceAsync\("Restart"\)' -or
        $machineUi -notmatch 'NotifyMachineCounterChanges' -or
        $notices -notmatch 'ShouldShow' -or
        $smoke -notmatch 'Guard: avisos de amenazas sin duplicados' -or
        $smoke -notmatch 'servicio: avisos solo tras cambios nuevos') {
        Stop-Build 'RC1: administración o pruebas de avisos incompletas.'
    }
    # RC1: centro de notificaciones de lectura, sin acciones sobre amenazas.
    $noticeCorePath = Join-Path $root 'PalClam\Services\SecurityNoticeFeed.cs'
    $noticeUiPath = Join-Path $root 'PalClam\MainWindow.SecurityCenter.cs'
    foreach ($file in @($noticeCorePath,$noticeUiPath,(Join-Path $root 'CAMBIOS_1.1.0_PREVIEW20.md'),
        (Join-Path $root 'docs\PRUEBAS_1.1.0_PREVIEW20.md'))) {
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { Stop-Build "RC1: falta $file" }
    }
    $noticeCore = Get-Content -LiteralPath $noticeCorePath -Raw -Encoding UTF8
    $noticeUi = Get-Content -LiteralPath $noticeUiPath -Raw -Encoding UTF8
    $noticeTests = Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8
    if ($main -notmatch 'BuildSecurityCenterPage\(securityPage\)' -or
        $main -notmatch '_pages.Add\("alerts", securityPage\)' -or
        $noticeCore -notmatch 'SecurityNoticeFeedResult ReadRecent' -or
        $noticeCore -notmatch 'SecurityNoticeReadMarker' -or
        $noticeUi -notmatch 'RefreshSecurityCenterAsync' -or
        $noticeUi -notmatch 'MarkSecurityCenterReviewed' -or
        $noticeUi -notmatch 'ExportSecurityCenter' -or
        $noticeTests -notmatch 'alertas: combina registros personales' -or
        (Get-Content -LiteralPath (Join-Path $root 'PalClam.SmokeTests\PalClam.SmokeTests.csproj') -Raw -Encoding UTF8) -notmatch 'SecurityNoticeFeed.cs') {
        Stop-Build 'RC1: centro de notificaciones o sus pruebas incompletos.'
    }
    # RC1 FIX2: PowerShell 5.1 usa la página ANSI para textos UTF-8 sin BOM
    # si no indicamos la codificación. Todas las lecturas Get-Content -Raw
    # incluyen -Encoding UTF8 para mantener comprobaciones reales con tildes
    # (reinserción, señala, inválido, validación) sin falsos negativos.
    # RC1: confirmar cableado funcional de Health 360 y Watch,
    # con sus preferencias voluntarias, auditoría y pruebas de regresión.
    $healthUi = Join-Path $root 'PalClam\MainWindow.Protection360.cs'
    $healthCore = Join-Path $root 'PalClam\Services\ProtectionHealthPolicy.cs'
    $volumeCore = Join-Path $root 'PalClam\Services\RemovableDriveMonitor.cs'
    $driveAudit = Join-Path $root 'PalClam\Services\DeviceWatchAudit.cs'
    foreach ($file in @($healthUi,$healthCore,$volumeCore,$driveAudit,
        (Join-Path $root 'CAMBIOS_1.1.0_PREVIEW20.md'),
        (Join-Path $root 'docs\PRUEBAS_1.1.0_PREVIEW20.md'))) {
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
            Stop-Build "RC1: falta el componente $file"
        }
    }
    $healthUiText = Get-Content -LiteralPath $healthUi -Raw -Encoding UTF8
    $healthCoreText = Get-Content -LiteralPath $healthCore -Raw -Encoding UTF8
    $volumeCoreText = Get-Content -LiteralPath $volumeCore -Raw -Encoding UTF8
    $guardText = Get-Content -LiteralPath $guardWindowPath -Raw -Encoding UTF8
    $noticeCoreText = Get-Content -LiteralPath $noticeCorePath -Raw -Encoding UTF8
    $newSmokeText = Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8
    $settingsText = Get-Content -LiteralPath (Join-Path $root 'PalClam\Infrastructure\SettingsStore.cs') -Raw -Encoding UTF8
    if ($main -notmatch 'BuildHealth360Page\(healthPage\)' -or
        $main -notmatch 'BuildDeviceGuardPage\(devicePage\)' -or
        $main -notmatch 'StartRemovableMonitoring\(\)' -or
        $healthUiText -notmatch 'ProtectionHealthPolicy\.Evaluate' -or
        $healthUiText -notmatch 'DriveInfo\.GetDrives\(' -or
        $healthUiText -notmatch 'ScanRemovableDrive\(' -or
        $volumeCoreText -notmatch 'RemovableDriveMonitor' -or
        $guardText -notmatch 'DownloadGuardPolicy\.Compose' -or
        $guardText -notmatch 'StartGuardOnLaunchAsync' -or
        $healthCoreText -notmatch 'HealthLevel\.Attention' -or
        $noticeCoreText -notmatch 'ParseDevice\(' -or
        $settingsText -notmatch 'UsbAutoScanEnabled \{ get; set; \} = false;' -or
        $settingsText -notmatch 'DownloadGuardEnabled \{ get; set; \} = false;' -or
        $newSmokeText -notmatch 'USB Watch: sin duplicados y reinserción detectable' -or
        $newSmokeText -notmatch 'salud 360: Guard degradado se señala') {
        Stop-Build 'RC1: Health 360 o USB/Download Guard incompletos o sin pruebas.'
    }
    [xml]$serviceXml = Get-Content -LiteralPath (Join-Path $root 'PalClam.Service\PalClam.Service.csproj') -Raw -Encoding UTF8
    if ([string]$serviceXml.Project.PropertyGroup.Version -ne '1.2.2') {
        Stop-Build 'RC1: version del servicio Windows no coincide.'
    }
    # RC1: remediación local sin ejecución ni restauración automática.
    $remedUi = Join-Path $root 'PalClam\MainWindow.Remediation.cs'
    $remedCore = Join-Path $root 'PalClam\Services\QuarantineRescanService.cs'
    $remedStore = Join-Path $root 'PalClam\Services\QuarantineStore.cs'
    $remedPolicy = Join-Path $root 'PalClam\Services\QuarantineReviewPolicy.cs'
    $remedAudit = Join-Path $root 'PalClam\Services\RemediationAuditLog.cs'
    foreach ($file in @($remedUi,$remedCore,$remedStore,$remedPolicy,$remedAudit,
        (Join-Path $root 'CAMBIOS_1.1.0_RC1.md'),
        (Join-Path $root 'docs\PRUEBAS_1.1.0_RC1.md'))) {
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
            Stop-Build "RC1: falta $file"
        }
    }
    $quarantineCode = Get-Content -LiteralPath $remedStore -Raw -Encoding UTF8
    $rescanCode = Get-Content -LiteralPath $remedCore -Raw -Encoding UTF8
    $remedUiCode = Get-Content -LiteralPath $remedUi -Raw -Encoding UTF8
    if ($main -notmatch 'RecheckQuarantineSelectedAsync\(' -or
        $quarantineCode -notmatch 'VerifyIsolatedAsync\(' -or
        $quarantineCode -notmatch 'SHA-256 de cuarentena diferente' -or
        $rescanCode -notmatch 'QuarantineStore.VerifyIsolatedAsync' -or
        $rescanCode -notmatch 'QuarantineStore.SaveRecheckAsync' -or
        $rescanCode -notmatch 'SignatureStore.HasSignatures' -or
        $remedUiCode -notmatch 'SelectedItems' -or
        $remedUiCode -notmatch 'ExportRemediationReport' -or
        $newSmokeText -notmatch 'Remediation PRO: cambio físico o SHA-256' -or
        $issText -notmatch 'CAMBIOS_1\.1\.0_RC1\.md') {
        Stop-Build 'RC1: faltan reanálisis aislado, control de integridad, interfaz o pruebas.'
    }
    # RC1: perfiles reales enlazados al motor, sin ETA inventada ni dos motores.
    $smartUi = Join-Path $root 'PalClam\MainWindow.SmartScan.cs'
    $smartPolicy = Join-Path $root 'PalClam\Services\SmartScanPlanPolicy.cs'
    $runnerPath = Join-Path $root 'PalClam\Services\ControlledScanRunner.cs'
    foreach ($source in @($smartUi,$smartPolicy,
        (Join-Path $root 'CAMBIOS_1.1.0_RC1.md'),
        (Join-Path $root 'docs\PRUEBAS_1.1.0_RC1.md'))) {
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { Stop-Build "RC1: falta $source" }
    }
    $smartUiCode = Get-Content -LiteralPath $smartUi -Raw -Encoding UTF8
    $smartPolicyCode = Get-Content -LiteralPath $smartPolicy -Raw -Encoding UTF8
    $runnerCode = Get-Content -LiteralPath $runnerPath -Raw -Encoding UTF8
    if ($main -notmatch 'BuildSmartScanPanel\(' -or
        $main -notmatch 'StartScanPlanAsync\(' -or
        $main -notmatch 'ScanFileEstimator.Estimate\(targets' -or
        $main -notmatch 'plan.BatchSize' -or
        $smartUiCode -notmatch 'StartPresetScanAsync' -or
        $smartUiCode -notmatch 'MessageBoxButton.YesNo' -or
        $smartPolicyCode -notmatch 'LocalFixedDrives' -or
        $smartPolicyCode -notmatch 'CollapseTargets' -or
        $runnerCode -notmatch 'IReadOnlyList<string> targets' -or
        $newSmokeText -notmatch 'Smart Scan: sin duplicados') {
        Stop-Build 'RC1: Smart Scan PRO no conectado o sin pruebas.'
    }
    if ($issText -notmatch 'CAMBIOS_1\.1\.0_PREVIEW21\.md' -or
        $issText -notmatch 'CAMBIOS_1\.1\.0_RC1\.md') {
        Stop-Build 'RC1: instalador sin historial de cambios.'
    }
    # RC1: panel visual, fuente local limitada y pruebas de fiabilidad.
    foreach ($relative in @('PalClam\MainWindow.DashboardInsights.cs',
          'PalClam\Services\DashboardOverviewPolicy.cs',
          'PalClam\Services\DashboardOverviewReader.cs',
          'docs\PRUEBAS_1.1.0_RC1.md',
          'CAMBIOS_1.1.0_RC1.md')) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
            Stop-Build "RC1: falta $relative"
        }
    }
    $insightsUi = Get-Content -LiteralPath (Join-Path $root 'PalClam\MainWindow.DashboardInsights.cs') -Raw -Encoding UTF8
    $insightsPolicy = Get-Content -LiteralPath (Join-Path $root 'PalClam\Services\DashboardOverviewPolicy.cs') -Raw -Encoding UTF8
    $insightsReader = Get-Content -LiteralPath (Join-Path $root 'PalClam\Services\DashboardOverviewReader.cs') -Raw -Encoding UTF8
    if ($dashboardText -notmatch 'BuildSecurityDashboardInsights\(page\)' -or
        $dashboardText -notmatch 'ArrangeDashboardModules\(' -or
        $main -notmatch 'RefreshDashboardInsightsAsync\(' -or
        $insightsUi -notmatch 'DashboardOverviewReader.Read\(' -or
        $insightsUi -notmatch 'Machine.IsFresh\(' -or
        $insightsPolicy -notmatch 'HasPartialData' -or
        $insightsReader -notmatch 'MaxHistoryBytes' -or
        $insightsReader -notmatch 'FileShare.ReadWrite' -or
        $newSmokeText -notmatch 'Dashboard 2.0: sin registros no se fabrican resultados' -or
        $issText -notmatch 'CAMBIOS_1\.1\.0_RC1\.md') {
        Stop-Build 'RC1: panel, telemetría o pruebas no integrados.'
    }
    # RC1: IA informativa con lista blanca, consentimiento y sin acciones sobre cuarentena.
    $analystPath = Join-Path $root 'PalClam\Services\AiSecurityAnalyst.cs'
    $analystUiPath = Join-Path $root 'PalClam\MainWindow.AiSecurityAnalyst.cs'
    $aiPagePath = Join-Path $root 'PalClam\MainWindow.AI.cs'
    $dashboardPolicyPath = Join-Path $root 'PalClam\Services\DashboardOverviewPolicy.cs'
    foreach ($required in @($analystPath, $analystUiPath, $aiPagePath,
        (Join-Path $root 'CAMBIOS_1.1.0_RC1.md'),
        (Join-Path $root 'docs\PRUEBAS_1.1.0_RC1.md'))) {
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
            Stop-Build "RC1: falta $required"
        }
    }
    $analyst = Get-Content -LiteralPath $analystPath -Raw -Encoding UTF8
    $analystUi = Get-Content -LiteralPath $analystUiPath -Raw -Encoding UTF8
    $aiPage = Get-Content -LiteralPath $aiPagePath -Raw -Encoding UTF8
    if ($analyst -notmatch 'ForNotices' -or $analyst -notmatch 'ForQuarantine' -or
        $analyst -notmatch 'ForHealth' -or $analyst -notmatch 'ForNotice' -or
        $analystUi -notmatch 'OpenAiAnalystWithPrompt' -or
        $analystUi -match 'AiGateway.QueryAsync' -or
        $aiPage -notmatch 'ResetAiConsent' -or
        $aiPage -notmatch 'MayContainPrivateData' -or
        $aiPage -notmatch 'MessageBoxButton.YesNo' -or
        (Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8) -notmatch 'AI Analyst: cuarentena solo contadores' -or
        (Get-Content -LiteralPath $dashboardPolicyPath -Raw -Encoding UTF8) -match 'int HistoryInvalidEntries, int HistoryInvalidEntries' -or
        $issText -notmatch 'CAMBIOS_1\.1\.0_RC1\.md') {
        Stop-Build 'RC1: IA, privacidad, pruebas o Dashboard incompletos.'
    }
    # RC1: cada comprobación referencia la implementación real, no
    # una etiqueta de versión ni un comentario de seguridad.
    $machineGuardText = Get-Content -LiteralPath (Join-Path $root 'PalClam.Service\MachineGuard.cs') -Raw -Encoding UTF8
    $machinePolicyText = Get-Content -LiteralPath (Join-Path $root 'PalClam.Service\MachineGuardPolicy.cs') -Raw -Encoding UTF8
    $snapshotText = Get-Content -LiteralPath (Join-Path $root 'PalClam\Services\MachineServiceSnapshot.cs') -Raw -Encoding UTF8
    $serviceScript = Get-Content -LiteralPath $machineHelper -Raw -Encoding UTF8
    if ($machineGuardText -notmatch 'MachineGuardPolicy.CoreIssue' -or
        $machineGuardText -notmatch 'Report\(coreIssue is not null \? "Error"' -or
        $machineGuardText -notmatch 'BoundedDiagnostic' -or
        $machinePolicyText -notmatch 'CoreIssue\(bool scannerAvailable, bool signaturesAvailable\)' -or
        $snapshotText -notmatch 'Pending == 0' -or
        $snapshotText -notmatch 'Contador de servicio inválido' -or
        (Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8) -notmatch 'cola pendiente no equivale a vigilancia saludable' -or
        $serviceScript -notmatch 'function Wait-ForRunning' -or
        $serviceScript -notmatch 'Wait-ForRunning' -or
        $serviceScript -notmatch '\$Action -eq ''Install'' -and -not \$wasRunning') {
        Stop-Build 'RC1: las correcciones de salud, servicio y firmas no están completas.'
    }
    $buildText = Get-Content -LiteralPath $build -Raw -Encoding UTF8
    $validationText = Get-Content -LiteralPath $validation -Raw -Encoding UTF8
    if ($buildText -notmatch 'ReleaseVersion\s*=\s*''1\.2\.2''' -or
        $validationText -notmatch 'PalClam_1\.2\.2_Setup_win-x64' -or
        $issText -notmatch 'CAMBIOS_1\.1\.0_RC1\.md' -or
        -not (Test-Path -LiteralPath (Join-Path $root 'docs\PRUEBAS_1.1.0_RC1.md'))) {
        Stop-Build 'RC1: instalador, validador o guía de pruebas desactualizados.'
    }
    # 1.1.0: aceptación explícita con evidencias reales. Jamás desbloquear publicación.
    $finalAcceptance = Join-Path $root 'docs\ACEPTACION_1.1.0_FINAL.ini'
    $finalGate = Join-Path $root 'Tools\ValidarEntrega110.ps1'
    $finalBatch = Join-Path $root 'Tools\ValidarEntrega110.ps1'
    $reviewBatch = Join-Path $root 'Tools\ValidarEntrega122.ps1'
    foreach ($item in @($finalAcceptance,$finalGate,$finalBatch,$reviewBatch,
        (Join-Path $root 'CAMBIOS_1.1.0_FINAL.md'),
        (Join-Path $root 'docs\PRUEBAS_1.1.0_FINAL.md'),
        (Join-Path $root 'docs\TRASPASO_A_WORK_MULTILENGUAJE.md'))) {
        if (-not (Test-Path -LiteralPath $item -PathType Leaf)) {
            Stop-Build "1.1.0: falta requisito de cierre: $item"
        }
    }
    $acceptanceText = Get-Content -LiteralPath $finalAcceptance -Raw -Encoding UTF8
    if ($acceptanceText -notmatch '(?m)^VERSION=1\.1\.0\s*$' -or
        $acceptanceText -notmatch '(?m)^APROBACION_AUTOR=(PENDIENTE|CONFORME)\s*$' -or
        (Get-Content -LiteralPath $finalGate -Raw -Encoding UTF8) -notmatch 'Pendientes de validación real' -or
        (Get-Content -LiteralPath $reviewBatch -Raw -Encoding UTF8) -notmatch 'ACEPTACION_1\.2\.2\.ini' -or
        $issText -notmatch 'CAMBIOS_1\.1\.0_FINAL\.md' -or
        $buildText -notmatch 'ReleaseVersion\s*=\s*''1\.2\.2''' -or
        $validationText -notmatch 'PalClam_1\.2\.2_Setup_win-x64' -or
        $publishText -notmatch 'BLOQUEADO' -or $publishText -notmatch 'exit\s+1') {
        Stop-Build '1.1.0: incongruencia de version, instalador o bloqueo de entrega.'
    }
    # 1.2.2: recursos y acta independientes; no reutilizar la aceptación 1.1.0.
    & (Join-Path $root 'Tools\VerificarLocalizacion.ps1') -ProjectRoot $root
    if ($LASTEXITCODE -ne 0) { Stop-Build 'Falló la verificación de localización 1.2.2.' }
    $acta120 = Get-Content -LiteralPath (Join-Path $root 'docs\ACEPTACION_1.2.2.ini') -Raw -Encoding UTF8
    if ($acta120 -notmatch '(?m)^VERSION=1\.2\.2\s*$' -or
        -not (Test-Path -LiteralPath (Join-Path $root 'Tools\ValidarEntrega122.ps1'))) {
        Stop-Build 'Faltan acta o puerta de aceptación de 1.2.2.'
    }
    Write-Host '[OK] PalClam 1.2.2, AppId preservado, localización inicial y publicación bloqueada.' -ForegroundColor Green
    exit 0
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
