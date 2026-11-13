; SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
; SPDX-License-Identifier: GPL-3.0-or-later
; This file is part of PalClam; see LICENSE-GPL-3.0.txt.
#define MyAppName "PalClam"
#define MyAppVersion "1.2.2"
#define MyAppExeName "PalClam.exe"

; PalClam 1.2.2: instaladores x64/x86 y automatico con motor oficial fijo y firmas iniciales.
; Las actualizaciones del motor posteriores se instalan SOLO desde la interfaz.
#ifndef BuildArch
  #define BuildArch "dual"
#endif
#if BuildArch == "x64"
  #define TargetRid "win-x64"
  #define SetupBaseName "PalClam_1.2.2_Setup_win-x64"
#elif BuildArch == "x86"
  #define TargetRid "win-x86"
  #define SetupBaseName "PalClam_1.2.2_Setup_win-x86"
#elif BuildArch == "dual"
  #define SetupBaseName "PalClam_1.2.2_Setup"
#else
  #error Arquitectura de instalador no admitida
#endif

[Setup]
AppId={{C74FA674-9A6D-471B-B1D0-195437635D29}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} 1.2.2
AppPublisher=palentino.es
AppPublisherURL=https://palclam.com/
DefaultDirName={autopf}\PalClam
DefaultGroupName=PalClam
OutputDir=..\dist\installer
OutputBaseFilename={#SetupBaseName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
#if BuildArch == "x64"
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#elif BuildArch == "x86"
ArchitecturesAllowed=x86compatible
ArchitecturesInstallIn64BitMode=
#else
ArchitecturesAllowed=x86compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif
MinVersion=10.0
UninstallDisplayIcon={app}\PalClam.exe
SetupIconFile=..\PalClam\Assets\PalClam.ico
LicenseFile=..\LICENSE-GPL-3.0.txt
CloseApplications=yes
RestartApplications=no
UsePreviousAppDir=yes
LanguageDetectionMethod=uilanguage

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[CustomMessages]
spanish.DesktopShortcut=Crear acceso directo en el escritorio
spanish.ShortcutGroup=Accesos directos:
english.DesktopShortcut=Create a desktop shortcut
english.ShortcutGroup=Shortcuts:
french.DesktopShortcut=Créer un raccourci sur le bureau
french.ShortcutGroup=Raccourcis :
german.DesktopShortcut=Desktop-Verknüpfung erstellen
german.ShortcutGroup=Verknüpfungen:
portuguese.DesktopShortcut=Criar um atalho no ambiente de trabalho
portuguese.ShortcutGroup=Atalhos:
italian.DesktopShortcut=Crea un collegamento sul desktop
italian.ShortcutGroup=Collegamenti:

spanish.PortableFolder=Esta carpeta contiene una copia portable. Elige otra carpeta de instalación.
english.PortableFolder=This folder contains a portable copy. Choose a different installation folder.
french.PortableFolder=Ce dossier contient une copie portable. Choisissez un autre dossier.
german.PortableFolder=Dieser Ordner enthält eine portable Kopie. Wählen Sie einen anderen Ordner.
portuguese.PortableFolder=Esta pasta contém uma cópia portátil. Escolha outra pasta de instalação.
italian.PortableFolder=Questa cartella contiene una copia portatile. Scegli un’altra cartella.

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopShortcut}"; GroupDescription: "{cm:ShortcutGroup}"; Flags: unchecked

[Files]
; Aplicacion publicada y runtime .NET 10 autocontenido.
#if BuildArch == "dual"
Source: "..\PalClam\dist\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: Is64BitInstallMode
Source: "..\PalClam\dist\win-x86\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: not Is64BitInstallMode
Source: "..\PalClam.Service\dist\win-x64\PalClam.Service.exe"; DestDir: "{app}"; Flags: ignoreversion; Check: Is64BitInstallMode
Source: "..\PalClam.Service\dist\win-x86\PalClam.Service.exe"; DestDir: "{app}"; Flags: ignoreversion; Check: not Is64BitInstallMode
#else
Source: "..\PalClam\dist\{#TargetRid}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\PalClam.Service\dist\{#TargetRid}\PalClam.Service.exe"; DestDir: "{app}"; Flags: ignoreversion
#endif
Source: "..\Tools\MachineService.ps1"; DestDir: "{app}\Tools"; Flags: ignoreversion
Source: "..\PalClam.Service\*.cs"; DestDir: "{app}\Source\PalClam.Service"; Flags: ignoreversion
Source: "..\PalClam.Service\*.csproj"; DestDir: "{app}\Source\PalClam.Service"; Flags: ignoreversion
; ClamAV de la arquitectura verificada al construir. Se instala bajo {app}, nunca en rutas globales.
#if BuildArch == "dual"
Source: "..\ThirdParty\ClamAV\win-x64\*"; DestDir: "{app}\ClamAV"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: Is64BitInstallMode
Source: "..\ThirdParty\ClamAV\win-x86\*"; DestDir: "{app}\ClamAV"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: not Is64BitInstallMode
#else
Source: "..\ThirdParty\ClamAV\{#TargetRid}\*"; DestDir: "{app}\ClamAV"; Flags: ignoreversion recursesubdirs createallsubdirs
#endif
; Bases iniciales preparadas por FreshClam en el PC del desarrollador.
; La app las copia al perfil en el primer arranque sin sobreescribir firmas nuevas.
Source: "..\ThirdParty\OfflineSignatures\*.cvd"; DestDir: "{app}\OfflineSignatures"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\ThirdParty\OfflineSignatures\*.cld"; DestDir: "{app}\OfflineSignatures"; Flags: ignoreversion skipifsourcedoesntexist
; Fuente correspondiente al motor GPL incluido (auditable en la instalacion).
Source: "..\ThirdParty\SourcePackages\clamav-1.5.4.tar.gz"; DestDir: "{app}\Source\ClamAV"; Flags: ignoreversion
Source: "..\ThirdParty\CHECKSUMS.txt"; DestDir: "{app}\Source\ClamAV"; Flags: ignoreversion
; Licencias y fuentes completas conservadas también al redistribuir instaladores.
Source: "..\dist\legal\*"; DestDir: "{app}\Licenses"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\COPYRIGHT.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\source\PalClam_1.2.2_Source.zip"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\ThirdParty\SourcePackages\Libraries\*.zip"; DestDir: "{app}\Source\Libraries"; Flags: ignoreversion
; Distribuir tambien el codigo fuente completo correspondiente a PalClam.
Source: "..\COPYING"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE-GPL-3.0.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\BUILD_FROM_SOURCE.md"; DestDir: "{app}\Source"; DestName: "README.md"; Flags: ignoreversion
Source: "..\THIRD-PARTY-NOTICES.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_0.6.0.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_0.7.0.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_0.9.0.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_0.10.0.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.0.0_RC2.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.0.0_RC3.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.0.0_RC4.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.0.0_RC5.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.0.0_RC7.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.0.0_RC8.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.0.0.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW1.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW2.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW3.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW4.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW5.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW6.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW7.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW8.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW9.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW10.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW11.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW12.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW13.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW14.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW15.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW16.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW17.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW18.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW19.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW20.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW21.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_PREVIEW22.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_RC1.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.1.0_FINAL.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\docs\PRUEBAS_1.1.0_RC1.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\PalClam.SmokeTests\*.cs"; DestDir: "{app}\Source\PalClam.SmokeTests"; Flags: ignoreversion
Source: "..\PalClam.SmokeTests\*.csproj"; DestDir: "{app}\Source\PalClam.SmokeTests"; Flags: ignoreversion
Source: "..\PalClam.ThemeTests\*.cs"; DestDir: "{app}\Source\PalClam.ThemeTests"; Flags: ignoreversion
Source: "..\PalClam.ThemeTests\*.csproj"; DestDir: "{app}\Source\PalClam.ThemeTests"; Flags: ignoreversion
Source: "..\docs\*"; DestDir: "{app}\Source\docs"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\PalClam\*.cs"; DestDir: "{app}\Source\PalClam"; Flags: ignoreversion
Source: "..\PalClam\*.csproj"; DestDir: "{app}\Source\PalClam"; Flags: ignoreversion
Source: "..\PalClam\*.manifest"; DestDir: "{app}\Source\PalClam"; Flags: ignoreversion
Source: "..\PalClam\Infrastructure\*.cs"; DestDir: "{app}\Source\PalClam\Infrastructure"; Flags: ignoreversion
Source: "..\PalClam\UI\*.cs"; DestDir: "{app}\Source\PalClam\UI"; Flags: ignoreversion
Source: "..\PalClam\UI\*.xaml"; DestDir: "{app}\Source\PalClam\UI"; Flags: ignoreversion
Source: "..\PalClam\Services\*.cs"; DestDir: "{app}\Source\PalClam\Services"; Flags: ignoreversion
Source: "..\PalClam\Assets\*"; DestDir: "{app}\Source\PalClam\Assets"; Flags: ignoreversion
Source: "..\PalClam\Resources\*.pem"; DestDir: "{app}\Source\PalClam\Resources"; Flags: ignoreversion
Source: "..\PalClam\Updater\*.ps1"; DestDir: "{app}\Source\PalClam\Updater"; Flags: ignoreversion
Source: "..\Tools\ReleaseSigner\*.cs"; DestDir: "{app}\Source\Tools\ReleaseSigner"; Flags: ignoreversion
Source: "..\Tools\ReleaseSigner\*.csproj"; DestDir: "{app}\Source\Tools\ReleaseSigner"; Flags: ignoreversion
Source: "..\Tools\*.ps1"; DestDir: "{app}\Source\Tools"; Flags: ignoreversion
Source: "..\Tools\*.psm1"; DestDir: "{app}\Source\Tools"; Flags: ignoreversion
Source: "..\Tools\*.py"; DestDir: "{app}\Source\Tools"; Flags: ignoreversion
Source: "..\Tools\LEEME.txt"; DestDir: "{app}\Source\Tools"; Flags: ignoreversion
Source: "..\Installer\PalClam.iss"; DestDir: "{app}\Source\Installer"; Flags: ignoreversion

Source: "..\CAMBIOS_1.2.0.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\CAMBIOS_1.2.0_PREVIEW3.md"; DestDir: "{app}\Source"; Flags: ignoreversion
; Localización y pruebas correspondientes a esta versión.
Source: "..\PalClam\Localization\*"; DestDir: "{app}\Source\PalClam\Localization"; Flags: ignoreversion
Source: "..\PalClam.FeatureTests\*.cs"; DestDir: "{app}\Source\PalClam.FeatureTests"; Flags: ignoreversion
Source: "..\PalClam.FeatureTests\*.csproj"; DestDir: "{app}\Source\PalClam.FeatureTests"; Flags: ignoreversion
Source: "..\PalClam.LocalizationTests\*.cs"; DestDir: "{app}\Source\PalClam.LocalizationTests"; Flags: ignoreversion
Source: "..\PalClam.LocalizationTests\*.csproj"; DestDir: "{app}\Source\PalClam.LocalizationTests"; Flags: ignoreversion
Source: "..\PalClam.LocalizationTests\Fixtures\*.resx"; DestDir: "{app}\Source\PalClam.LocalizationTests\Fixtures"; Flags: ignoreversion
Source: "..\CAMBIOS_1.2.0_PREVIEW1.md"; DestDir: "{app}\Source"; Flags: ignoreversion

Source: "..\Tools\LocalizationInventory\*.cs"; DestDir: "{app}\Source\Tools\LocalizationInventory"; Flags: ignoreversion
Source: "..\Tools\LocalizationInventory\*.csproj"; DestDir: "{app}\Source\Tools\LocalizationInventory"; Flags: ignoreversion

Source: "..\CAMBIOS_1.2.2.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\PalClam.DistributionTests\*.cs"; DestDir: "{app}\Source\PalClam.DistributionTests"; Flags: ignoreversion
Source: "..\PalClam.DistributionTests\*.csproj"; DestDir: "{app}\Source\PalClam.DistributionTests"; Flags: ignoreversion

; Instrucciones publicas y propiedades de compilacion: sin BAT.
Source: "..\BUILD_FROM_SOURCE.md"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\Directory.Build.props"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\LICENSE-GPL-3.0.txt"; DestDir: "{app}\Source"; Flags: ignoreversion
Source: "..\COPYING"; DestDir: "{app}\Source"; Flags: ignoreversion

[InstallDelete]
; Solo accesos BAT antiguos incluidos por PalClam, nunca Data ni archivos del usuario.
Type: files; Name: "{app}\Source\BUILD_INSTALLER.bat"
Type: files; Name: "{app}\Source\COMPILAR.bat"
Type: files; Name: "{app}\Source\CREAR_INSTALADOR.bat"
Type: files; Name: "{app}\Source\CREAR_PORTABLE.bat"
Type: files; Name: "{app}\Source\DIAGNOSTICO.bat"
Type: files; Name: "{app}\Source\EJECUTAR.bat"
Type: files; Name: "{app}\Source\EMPEZAR-AQUI.bat"
Type: files; Name: "{app}\Source\EMPEZAR_AQUI.bat"
Type: files; Name: "{app}\Source\GENERAR_SOLO_INSTALADOR.bat"
Type: files; Name: "{app}\Source\INICIALIZAR_ACTUALIZACIONES.bat"
Type: files; Name: "{app}\Source\INSTALAR.bat"
Type: files; Name: "{app}\Source\PREPARAR_MOTOR.bat"
Type: files; Name: "{app}\Source\PREPARAR_OFFLINE.bat"
Type: files; Name: "{app}\Source\PRUEBAS.bat"
Type: files; Name: "{app}\Source\PUBLICAR_ACTUALIZACION.bat"
Type: files; Name: "{app}\Source\PalClam\COMPILAR-RAPIDO.bat"
Type: files; Name: "{app}\Source\PalClam\COMPILAR.bat"
Type: files; Name: "{app}\Source\PalClam\EJECUTAR.bat"
Type: files; Name: "{app}\Source\REVISAR_ENTREGA.bat"
Type: files; Name: "{app}\Source\VALIDAR_CANDIDATA_RC1.bat"
Type: files; Name: "{app}\Source\VALIDAR_ENTREGA_110.bat"
Type: files; Name: "{app}\Source\VALIDAR_ENTREGA_120.bat"
Type: files; Name: "{app}\Source\VALIDAR_ENTREGA_121.bat"
Type: files; Name: "{app}\Source\VALIDAR_ENTREGA_122.bat"
Type: files; Name: "{app}\Source\VALIDAR_PAQUETE.bat"
Type: files; Name: "{app}\Source\VERIFICAR_PUBLICACION.bat"

[Icons]
Name: "{group}\PalClam"; Filename: "{app}\PalClam.exe"; IconFilename: "{app}\PalClam.exe"
Name: "{autodesktop}\PalClam"; Filename: "{app}\PalClam.exe"; IconFilename: "{app}\PalClam.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\PalClam.exe"; Description: "{cm:LaunchProgram,PalClam}"; Flags: nowait postinstall skipifsilent

[Registry]
; No crear accesos contextuales durante Setup: la función es voluntaria en Opciones.
; Eliminar únicamente nuestros verbos propios al desinstalar.
Root: HKCU; Subkey: "Software\Classes\*\shell\PalClamScan"; Flags: dontcreatekey uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Directory\shell\PalClamScan"; Flags: dontcreatekey uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Drive\shell\PalClamScan"; Flags: dontcreatekey uninsdeletekey

[UninstallRun]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoLogo -NoProfile -ExecutionPolicy Bypass -File ""{app}\Tools\MachineService.ps1"" -Action Remove"; Flags: runhidden; RunOnceId: "PalClamRemoveMachineGuard"; Check: IsAdminInstallMode
; Eliminar la tarea propia para evitar que Windows ejecute una ruta ya desinstalada.
Filename: "{sys}\schtasks.exe"; Parameters: "/Delete /TN ""PalClam - Analisis programado"" /F"; Flags: runhidden; RunOnceId: "PalClamDeleteScheduledTask"

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if FileExists(ExpandConstant('{app}\palclam.portable')) then
    Result := CustomMessage('PortableFolder');
end;

// Inicio automático opt-in: no activarlo durante Setup.
// Eliminar las entradas SOLO cuando apuntan a ESTA instalación, no a otra copia.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Command, OurCommand: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    OurCommand := Lowercase('"' + ExpandConstant('{app}\PalClam.exe') + '" --startup-hidden');
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'PalClam', Command) then
    begin
      if Lowercase(Command) = OurCommand then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'PalClam');
    end;
    if IsAdminInstallMode then
    begin
      if RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Run', 'PalClam', Command) then
      begin
        if Lowercase(Command) = OurCommand then
          RegDeleteValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Run', 'PalClam');
      end;
    end;
  end;
end;
