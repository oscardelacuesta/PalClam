# PalClam 1.1.0 Preview 12 — Progreso fiable y resultados claros

## Mejoras de interfaz
- Nuevo aviso contextual debajo del progreso: distingue análisis, pausa solicitada, pausa real y detención pendiente.
- Porcentaje exclusivamente *orientativo* frente a una enumeración previa: nunca supera el 99% antes de finalizar; se oculta si el recuento es truncado, tiene errores o deja de ser coherente con la salida de ClamAV.
- Estimación de tiempo restante condicionada a datos suficientes (12s, 25 respuestas, tasa sostenida) y limitada a referencias consistentes. Se retira al pausar, cancelar o al perder actividad.
- Panel de cierre con veredicto, detecciones, errores, resultados por archivo y recuento de `Scanned files` confirmado por ClamAV cuando existe.
- Botones con tres estados distintos: **Pausar**, **Anular pausa solicitada**, **Reanudar**. La pausa efectiva ocurre solo entre lotes; no se suspende el proceso ClamAV.
- Barra flotante y tarjeta del panel principal usan los mismos estados de pausa y el mismo porcentaje orientativo.

## Correcciones de consistencia
- Al reanudar, se retira inmediatamente el indicador de «lote en espera» obsoleto.
- Los eventos tardíos de un escaneo anterior se descartan mediante identificador de sesión.
- El seguimiento de velocidad olvida muestras anteriores a una pausa: no publica una ETA desactualizada.
- Se conserva ClamAV, la cuarentena, Guard, IA opcional, seis temas, opciones y el instalador; se mantiene el AppId anterior.

## Validación
- Nuevas pruebas unitarias en `PalClam.SmokeTests` (cobertura truncada, denominador inconsistente, ETA, pausa, detención y finalización).
- `PRUEBAS.bat` conserva pruebas WPF de 360 alternancias y compilación `win-x64`.
- **No ejecutado aquí:** .NET 10 / WPF en Windows, ClamAV real, instalador Inno Setup. No se declara corregido en producción hasta ejecutar las pruebas de aceptación.
