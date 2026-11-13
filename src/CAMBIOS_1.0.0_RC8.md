# PalClam 1.0.0 RC8 — Correcciones de cierre

1. **Validator RC7 → RC8:** subsanada la comprobación de versión antigua que impedía validar el paquete tras compilar.
2. **Comprobación previa de entrega:** `Tools/VerificarFuentes.ps1` asegura que el archivo de proyecto, el instalador y los BAT apuntan a RC8, conserva el mismo AppId para actualizaciones sobre RC7 y rechaza claves privadas dentro del árbol de fuentes.
3. **Rendimiento:** las comparaciones entre dos ejecuciones dejan de considerarse equivalentes si varían el resultado o las detecciones. Las gráficas históricas, como hasta ahora, siguen siendo descriptivas.
4. **Informes:** actualización de etiquetas de la versión en Guard, Mail y análisis.
5. **Pruebas:** tres escenarios de regresión más; `REVISAR_ENTREGA.bat` ejecuta el control del instalador offline sin realizar una instalación.

Esta RC mantiene el bloqueo del canal estable. Ninguna comprobación estática sustituye las pruebas reales en Windows, ni acredita por sí sola la eficacia antivirus o el cumplimiento legal del paquete.
