# PalClam

[English](#english) | [Español](#español)

## English

**Free and open-source antivirus for Windows, powered by ClamAV.**

PalClam provides a modern interface for scanning files and email, managing quarantine, and monitoring folder changes. It includes a reactive anti-ransomware monitor, optional AI assistance, and a configurable disk activity indicator.

**Version:** 1.2.2 · **Architectures:** x64 and x86 · **Distribution:** installer and portable · **Project license:** GPLv3 or later.

[Downloads and releases](https://github.com/oscardelacuesta/PalClam/releases) · [Report an issue](https://github.com/oscardelacuesta/PalClam/issues) · [Author's blog](https://www.palentino.es/)

### Main features

| Feature | What it provides |
|---|---|
| Antivirus scanning | Scan files, folders, and drives using the ClamAV engine. |
| Quarantine and history | Reversible isolation, restoration, and a history of results and actions. |
| Scheduled scans | Schedule scans in the installed edition. |
| Folder monitoring | Guard observes changes in configured folders and allows affected files to be scanned. |
| Email and attachments | Scan EML, MSG, and OFT files; import MBOX and Maildir mailboxes; integrate with classic Outlook and Microsoft Graph. |
| Anti-ransomware monitor | Alerts for patterns of changes and renaming that may indicate suspicious activity in selected folders. |
| USB and downloads | Detect newly connected removable drives and configure monitoring of the downloads folder. |
| PalClam AI | Optional assistance for interpreting security information using local or remote providers configured by the user. |
| Disk activity LED | Configurable indicators for reading, writing, scanning, detections, and low disk space. |
| Customization | Seven themes and six languages, with the initial language selected from Windows settings. |
| Engine and signatures | ClamAV preparation, initial databases for offline scans, and signature updates. |

### Downloads

Visit [Releases](https://github.com/oscardelacuesta/PalClam/releases) to find the published files for each version.

| Version 1.2.2 file | Purpose |
|---|---|
| `PalClam_1.2.2_Setup.exe` | Installer that selects x64 or x86 according to Windows. |
| `PalClam_1.2.2_Setup_win-x64.exe` | Dedicated 64-bit installer. |
| `PalClam_1.2.2_Setup_win-x86.exe` | Dedicated 32-bit installer. |
| `PalClam_1.2.2_Portable_win-x64.zip` | 64-bit portable edition. |
| `PalClam_1.2.2_Portable_win-x86.zip` | 32-bit portable edition. |
| `PalClam_1.2.2_Source.zip` | Corresponding source code and build instructions. |

Application packages include the .NET runtime and the engine for their architecture. You do not need to install the SDK or development tools to use the distributed application.

#### Installed edition

Download and run the installer, then follow its instructions. The automatic installer is recommended unless you need to select a specific architecture. Application data is stored in your Windows user profile.

#### Portable edition

Extract **the entire ZIP** into a writable folder and run `PalClam.exe`. Application data is stored in the `Data` folder beside the program. Keep the `palclam.portable` file, which enables portable mode.

The portable edition does not register services, scheduled tasks, automatic startup, or Explorer integration. To update it, close PalClam and replace the program files while preserving `Data`.

### Protection scope

PalClam is a complementary security tool. Guard and the anti-ransomware monitor are **reactive**: they observe changes and trigger scans or alerts. They do not provide preventive write blocking or decrypt affected files. Suspicious behavior alerts are distinguished from detections reported by the ClamAV engine.

Email scanning uses the supported formats and integrations. Classic Outlook requires its local integration, and Microsoft Graph requires user authorization. Coverage depends on configuration and access to the selected messages.

PalClam AI is optional. Remote providers require the appropriate configuration and credentials; sending text requires the consent provided for in the application. AI assistance supplements the interpretation of results.

### Languages

Spanish, English, French, German, Portuguese, and Italian. The application initially selects its language from Windows settings and lets you change it in the options.

### Building from source

Builds are performed on Windows. Requirements:

- .NET 10 SDK.
- Windows PowerShell 5.1 or later.
- Inno Setup 6.3 or later to generate installers.
- An Internet connection for the initial preparation of dependencies, the engine, and signatures.

Extract the public source ZIP, open PowerShell in its root folder, and run:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File .\Tools\ConstruirDistribucion.ps1 -Mode All
```

The process runs checks, builds both architectures, prepares the engine and signatures, and generates installers and portable packages. Output files are placed in `dist/installer` and `dist/portable`. A failure stops the build.

The public sources include the PowerShell scripts required to rebuild the project. Full instructions are provided in `BUILD_FROM_SOURCE.md`, included in the source ZIP. The author's private keys are not required to build and run a copy.

### License and third-party components

Copyright © 2026 **Óscar de la Cuesta Campillo**.

PalClam's own code is distributed under the **GNU General Public License, version 3 or later** (`GPL-3.0-or-later`). License texts and notices are included in `COPYING`, `LICENSE-GPL-3.0.txt`, and `COPYRIGHT.md`.

ClamAV is used as an external program and retains its GPLv2 license. Libraries and other components retain their original licenses and attributions, detailed in `THIRD-PARTY-NOTICES.md` and the license files included with the distributions.

When redistributing packages, preserve their `Licenses` and `Source` folders and the corresponding notices. Published sources must correspond to the version of the distributed binaries.

PalClam is an independent project. Third-party trademarks belong to their respective owners.

### Contributing

Suggestions, translations, and bug reports are welcome through [Issues](https://github.com/oscardelacuesta/PalClam/issues). When reporting a problem, include your PalClam version, Windows version, steps to reproduce it, and the error message.

### Author

Developed by **Óscar de la Cuesta Campillo**.

- Blog: [palentino.es](https://www.palentino.es/).
- Planned product website: [palclam.com](https://palclam.com/) — in preparation.

---

## Español

**Antivirus gratuito y de código abierto para Windows, con motor ClamAV.**

PalClam ofrece una interfaz moderna para analizar archivos y correos, gestionar la cuarentena y observar cambios en carpetas. Incorpora un monitor anti-ransomware reactivo, asistencia opcional con IA y un indicador de actividad del disco configurable.

**Versión:** 1.2.2 · **Arquitecturas:** x64 y x86 · **Distribución:** instalador y portable · **Licencia propia:** GPLv3 o posterior.

[Descargas y versiones](https://github.com/oscardelacuesta/PalClam/releases) · [Comunicar una incidencia](https://github.com/oscardelacuesta/PalClam/issues) · [Blog del autor](https://www.palentino.es/)

### Funciones principales

| Función | Qué ofrece |
|---|---|
| Análisis antivirus | Análisis de archivos, carpetas y unidades con el motor ClamAV. |
| Cuarentena e historial | Aislamiento reversible, restauración y consulta de los resultados y acciones realizadas. |
| Análisis programados | Programación de análisis en la edición instalada. |
| Vigilancia de carpetas | Guard observa cambios en las carpetas configuradas y permite analizar los archivos afectados. |
| Correo y adjuntos | Análisis de EML, MSG y OFT; importación MBOX y Maildir; integración con Outlook clásico y Microsoft Graph. |
| Monitor anti-ransomware | Avisos ante patrones de cambios y renombrados compatibles con actividad sospechosa en las carpetas elegidas. |
| USB y descargas | Detección de nuevas unidades extraíbles y vigilancia configurable de la carpeta de descargas. |
| PalClam AI | Asistencia opcional para interpretar información de seguridad con proveedores locales o remotos configurados por el usuario. |
| LED de disco | Indicador configurable de lectura, escritura, análisis, detecciones y poco espacio disponible. |
| Personalización | Siete temas y seis idiomas, con selección inicial según el idioma de Windows. |
| Motor y firmas | Preparación de ClamAV, bases iniciales para análisis sin conexión y actualización de firmas. |

### Descargas

Consulta [Releases](https://github.com/oscardelacuesta/PalClam/releases) para acceder a los archivos publicados de cada versión.

| Archivo de la versión 1.2.2 | Uso |
|---|---|
| `PalClam_1.2.2_Setup.exe` | Instalador que selecciona x64 o x86 según Windows. |
| `PalClam_1.2.2_Setup_win-x64.exe` | Instalador específico de 64 bits. |
| `PalClam_1.2.2_Setup_win-x86.exe` | Instalador específico de 32 bits. |
| `PalClam_1.2.2_Portable_win-x64.zip` | Edición portable de 64 bits. |
| `PalClam_1.2.2_Portable_win-x86.zip` | Edición portable de 32 bits. |
| `PalClam_1.2.2_Source.zip` | Código fuente correspondiente e instrucciones de compilación. |

Los paquetes de la aplicación incluyen el runtime .NET y el motor de su arquitectura. No es necesario instalar el SDK ni las herramientas de desarrollo para utilizar los distribuibles.

#### Edición instalada

Descarga el instalador, ejecútalo y sigue sus indicaciones. El instalador automático es la opción recomendada si no necesitas elegir una arquitectura concreta. Los datos propios se guardan en el perfil de Windows.

#### Edición portable

Extrae **todo el ZIP** en una carpeta con permiso de escritura y ejecuta `PalClam.exe`. Los datos se guardan en la carpeta `Data` situada junto al programa. Conserva el archivo `palclam.portable`, que activa este modo.

La portable no registra servicios, tareas programadas, inicio automático ni integración con el Explorador. Para actualizarla, cierra PalClam y sustituye los archivos del programa conservando `Data`.

### Alcance de la protección

PalClam es una herramienta de seguridad complementaria. Guard y el monitor anti-ransomware son **reactivos**: observan cambios y generan análisis o avisos. No proporcionan bloqueo preventivo de escrituras ni descifrado de archivos afectados. Un aviso de comportamiento sospechoso se distingue de una detección del motor ClamAV.

El análisis de correo se realiza mediante los formatos y las integraciones admitidos. Outlook clásico requiere su integración local y Microsoft Graph necesita autorización del usuario. La cobertura depende de la configuración y del acceso a los mensajes seleccionados.

PalClam AI es opcional. Los proveedores remotos necesitan su configuración y credenciales correspondientes; el envío de texto requiere el consentimiento previsto en la aplicación. La asistencia de IA complementa la interpretación de los resultados.

### Idiomas

Español, inglés, francés, alemán, portugués e italiano. La aplicación selecciona inicialmente el idioma según Windows y permite cambiarlo en las opciones.

### Compilar desde las fuentes

La compilación se realiza en Windows. Se necesitan:

- SDK .NET 10.
- Windows PowerShell 5.1 o posterior.
- Inno Setup 6.3 o posterior para generar los instaladores.
- Conexión a Internet para la primera preparación de dependencias, motor y firmas.

Extrae el ZIP público de fuentes, abre PowerShell en su carpeta raíz y ejecuta:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File .\Tools\ConstruirDistribucion.ps1 -Mode All
```

El proceso ejecuta las comprobaciones, compila ambas arquitecturas, prepara el motor y las firmas y genera los instaladores y portables. Las salidas quedan en `dist/installer` y `dist/portable`. Un error detiene la construcción.

Las fuentes públicas contienen los scripts PowerShell necesarios para reconstruir el proyecto. Encontrarás las instrucciones completas en `BUILD_FROM_SOURCE.md`, incluido en el ZIP de fuentes. No se necesitan claves privadas del autor para compilar y ejecutar una copia.

### Licencia y componentes de terceros

Copyright © 2026 **Óscar de la Cuesta Campillo**.

El código propio de PalClam se distribuye bajo la **GNU General Public License, versión 3 o posterior** (`GPL-3.0-or-later`). Los textos y avisos se incluyen en `COPYING`, `LICENSE-GPL-3.0.txt` y `COPYRIGHT.md`.

ClamAV se utiliza como programa externo y conserva su licencia GPLv2. Las bibliotecas y demás componentes mantienen sus licencias y atribuciones originales, detalladas en `THIRD-PARTY-NOTICES.md` y en los archivos de licencias incluidos con las distribuciones.

Al redistribuir los paquetes, conserva sus carpetas `Licenses` y `Source` y los avisos correspondientes. Las fuentes publicadas deben corresponder a la versión de los binarios distribuidos.

PalClam es un proyecto independiente. Las marcas de terceros pertenecen a sus respectivos titulares.

### Colaborar

Las sugerencias, traducciones e informes de errores son bienvenidos en [Issues](https://github.com/oscardelacuesta/PalClam/issues). Para describir un fallo, indica la versión de PalClam, la versión de Windows, los pasos para reproducirlo y el mensaje de error.

### Autor

Desarrollado por **Óscar de la Cuesta Campillo**.

- Blog: [palentino.es](https://www.palentino.es/).
- Web del producto prevista: [palclam.com](https://palclam.com/) — en preparación.
