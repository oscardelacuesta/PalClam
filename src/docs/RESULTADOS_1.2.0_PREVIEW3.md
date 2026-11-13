# Resultados de PalClam 1.2.0 Preview 3

12 de noviembre de 2026. Preparado sobre el ZIP completo 1.2.0 Preview 2. Entorno de validación: Linux, SDK .NET 10.0.404. Esta entrega contiene fuentes y scripts; no contiene un instalador ejecutado o aceptado en Windows.

## Implementado

- LED independiente de bandeja con ocho estados, colores y preferencias configurables en Opciones → General; lectura/escritura mediante PDH y espacio del volumen seleccionado. Estado de análisis conectado a archivos, correo y trabajo de Guard personal; alertas pendientes y sospechas de ransomware tienen prioridad.
- Anti-ransomware opcional en Protección: carpetas locales, exclusiones, arranque voluntario con PalClam, ventana temporal y umbrales. Correlación de archivos distintos, renombrados y muestras de alta aleatoriedad; avisos de actividad o eliminación masiva. Notificaciones, LED e historial local rotado. Los fallos de vigilancia y eventos perdidos no se presentan como cobertura correcta.
- Outlook clásico abierto con selector de carpeta y copias MSG Unicode de solo lectura; no hay parser directo PST/OST. Graph permite elegir cinco carpetas estándar. Importación MBOX y Maildir, límite de recursos, rutas temporales generadas, conservación de originales, asignación de resultados al origen y limpieza.
- 148 claves nuevas, 2.055 totales, 12.330 textos en seis idiomas. Se conserva el contenido de todas las 1.907 claves previas en cada idioma y la detección de Windows.
- Pkcs 10.0.14 explícito en aplicación y pruebas MIME. La aplicación con MsgReader ya resolvía esa versión; la referencia explícita evita que el nuevo proyecto de pruebas MIME resuelva 10.0.0, para la que NuGet notificó dos avisos de seguridad. No se rebaja ninguna dependencia ni se silencian las alertas.

## Ejecutado y resultado

| Comprobación | Resultado real |
|---|---|
| Compilación Release de WPF con targeting Windows | Correcta, 0 errores y 0 advertencias |
| Compilación Release del servicio | Correcta, 0 errores y 0 advertencias |
| Compilación de ThemeTests WPF | Correcta, 0 errores y 0 advertencias; sin ejecución visual |
| LocalizationTests, recursos reales | 49.142 comprobaciones superadas |
| FeatureTests | 55 comprobaciones superadas en Linux |
| Auditoría NuGet transitiva en restauración WPF | Sin avisos en esta ejecución, con NuGetAuditMode=all |
| Validación estática de localización y paquete | 2.055 claves × 6, formatos, enlaces de fuentes, versión, AppId, acta nueva y bloqueo correctos |
| Conservación de componentes sensibles | Servicio .cs, actualización firmada, clave pública y cuarentena conservados frente a Preview 2; Guard personal solo añade una propiedad de lectura de actividad |
| SmokeTests generales | Ejecución parcial; termina con error en una comprobación de raíces USB Windows en Linux, detallada abajo |

Los registros están en validacion-preview3/. No se sustituyen por marcas de aceptación manual.

FeatureTests verifica prioridad de estados del LED, ajustes corruptos, valores persistidos, falta de contadores, espacio y ámbito; eventos repetidos frente a archivos distintos, ventana y enfriamiento, advertencias frente a detecciones, límites de carpetas y entropía; MBOX/Maildir, separación de mensajes, líneas grandes, formatos no reconocidos, límites, enlaces, cancelación, limpieza, asignación de origen y hashes de originales. También recorre el monitor real del sistema de archivos con renombrados y datos sintéticos de alta entropía en .txt, sin malware ni cifrar archivos del usuario. El historial conserva la sospecha como advertencia, no como detección de ClamAV.

La ejecución observada en Linux no equivale a probar ReadDirectoryChangesW, PDH, Explorer, los avisos visuales de Windows o Outlook. El SDK se ejecutó secuencialmente y sin servidores de compilación. Esos parámetros temporales no alteran los BAT de Windows.

## SmokeTests y límites de plataforma

La batería general se detiene en `USB Watch: no analiza unidades previas al inicio`, PalClam.SmokeTests/Program.cs:207. La prueba utiliza raíces F:\ y G:\; Path las interpreta de otra forma en Linux. El mismo fallo está documentado en la base Preview 2, y RemovableDriveMonitor se conserva byte a byte. No se declara la batería completa superada ni se modifica esa lógica para forzar el resultado: debe ejecutarse en Windows.

## Pendiente de aceptación Windows

- Iconos reales y PDH en distintos idiomas de Windows, unidades y permisos; colores, persistencia, DPI, consumo y cierre.
- Outlook clásico, permisos COM, selector, MSG Unicode y distintos almacenes/proveedores; Graph con una cuenta de prueba autorizada. No se ha accedido a ninguna cuenta ni buzón real durante la preparación.
- ClamAV real, adjuntos, correos positivos de prueba, omisiones y cuarentena existentes; instalación, servicio Windows, selftest del servicio, Inno Setup y equipo limpio.
- Monitor bajo ráfagas y pérdida de eventos en Windows, falsos positivos, falsos negativos, logs y permisos; todos los idiomas, siete temas y escalados.
- Toda el acta ACEPTACION_1.2.0_PREVIEW3.ini permanece PENDIENTE. La publicación continúa bloqueada.

## Límites que conserva la interfaz

El anti-ransomware detecta indicios, puede alertar después de daños y tiene falsos positivos y falsos negativos. No bloquea, mata procesos, descifra, crea copias de seguridad ni vigila sectores/BitLocker. Solo cubre carpetas seleccionadas mientras PalClam está abierto. Las muestras son parciales y acotadas; no representan una inspección completa del disco.

Los correos originales no se cambian. El nuevo Outlook utiliza Graph; PST/OST se acceden solo a través de carpetas montadas en Outlook clásico. MBOX/Maildir son importaciones voluntarias y limitadas, no sincronización completa ni servicio continuo. Los errores, límites, cancelaciones y omisiones invalidan la conclusión de limpio. Outlook controla SaveAs y el tamaño real se valida al terminar; las copias aceptadas respetan la cuota de importación, pero esa escritura externa puede superar momentáneamente su estimación.

Uso y límites detallados: USO_1.2.0_PREVIEW3.md. Ensayos: PRUEBAS_1.2.0_PREVIEW3.md. El instalador se construye en Windows con los BAT incluidos; no se publica automáticamente.
