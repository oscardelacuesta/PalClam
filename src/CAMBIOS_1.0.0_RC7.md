# PalClam 1.0.0 RC7 — Rendimiento histórico y comparación

- Panel de tendencias en tiempo real (archivos/s, CPU ClamAV, RAM ClamAV), usando muestras reales, sin inventar valores.
- Ventana WPF de rendimiento con gráficos históricos y comparación de dos análisis del mismo destino.
- Impide comparar resultados cancelados, con errores, firmas o motor diferente, distinto número de archivos o configuración distinta.
- Exportación CSV local con advertencia de privacidad de rutas.
- Mejoras en historial con motor, identidad de firmas por metadatos, perfil y tasas medias.
- Ampliación de smoke tests; compilación e instalador siguen por BAT con .NET 10.
- `clamd` sigue en evaluación: no se activa un daemon TCP sin autenticación por defecto.

No se ha validado la compilación WPF ni el instalador en Windows desde este entorno.
