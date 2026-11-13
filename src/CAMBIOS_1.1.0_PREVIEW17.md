# PalClam 1.1.0 Preview 17 — Control del servicio y notificaciones Guard

Continúa **Preview 16**. Mantiene la interfaz WPF, Inspector, ClamAV, seis temas,
arranque multiusuario, aurora, Guard 2.0, Machine Guard experimental y las pruebas existentes.

## Administración del servicio (solo instalación compartida)

- Acciones **Iniciar**, **Detener** y **Reiniciar** con confirmación y autorización UAC.
- No se reinstala el servicio para cambiar su estado.
- El script comprueba la identidad y la ruta del servicio antes de modificarlo.
- Solo hay una operación administrativa pendiente desde la interfaz cada vez.
- Se mantiene el uso de `LocalService`, no `LocalSystem`. No se añade un canal IPC privilegiado.

## Notificaciones del Guard personal

- Avisos de amenazas por archivo, suprimiendo duplicados cercanos (2 minutos).
- Advertencias (incluidos archivos omitidos) y errores limitados (5 minutos por categoría).
- **Todos** los hallazgos se guardan en el historial y el registro, incluso sin aviso.
- Las notificaciones muestran el nombre del archivo, no la ruta completa.

## Notificaciones de Machine Guard

- Al abrir la página se toma una referencia de contadores, sin anunciar eventos antiguos.
- Un sondeo de solo lectura cada 10 segundos continúa mientras WPF está abierta, incluso en bandeja.
- Durante el sondeo, incrementos de detecciones, errores o pérdida de eventos pueden generar avisos.
- **Sin WPF en ejecución no existen notificaciones de escritorio de PalClam**:
  el servicio registra los sucesos en ProgramData; no abre ventanas.
- Conserva los estados degradados y la distinción entre detección y protección preventiva.

## Pruebas y límites

- SmokeTests incluyen los límites por archivo/categoría.
- `PRUEBAS.bat` compila por separado UI WPF y servicio. Después hay que comprobar
  UAC, instalación/desinstalación, notificaciones y rendimiento en Windows.
- No se garantiza bloqueo de ejecución ni cobertura total durante pérdida de eventos.
