# PalClam 1.1.0 Preview 14 — Guard 2.0

## Novedades

- La pausa detiene el **procesamiento** de nuevos archivos, pero conserva los observadores de Windows activos. Un análisis ya iniciado puede terminar.
- Cola limitada y coalescencia de ráfagas; perfiles Eficiente, Equilibrado y Rápido con distinta ventana de estabilización y frecuencia.
- Indicadores de pendientes, agrupados y eventos perdidos en la pantalla Guard.
- Al desbordarse la cola o informar Windows de pérdida de eventos, Guard queda **degradado** y recomienda análisis manual. No se anuncia cobertura completa.
- Se mantienen ClamAV, Inspector, IA, correo, cuarentena, temas y herramientas de compilación.
- Cinco pruebas de política de rendimiento incorporadas a SmokeTests.

## Límites importantes

Guard sigue siendo **vigilancia reactiva con FileSystemWatcher**, no prevención de acceso ni bloqueo de ejecución. Con PalClam cerrado no vigila. La pausa, los fallos, un cierre o la cola llena pueden dejar cambios sin analizar. No se reconstruye automáticamente el conjunto de cambios mientras la aplicación estuvo cerrada. Para recuperar cobertura tras una interrupción, ejecuta un análisis manual de las carpetas. No se cuarentena automáticamente un archivo: requiere confirmación expresa.

## Validación

- Comprobaciones estructurales y de integridad del código disponibles en el ZIP.
- `PRUEBAS.bat` incluye SmokeTests, regresión de temas y compilación real Windows.
- **No se ha ejecutado la compilación Windows en el entorno que prepara el ZIP**; es necesaria la prueba local.

## Ajustes finales — Panel visual / Aurora / Windows

- Corregida la tarjeta de estado del motor para que «Repetir análisis» no se corte: 244 px y adaptación a modo apilado y texto 130 %.
- Ondas aurora suaves en la franja de marca (escudo, nombre y distintivo Preview), coloreadas según la paleta actual. No interceptan clics; se detienen al ocultar/minimizar.
- Interruptor de aurora en Opciones > Apariencia.
- Inicio con Windows opcional mediante HKCU/Run, lanzado con --startup-hidden y en bandeja. Sin permisos elevados ni servicio.
- Interruptor en Opciones > General y restauración de preferencias; diagnóstico si Windows rechaza el registro.
- Pruebas puras para el argumento y ruta entrecomillada, y comprobación previa de archivos añadidos.

## Preview 14 FIX1 — ajuste visual de cabecera

- Aumentado el ancho de la tarjeta **Estado del motor** para evitar el recorte de "Repetir análisis".
- Nueva aurora de cabecera con **cintas vectoriales animadas**, movimiento real y color dependiente del tema.
- Eliminado el efecto hover tipo botón en el bloque de marca **PalClam**.

