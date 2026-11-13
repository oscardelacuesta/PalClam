# PalClam 1.2.2 — migración a GNU GPLv3 o posterior

Preparación: 12 de noviembre de 2026. Licencia propia: **GPL-3.0-or-later**. La versión del producto es 1.2.2; GNU GPL es versión 3 o posterior. Base exacta: PalClam_1.2.1_COMPLETO.zip, SHA256 `a48b123c5b22e65a65f3b22634f8cc4ad72fd86cc7553ab693095f86a52c0d2e`.

## Alcance y separación de componentes

El autor solicita pasar su código propio a GPLv3 o posterior. Se actualizan interfaz, ayuda, seis idiomas, avisos, scripts, servicio y documentos vigentes. Los archivos de implementación propios incluyen declaraciones SPDX. Los textos completos oficiales se conservan sin editar en COPYING y LICENSE-GPL-3.0.txt. Su SHA256 es `3972dc9744f6499f0f9b2dbf76696f2ae7ad8af9b23dde66d6af86c9dfb36986`.

Directory.Build.props aplica PackageLicenseExpression, metadatos de ensamblado y el texto íntegro GPLv3 como recurso embebido a los proyectos propios. La publicación también copia COPYING y LICENSE-GPL-3.0.txt junto al programa. El diálogo Inno LicenseFile presenta GPLv3 y la aplicación abre ese mismo texto en Acerca de. La ayuda explica GPLv3 o posterior en los seis idiomas. Se regeneran siete claves canónicas de recursos afectadas por licencia o versión; el número de claves se conserva.

ClamAV 1.5.4 sigue siendo un programa independiente bajo GPLv2, ejecutado por procesos o su protocolo externo. No hay enlace directo a libclamav en el código propio. No se cambia la licencia del motor ni sus paquetes oficiales. OpenMcdf mantiene MPL-2.0 con distribución adicional GPLv3 mediante la sección 3.3; UTF.Unknown permite elegir GPLv3 a partir de su alternativa original GPLv2-o-posterior. Se actualizan las decisiones y avisos del distribuidor, conservando todos los textos, metadatos y archivos fuente upstream sin modificaciones. Se mantienen las otras nueve licencias de bibliotecas y los avisos de .NET/Inno.

El archivo GPLv2 raíz se conserva para referencias a terceros e historia; no es la licencia primaria del código propio 1.2.2. Los originales y permisos de las versiones publicadas antes no se revocan. El alcance explícito está en COPYRIGHT.md. Las fuentes y paquetes no incluyen claves privadas, contraseñas ni datos personales.

## Construcción y comprobaciones ejecutadas

Entorno Linux, SDK .NET 10.0.404 y PowerShell 7.6.5, ambos descargados desde sus orígenes oficiales y verificados mediante hashes. Logs propios de esta entrega en docs/validacion-1.2.2; los de entregas anteriores se mantienen como evidencia histórica.

| Comprobación | Resultado observado |
|---|---|
| Localización .NET | 49.358 comprobaciones superadas; 2.064 claves × 6 idiomas = 12.384 textos. |
| Funciones correo/monitor | 55 comprobaciones superadas con archivos sintéticos y eventos reales; sin Outlook ni malware. |
| Arquitectura/almacenamiento | 22 comprobaciones superadas. |
| Empaquetado y licencias | 47 comprobaciones superadas, incluidas ambas portables, GPLv3 íntegra, COPYING/props en fuentes, fuentes de bibliotecas, exclusión de datos personales y rechazo de una licencia propia sustituida por GPLv2. Fixtures sintéticos en carpetas temporales, eliminados. |
| Aplicación x64 y x86 | Publicación autocontenida correcta para win-x64 y win-x86; PE con arquitectura correcta. |
| Servicio x64 y x86 | Publicación autocontenida correcta; PE con arquitectura correcta. |
| Licencia dentro de los binarios | Los cuatro programas publicados contienen GPL-3.0-or-later, el recurso PalClam.License.GPL3 y el texto GPLv3 oficial íntegro. En la aplicación el código y recurso están en PalClam.dll; en el servicio se incluyen en su EXE autocontenido. |
| Licencias junto al programa | COPYING y LICENSE-GPL-3.0.txt publicados, con SHA256 oficial correcto para ambas arquitecturas. |
| ThemeTests WPF | Compilación cruzada correcta, 0 errores y 0 advertencias; no se ejecuta interfaz Windows en Linux. |
| Auditoría GPLv3 | Comprobadas 125 fuentes C#, los seis recursos, metadatos, instalación y decisiones de licencia de bibliotecas. |
| Inventario de dependencias | Once paquetes reales coinciden con la revisión. Se recopilan avisos de los runtimes x64/x86 usados. Ningún texto original de terceros ni metadato NuGet se altera respecto al completo 1.2.1. |
| Fuentes upstream | SHA256 de los ZIP OpenMcdf y UTF.Unknown conservado. Se mantienen versiones y fuentes exactas. |
| Scripts | Análisis sintáctico de PowerShell correcto y verificador completo de fuentes superado. |
| Fuentes y parche | Archivos, hashes y manifiesto recalculados. El parche se contrasta byte a byte con la base para reconstruir exactamente el completo, sin borrados. |

Las pruebas de empaquetado utilizan PE, firmas y datos expresamente sintéticos para comprobar la estructura, privacidad y propagación de fallos. Ningún motor o firma simulado se añade a la entrega. Los paquetes finales aquí ofrecidos contienen fuentes y documentación para compilar con el BAT, no instaladores ya ejecutados o certificados.

## Al ejecutar EMPEZAR_AQUI.bat

Se conservan la preparación del motor y firmas, sus comprobaciones de seguridad, las cinco salidas, AppId, configuración de la edición instalada, Data de la portable y canales de actualización firmados. La verificación GPLv3 se ejecuta antes de construir y sobre las publicaciones, y vuelve a formar parte de la validación de distribución. Si falta el texto oficial, los metadatos o el recurso GPLv3, se detiene la entrega.

Salidas esperadas:

- dist/installer/PalClam_1.2.2_Setup.exe
- dist/installer/PalClam_1.2.2_Setup_win-x64.exe
- dist/installer/PalClam_1.2.2_Setup_win-x86.exe
- dist/portable/PalClam_1.2.2_Portable_win-x64.zip
- dist/portable/PalClam_1.2.2_Portable_win-x86.zip

Cada salida incorpora sus avisos y fuentes correspondientes mediante las carpetas Licenses y Source. El motor GPLv2 se distribuye con su fuente oficial verificada al preparar en Windows. No se cambia la arquitectura para evitar sus obligaciones de licencia, ni se atribuye al autor de PalClam la propiedad de componentes externos.

## Pendientes de Windows

No se ha ejecutado ISCC ni la interfaz WPF, ClamAV/FreshClam/SigTool, Outlook o PDH nativos. La generación real de los cinco distribuibles debe comprobarse en Windows con SDK .NET 10 e Inno Setup 6.3 o posterior. PRUEBAS.bat conserva las suites completas SmokeTests, ThemeTests y el selftest del servicio para Windows; no se declaran superadas aquí. El ensayo USB de rutas Windows que limitaba SmokeTests en Linux ya estaba documentado en 1.2.1; no se ha alterado para ocultarlo.

Todas las casillas de ACEPTACION_1.2.2.ini empiezan PENDIENTE, incluidas las dos nuevas sobre fuentes/referencias GPLv3 y binarios/distribuibles. La publicación automática sigue bloqueada. Estos resultados documentan lo observado, sin certificar instalación, seguridad antivirus o conformidad jurídica absoluta.

## Orígenes de licencia consultados

- GNU GPLv3, texto oficial: https://www.gnu.org/licenses/gpl-3.0.txt
- Aplicación de GNU GPL: https://www.gnu.org/licenses/gpl-howto.html
- Programas independientes y agregados: https://www.gnu.org/licenses/gpl-faq.html#MereAggregation
- Compatibilidad entre versiones GPL: https://www.gnu.org/licenses/gpl-faq.html#AllCompatibility
- Combinación MPL/GPL: https://www.mozilla.org/en-US/MPL/2.0/combining-mpl-and-gpl/
- UTF.Unknown, alternativas originales: README del commit 4654fbf0cdb47705e74a2c74bae20d486eb23fa8 incluido en Source/Libraries.
