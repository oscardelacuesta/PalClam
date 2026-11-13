# PalClam 1.1.0 RC1 — Cierre visual FIX3 (Ayuda)

## Fallo corregido

`PRUEBAS.bat` superaba la comprobación de fuentes, pero las pruebas de humo se detenían en `ayuda: todos los accesos apuntan a secciones existentes`.

El catálogo `HelpContentCatalog` enlaza a `health` (Security Health 360) y `devices` (USB y Descargas), páginas existentes de la aplicación; la lista de páginas admitidas de las pruebas había quedado desactualizada.

## Corrección

- Actualizados los catálogos de páginas válidas en `PalClam.SmokeTests/Program.cs` para incluir `health`, `devices` y `alerts` (Centro de notificaciones).
- Se mantiene intacta la aserción que rechaza accesos a páginas inexistentes.
- Verificación estática cruzada de todos los destinos de Ayuda y Consejos con las páginas declaradas mediante `_pages.Add(...)` en `PalClam/MainWindow.cs`.
- No se alteran ClamAV, Machine Guard, el instalador, la aurora, los temas ni la interfaz visual.

## Comprobación en Windows

1. Descomprimir el paquete completo FIX3 en una carpeta nueva o sobrescribir los dos archivos del parche sobre FIX2.
2. Ejecutar `PRUEBAS.bat` y comprobar que la prueba `ayuda: todos los accesos apuntan a secciones existentes` pasa.
3. Revisar el resto de las pruebas y la compilación/integración con Inno Setup. No distribuir si alguna fase falla.

Esta entrega se ha verificado estáticamente en un entorno sin SDK de .NET/Windows; no se afirma que haya compilado.
