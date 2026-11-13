# PalClam 1.1.0 Preview 7 · Integración y fiabilidad

Basada en Preview 6, que ya había superado las comprobaciones del usuario en Windows.

## Cambios funcionales

- **Firmas:** diagnóstico local explícito de bases main/daily, fecha indicativa de `daily`, último intento fallido y última consulta satisfactoria de FreshClam. Los avisos nunca afirman que las firmas locales son idénticas a las últimas publicadas.
- **Intervalos:** el actualizador recuerda el último intento (acertado o fallido), para evitar que un fallo de Internet provoque comprobaciones reiteradas sin respetar el intervalo. El botón manual sigue disponible.
- **Explorador:** un análisis solicitado mediante `--scan` se abre directamente en Análisis y comienza sin esperar comprobaciones de Internet, siempre que ClamAV y firmas ya estén operativos. Diagnóstico del menú contextual por usuario y aviso sobre entradas obsoletas tras una actualización.
- **Correo:** listado con filtros de detecciones, contenido no examinado y advertencias heurísticas; exportación TXT agrupada por gravedad, sin presentar enlaces sospechosos como virus confirmados.
- **Privacidad:** ninguna detección provoca aislamiento ni eliminación sin confirmación; no se abren enlaces de los correos ni se ejecutan adjuntos.

## Limitaciones

- La antigüedad indicada se estima a partir de la fecha local del archivo daily: no es un sello del servidor de ClamAV ni garantiza vigencia.
- La integración con el Explorador es el menú contextual clásico de Windows. En Windows 11 puede estar en «Mostrar más opciones».
- El análisis offline requiere motor y firmas ya preparados; cuando el usuario lo inicia desde el Explorador, no se descargan firmas automáticamente antes de empezar.
- Guard utiliza vigilancia de cambios, no bloqueo preventivo de ejecución. No incorpora minifiltro de Windows ni controladores.
- La pausa/reanudación real de `clamscan` sigue pendiente. Se mantienen Detener y Reintentar.
- PalClam no sustituye Microsoft Defender; es complementario.

## Validación

Se añaden 7 pruebas automatizadas sin conexión sobre salud de firmas y clasificación de correo (total previsto: 81). Se mantiene la validación de motor/firmas offline e instalador, y el bloqueo de publicación automática de Preview.
La compilación WPF y la ejecución del instalador requieren Windows; no se consideran verificadas por las pruebas estáticas.
