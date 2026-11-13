# PalClam 1.2.2 — construcción desde las fuentes públicas

Estas fuentes no incluyen accesos BAT. Los scripts PowerShell contienen el proceso real de compilación, validación, preparación del motor, firmas, instaladores y portables. El código propio es GPL-3.0-or-later; COPYING, LICENSE-GPL-3.0.txt y COPYRIGHT.md indican los términos. Las bibliotecas y ClamAV conservan sus licencias.

Extrae todo `PalClam_1.2.2_Source.zip` en una carpeta de trabajo. No compiles directamente desde el ZIP. Instala SDK .NET 10, Windows PowerShell 5.1 o posterior e Inno Setup 6.3 o posterior. La primera construcción requiere acceso a NuGet, ClamAV y sus fuentes oficiales. Las fuentes exactas de OpenMcdf y UTF.Unknown se incluyen en ThirdParty/SourcePackages/Libraries con sus hashes; el motor ClamAV y sus firmas se preparan al construir.

Abre una consola en la raíz extraída y ejecuta:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File .\Tools\ConstruirDistribucion.ps1 -Mode All
```

También puedes indicar una ruta absoluta, aunque la consola esté en otra carpeta:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "C:\Mi carpeta\PalClam\Tools\ConstruirDistribucion.ps1" -ProjectRoot "C:\Mi carpeta\PalClam" -Mode All
```

Para comprobar solo la localización del proyecto, sin compilar, instalar ni descargar:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File .\Tools\ConstruirDistribucion.ps1 -Mode CheckPaths
```

Fases disponibles: `Test`, `Build`, `Prepare`, `Installer`, `Portable` y `Validate`. `Installer` y `Portable` requieren las publicaciones, motor y firmas ya preparados; `All` recorre las fases en orden y detiene el proceso ante un error. Las pruebas de aceptación sobre el equipo real siguen en docs/ACEPTACION_1.2.2.ini.

Las cinco salidas son los instaladores automático, x64 y x86 en dist/installer y las portables x64/x86 en dist/portable. Incluyen licencias y fuentes correspondientes. El resultado público no incluye BAT en Source. Si no se localiza Inno Setup, configura INNO_SETUP_COMPILER con la ruta completa a ISCC.exe.

ClamAV es un proceso externo GPLv2. No se requiere ninguna clave privada del autor para compilar y ejecutar una copia; el canal oficial de actualizaciones conserva su clave pública. No publiques datos de usuario, cuarentena, secretos ni claves privadas. La construcción no sube ni publica actualizaciones automáticamente; Tools/PublicacionBloqueada.ps1 mantiene ese bloqueo explícito.
