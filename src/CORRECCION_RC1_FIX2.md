# PalClam 1.0.0 RC1 FIX2 — Correcciones de compilación

**Problema recibido:** FIX1 pasa 12 pruebas lógicas, pero la fase WPF/Windows x64 falla con 116 errores (108 `CS0104`, 8 `CS0176`) y 1 advertencia `CS8602`.

## Correcciones aplicadas
- `PalClam/GlobalUsings.cs`: nuevos alias explícitos para los tipos WPF que entraban en conflicto con Windows Forms y `System.Drawing`: `Application`, `Binding`, `Brushes`, `Clipboard`, `Color`, `Cursors`, `DataFormats`, `DragDropEffects`, `FontFamily`, `MessageBox`, `OpenFileDialog`, `Orientation` y `SaveFileDialog`. Se mantienen los alias existentes para controles y eventos.
- `MainWindow.cs`, `MainWindow.Guard.cs`, `MainWindow.Outlook.cs`: 8 accesos `HorizontalAlignment.Left/Center` cualificados como `System.Windows.HorizontalAlignment`, para evitar que se interpreten como acceso a una propiedad de instancia (`CS0176`).
- `Services/MailScanService.cs`: si una parte MIME carece de `Content`, se marca como **no analizada** en el informe; ya no se desreferencia el contenido potencialmente nulo (`CS8602`).
- `Installer/PalClam.iss`, `CREAR_INSTALADOR.bat`, `GENERAR_SOLO_INSTALADOR.bat`, `INSTALAR.bat`: nombre del instalador diferenciado con sufijo `FIX2`, para no confundirlo con los paquetes anteriores.

## Instrucciones
1. Extraer este ZIP en **una carpeta nueva**. No superponerlo sobre FIX1.
2. Ejecutar `PRUEBAS.bat`. Debe completar las 12 pruebas de lógica y compilar el proyecto WPF.
3. Solo si se supera: ejecutar `CREAR_INSTALADOR.bat`, que prepara el motor offline y las firmas de respaldo.
4. Probar instalación, ejecución, cierre a bandeja, PalClam Guard, ClamAV, Outlook (con permisos) y desinstalación antes de distribuir.

## Alcance de la verificación
Se han revisado las 116 referencias del registro y se ha comprobado que están resueltas en el código fuente, incluida la advertencia CS8602. **No se ha podido compilar el proyecto WPF en Windows** aquí; si surge un nuevo error, conservar la salida íntegra de la consola. La versión sigue siendo **Release Candidate**.
