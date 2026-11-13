# Actualizaciones de PalClam 0.8.0

Hay **tres actualizaciones distintas**:

1. **PalClam (aplicación)**: usa instalador Inno Setup descargado desde `https://www.palentino.es/palclam/updates/` y manifiesto firmado RSA. Debes ejecutar `PUBLICAR_ACTUALIZACION.bat` y subir sus tres archivos al servidor. La clave privada se guarda en `%LOCALAPPDATA%\PalClam-Publisher` y no debe compartirse. Hasta que publiques el canal, las comprobaciones no encontrarán nuevas versiones.
2. **Motor ClamAV**: descargado desde la propia aplicación utilizando el ZIP portable de la publicación oficial estable GitHub `Cisco-Talos/clamav`; NO usa el instalador de PalClam ni BAT adicionales. Más detalles en `ACTUALIZADOR_MOTOR.md`.
3. **Firmas**: actualizadas por `freshclam` desde su red oficial.

Las comprobaciones de motor/firmas requieren conexión durante la descarga. No declarar que la protección en tiempo real está disponible: no se implementa.
