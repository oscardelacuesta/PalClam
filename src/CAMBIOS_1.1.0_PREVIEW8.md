# PalClam 1.1.0 Preview 8 — Scan Control PRO

**Candidata de pruebas. No publicación automática.** Basada en Preview 7.

## Funciones implementadas

- Cabecera de análisis destacada, controles **Pausar/Reanudar** y **Detener** siempre accesibles sin scroll.
- Barra fija de control disponible también desde otras páginas mientras se analiza.
- Tarjeta de seguimiento destacada en Inicio: estado, archivo/lote, resultados, incidencias, tiempo, progreso orientativo y acciones.
- Barra flotante opcional configurable desde Opciones; botón para mostrarla, ocultar sin cancelar y opción siempre visible.
- **Modo equilibrado:** 128 archivos por lote `clamscan --file-list` de ClamAV; la pausa aguarda el final del lote en curso. El nombre mostrado es el del **lote**, NO un archivo actual ficticio.
- **Modo preciso:** un archivo por ejecución de ClamAV, permite saber qué archivo se está enviando al motor y pausar entre archivos. Puede ser **notablemente más lento** por las recargas de firmas. No se recomienda para análisis de unidades enormes.
- Seguimiento explícito del **último archivo cuyo resultado ha sido confirmado por ClamAV**, separado del lote en proceso.
- Conserva contadores de los lotes, resúmenes, detecciones, errores, historial y cobertura incompleta.
- Errores al enumerar directorios y ejecuciones incompletas impiden marcar falsamente como limpio.
- Ventana flotante no envía datos a Internet; su opción se guarda por usuario.
- Se conservan Self-Defense, Guard, IA, correo, actualizaciones, temas, personaje, cuarentena, integración Explorer y BAT.

## Límites conscientes

- Pausa cooperativa **no congela inmediatamente** un archivo comprimido muy grande; espera a la terminación del lote. No se suspende ningún hilo interno de ClamAV.
- Reanudar funciona mientras la tarea sigue abierta. **No hay persistencia de checkpoints entre cierres** ni recuperación después de un reinicio del PC.
- El modo por lotes carga las firmas varias veces; el rendimiento puede ser inferior al de Preview 7. En el modo preciso el coste es mayor.
- El porcentaje compara resultados completados con una estimación; no mide el avance interno de un archivo ni los bytes físicos leídos.
- No existe bloqueo preventivo de malware; Guard permanece como vigilancia, no sustituye Defender.
- No elimina ni aísla archivos sin confirmación explícita.

## Notas de pruebas

La validación estática y el ZIP se comprueban aquí; **la compilación y el funcionamiento real en Windows siguen pendientes**. Ver `docs/PRUEBAS_1.1.0_PREVIEW8.md`.
