# PalClam 1.2.0 — resultados

12 de noviembre de 2026. Entorno de preparación: Linux, SDK .NET 10.0.404. Base exacta: PalClam_1.2.0_Preview3_COMPLETO.zip (SHA256 b66c63ed0681f5e5859c4574e088ea1d221a03dd3ba4c7a38f1368d9ee54462f).

## Identificación entregada

- Nombre y versión: PalClam 1.2.0, sin sufijo RC ni Preview.
- InformationalVersion de aplicación y servicio: 1.2.0. FileVersion generado: 1.2.0.0.
- Ventana, pie, Acerca de, inicio e informe de correo: 1.2.0.
- Instalador que generan los BAT: PalClam_1.2.0_Setup_win-x64.exe.
- ZIP completo de fuentes y parche sobre la base exacta Preview 3. No se entrega un instalador precompilado.

## Comprobado en esta entrega

| Comprobación | Resultado |
|---|---|
| Compilación Release WPF con targeting Windows | Correcta, 0 errores y 0 advertencias |
| Compilación Release del servicio | Correcta, 0 errores y 0 advertencias |
| LocalizationTests | 49.070 comprobaciones superadas |
| Catálogo multilenguaje | 2.052 claves × 6 idiomas = 12.312 textos |
| Etiquetas de producto | Eliminadas las etiquetas RC/Preview de la versión activa en interfaz, metadatos, recursos y nombres de salida |
| Versiones generadas por el SDK | Aplicación y servicio: 1.2.0 / 1.2.0.0 |
| Conservación del comportamiento | 66 archivos C# de servicios, 3 de infraestructura y 5 del servicio Windows idénticos a la base |
| Seguridad y compatibilidad | AppId, clave pública, actualizador firmado, motor, políticas de correo, LED y anti-ransomware conservados |
| Instalador y validadores | Referencias de fuentes presentes, nombres coherentes y acta nueva con 35 ensayos PENDIENTE |

Solo se consolidan tres etiquetas duplicadas de versión del catálogo: de las 2.055 claves previas se pasa a 2.052, conservando las traducciones revisadas de la base más reciente. No se elimina la traducción de funciones o estados. Los demás valores existentes se compararon y permanecen intactos.

Registros actuales: validacion-1.2.0/. El cambio de versión afecta a textos y metadatos, sin cambios en políticas de análisis ni formatos de configuración. Las 55 comprobaciones funcionales de correo/LED/anti-ransomware pasaron en la base Preview 3; el código que prueban permanece idéntico y sus registros originales se conservan en validacion-preview3/, identificados como anteriores. No se presentan como una nueva ejecución de esas pruebas.

## Pendiente de Windows

La identificación 1.2.0 no certifica ensayos que no se han realizado. Sigue pendiente la ejecución visual WPF, PDH y bandeja reales, Outlook clásico/Graph con buzón de prueba autorizado, ClamAV real, servicio Windows y su selftest, Inno Setup, instalación limpia, actualización y comprobación completa en equipo Windows.

La batería SmokeTests general de la base se detiene en Linux en la prueba USB de raíces F:\ y G:\ (Program.cs:207), que se interpretan de otra forma en esa plataforma. El código no se modifica para forzarla; no se declara superada toda la batería. Se debe ejecutar PRUEBAS.bat en Windows antes de aceptar la entrega. Los detalles del ensayo anterior siguen en docs/RESULTADOS_1.2.0_PREVIEW3.md.

El acta vigente es ACEPTACION_1.2.0.ini y no hereda conformidades de otras versiones. Todas sus marcas se mantienen PENDIENTE; los registros automáticos se documentan por separado. Publicación automática bloqueada. No se ha publicado ni instalado nada en el equipo del usuario.

## Funciones incluidas

Se mantienen la ampliación de correo de Outlook clásico y Microsoft Graph, EML/MSG/OFT, MBOX/Maildir, LED de disco configurable y monitor opcional anti-ransomware. También idiomas automáticos/manuales, siete temas, aurora, Guard, análisis programados y cuarentena reversible.

El anti-ransomware es un monitor reactivo de indicios: puede avisar después de daños y no garantiza detener el cifrado, no descifra ni sustituye copias de seguridad. Los correos requieren la selección/autorización del usuario. Para límites y uso consulta USO_1.2.0.md; para aceptación, PRUEBAS_1.2.0.md.
