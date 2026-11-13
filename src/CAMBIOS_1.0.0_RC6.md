# PalClam 1.0.0 RC6 — Scan Engine PRO

- Contadores de rendimiento medidos en cada análisis: archivos/s, MiB lógicos/s, CPU y RAM del proceso clamscan.
- CPU de proceso normalizada al total de procesadores lógicos del equipo, con muestreo de 750 ms.
- Intervalo sin respuestas: muestra avisos sin asumir que ClamAV está bloqueado.
- Tres prioridades de proceso configurables (Económico, Equilibrado y Prioritario). No se ejecutan motores adicionales.
- Informe e historial con CPU media y pico, RAM pico, volumen lógico y silencio máximo.
- Ocho pruebas adicionales para cálculo de tasas, CPU, estado de actividad y archivos locales/red.
- Se mantienen instalación offline, motor ClamAV, firmas, actualizaciones, Guard, Mail, Outlook y cuarentena.

**Límites:** El nombre del archivo presentado sigue siendo el último resultado **terminado**, no el archivo que se examina exactamente en ese instante. Las MiB/s estimadas se basan en tamaños de archivos locales que ClamAV informa al concluir, no en lectura física. La opción Prioritario cambia prioridad de un único proceso y no implementa multihilo. Para concurrencia real y mayor precisión se estudiará clamd tras validar esta versión en Windows.

No hay telemetría en la nube ni transferencia de rutas a terceros. Los datos de rendimiento son exclusivamente locales. RC6 NO es aún estable ni debe publicarse en el canal estable.
