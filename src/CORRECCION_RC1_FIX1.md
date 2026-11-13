# PalClam 1.0.0 RC1 - FIX1 de compilación (.NET 10)

Corrección de los **87 errores del registro** recibido el 9 de noviembre de 2026.

## Causas

- **CS0104**: al tener `UseWPF=true` y `UseWindowsForms=true`, los nombres de controles y pinceles aparecen en ambos entornos. Se fijan explícitamente mediante aliases globales WPF en `PalClam/GlobalUsings.cs`. `NotifyIcon` sigue utilizando `Forms = System.Windows.Forms` dentro de `MainWindow.Tray.cs`.
- **CS0246**: en `Services/OutlookGraphClient.cs` faltaba la importación `System.Net.Http`, necesaria para `HttpClient` y `HttpResponseMessage`.

## Ejecución

1. Descomprimir este ZIP en una carpeta nueva (no mezclar con la versión anterior).
2. Ejecutar `PRUEBAS.bat` (valida lógica y compila WPF con .NET 10).
3. Si pasa, ejecutar `CREAR_INSTALADOR.bat` (descarga/integra los componentes offline y genera Inno Setup).

**Estado:** revisión y análisis estático completados. Pendiente de compilar en Windows; esta variante **no** ha superado todavía pruebas reales de WPF, de instalación o de Outlook.

Se mantiene la identidad de versión RC1, las claves de firma, los scripts, funciones y nombres de instalador. FIX1 distingue este paquete **de fuentes**, no cambia el canal de publicación. No publicar en el canal estable.
