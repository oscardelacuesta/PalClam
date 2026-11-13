# PalClam 0.6.0 — Icono y mejoras de uso diario

## Nuevo

- **Icono exclusivo** diseñado para PalClam: escudo violeta con verificación, sin símbolos protegidos de ClamAV. Se aplica al EXE, la ventana de WPF, accesos directos y el instalador Inno Setup. Recursos fuente en `PalClam/Assets`.
- **Arrastrar y soltar:** un archivo o carpeta desde el Explorador inicia un análisis (nunca elimina ni mueve archivos).
- **Menú contextual opcional:** desde Opciones, activar o desactivar «Analizar con PalClam» para archivos, carpetas y unidades (HKCU, sin administrador). En Windows 11 puede estar en «Mostrar más opciones». En portables, si se cambia la ubicación, volver a activarlo.
- **Nueva pestaña Diagnóstico:** muestra si existe el motor, su versión real, las bases de firmas presentes y la integración con Explorer. Botones para actualizar, copiar o exportar TXT.
- **Atajos:** Ctrl+O (elegir archivo); Ctrl+Mayús+O (elegir carpeta).
- **Actualización de versión** de 0.5.0 a 0.6.0 en ventana, ensamblado, BAT e instalador.

## Conservado

- .NET 10 WPF y construcción vía `COMPILAR.bat` / `CREAR_INSTALADOR.bat` sin Visual Studio.
- Instalador offline con motor ClamAV y firmas verificadas en el equipo de desarrollo.
- Actualizaciones propias de aplicación, motor y firmas desde PalClam.
- Análisis sin borrado automático, resultados, historial, log, opciones y métricas.

## Antes de publicar

El proyecto no se ha compilado en Windows en este entorno. Ejecutar `COMPILAR.bat` en Windows, probar el análisis y después `CREAR_INSTALADOR.bat`. Verificar legalmente la distribución de firmas y dependencias como en 0.5.0. La integración con el Explorador requiere probarla en Windows 10 y 11, incluida la desinstalación.

El icono SVG/PNG/ICO se ha elaborado para este proyecto; no contiene marcas oficiales de ClamAV ni Cisco.
