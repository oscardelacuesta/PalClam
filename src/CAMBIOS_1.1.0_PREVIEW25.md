# PalClam 1.1.0 Preview 25 — consolidación y estabilidad

Sobre Preview 24. **No añade nuevas promesas de protección.**

## Correcciones funcionales

- Machine Guard revalida cada ~10 s la presencia del motor y de firmas compartidas:
  si desaparecen, emite `Error` (no `Vigilando`) y registra la incidencia una vez por transición.
  Si regresan, el historial de fallos continúa visible; no se restablece un estado
  saludable ocultando incidentes.
- Si el motor o las firmas faltan durante un análisis, se omite el archivo con
  registro explícito y contador de omitidos; no se da por analizado.
- Los mensajes recibidos de ClamAV se recortan en el registro a 2.000 caracteres
  para reducir crecimiento excesivo de incidencias.
- El lector WPF de Machine Guard solo considera saludable el estado con cola
  sin pendientes, además de latido y contadores sin errores; rechaza contadores
  negativos o mal formados en el JSON de estado. Incluye regresiones automatizadas.
- Tras Start/Restart e instalación, el script elevado comprueba un estado Running
  confirmado por el SCM; un retorno 0 de `sc.exe` no basta.
- RefreshSignatures respeta el estado detenido del servicio. Una actualización
  online que no consigue recuperar el servicio informa un error y no lo silencia.

## Consolidación de paquete

- Versiones WPF y servicio actualizadas coherentemente a `1.1.0-preview.25`.
- Revisión de BAT, Inno Setup, verificadores de fuentes y del paquete.
- Añadidos controles de consistencia de arranque y diagnósticos del servicio.
- Se mantiene el AppId del instalador y se bloquea `PUBLICAR_ACTUALIZACION.bat`
  para esta Preview.

## Límite de validación

Se ha verificado la consistencia del árbol de fuentes y ZIP/parche en el entorno
actual. No se ha ejecutado `dotnet build` ni Windows SCM/ClamAV/Inno Setup aquí.
Requiere pruebas reales en Windows antes de uso de producción o candidata RC1.
