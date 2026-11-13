# PalClam

**Antivirus gratuito y de código abierto para Windows, con motor ClamAV.**

PalClam ofrece una interfaz moderna para analizar archivos y correos, gestionar la cuarentena y observar cambios en carpetas. Incorpora un monitor anti-ransomware reactivo, asistencia opcional con IA y un indicador de actividad del disco configurable.

**Versión:** 1.2.2 · **Arquitecturas:** x64 y x86 · **Distribución:** instalador y portable · **Licencia propia:** GPLv3 o posterior.

[Descargas y versiones](https://github.com/oscardelacuesta/PalClam/releases) · [Comunicar una incidencia](https://github.com/oscardelacuesta/PalClam/issues) · [Blog del autor](https://www.palentino.es/)

## Funciones principales

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

## Descargas

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

### Edición instalada

Descarga el instalador, ejecútalo y sigue sus indicaciones. El instalador automático es la opción recomendada si no necesitas elegir una arquitectura concreta. Los datos propios se guardan en el perfil de Windows.

### Edición portable

Extrae **todo el ZIP** en una carpeta con permiso de escritura y ejecuta `PalClam.exe`. Los datos se guardan en la carpeta `Data` situada junto al programa. Conserva el archivo `palclam.portable`, que activa este modo.

La portable no registra servicios, tareas programadas, inicio automático ni integración con el Explorador. Para actualizarla, cierra PalClam y sustituye los archivos del programa conservando `Data`.

## Alcance de la protección

PalClam es una herramienta de seguridad complementaria. Guard y el monitor anti-ransomware son **reactivos**: observan cambios y generan análisis o avisos. No proporcionan bloqueo preventivo de escrituras ni descifrado de archivos afectados. Un aviso de comportamiento sospechoso se distingue de una detección del motor ClamAV.

El análisis de correo se realiza mediante los formatos y las integraciones admitidos. Outlook clásico requiere su integración local y Microsoft Graph necesita autorización del usuario. La cobertura depende de la configuración y del acceso a los mensajes seleccionados.

PalClam AI es opcional. Los proveedores remotos necesitan su configuración y credenciales correspondientes; el envío de texto requiere el consentimiento previsto en la aplicación. La asistencia de IA complementa la interpretación de los resultados.

## Idiomas

Español, inglés, francés, alemán, portugués e italiano. La aplicación selecciona inicialmente el idioma según Windows y permite cambiarlo en las opciones.

## Compilar desde las fuentes

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

## Licencia y componentes de terceros

Copyright © 2026 **Óscar de la Cuesta Campillo**.

El código propio de PalClam se distribuye bajo la **GNU General Public License, versión 3 o posterior** (`GPL-3.0-or-later`). Los textos y avisos se incluyen en `COPYING`, `LICENSE-GPL-3.0.txt` y `COPYRIGHT.md`.

ClamAV se utiliza como programa externo y conserva su licencia GPLv2. Las bibliotecas y demás componentes mantienen sus licencias y atribuciones originales, detalladas en `THIRD-PARTY-NOTICES.md` y en los archivos de licencias incluidos con las distribuciones.

Al redistribuir los paquetes, conserva sus carpetas `Licenses` y `Source` y los avisos correspondientes. Las fuentes publicadas deben corresponder a la versión de los binarios distribuidos.

PalClam es un proyecto independiente. Las marcas de terceros pertenecen a sus respectivos titulares.

## Colaborar

Las sugerencias, traducciones e informes de errores son bienvenidos en [Issues](https://github.com/oscardelacuesta/PalClam/issues). Para describir un fallo, indica la versión de PalClam, la versión de Windows, los pasos para reproducirlo y el mensaje de error.

## Autor

Desarrollado por **Óscar de la Cuesta Campillo**.

- Blog: [palentino.es](https://www.palentino.es/).
- Web del producto prevista: [palclam.com](https://palclam.com/) — en preparación.
