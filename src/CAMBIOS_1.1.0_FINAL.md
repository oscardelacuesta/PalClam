# PalClam 1.1.0 — cierre de código, pendiente de aceptación de distribución

Base: **RC1 / Cierre Visual FIX3** (compilación automática informada por el usuario como correcta). Fecha del paquete: 12/11/2026.

## Cierre realizado

- Metadatos de versión del cliente WPF, el servicio Windows independiente y el instalador alineados con **1.1.0**.
- Sin nuevos módulos antivirus ni cambios de comportamiento de ClamAV, cuarentena, IA, Guard o firmas.
- Identificador `AppId` de Inno Setup conservado para probar actualización desde la versión previa.
- Nombres de BAT, instalador y validadores coherentes con la versión final.
- Nueva puerta de aceptación manual `VALIDAR_ENTREGA_110.bat`: toda comprobación empieza pendiente; el canal de publicación automatizada continúa bloqueado.
- Interfaz mantenida: siete temas (incluido Oscuro violeta), aurora con velocidad configurable, barras de desplazamiento modernas, ayuda junto a ajustes y controles oscuros.
- Guía de migración a ChatGPT Work para **PalClam 1.2.0 multilenguaje**, sin modificaciones de recursos todavía.

## Qué NO se ha hecho ni se certifica

- Este ZIP contiene **fuentes** y utilidades, no un EXE ni un instalador firmado o compilado.
- No se han completado aquí pruebas de Windows, instalación limpia, desinstalación, cuarentena real, EICAR ni restauración; requieren aceptación con evidencias.
- Ninguna firma digital de código está incluida; la clave privada de actualizaciones tampoco debe incluirse.
- **No distribuir** a terceros ni subir al canal estable hasta que la matriz de aceptación figure conforme y se haya contrastado el instalador generado en Windows.
