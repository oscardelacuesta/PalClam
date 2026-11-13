# PalClam 1.1.0 Preview 5 FIX1 — Corrección de compilación

Corrección puntual, **sin cambiar la versión funcional** `1.1.0-preview.5` ni el identificador de instalación Inno Setup.

## Errores resueltos

- `CS0104` en `HistoryWindow.cs` (4 apariciones) y `PerformanceWindow.cs` (8): se utiliza explícitamente `System.Windows.Media.ColorConverter`, evitando la ambigüedad con `System.Drawing.ColorConverter`.
- `CS1912` en `PerformanceWindow.cs` (2 apariciones): eliminado el segundo inicializador `FontSize` de cada control.
- `CS0105` en `HistoryWindow.cs`: retirada una directiva `using PalClam.Services;` duplicada.
- `NU1510` en `PalClam.csproj`: retirada la referencia redundante a `System.Security.Cryptography.ProtectedData`, disponible en el destino .NET 10 de Windows.
- Corrección cosmética de los encabezados y títulos de cinco BAT que seguían anunciando Preview 3.

## Aplicación del parche

Descomprime el ZIP del parche en la **carpeta raíz del proyecto Preview 5** y acepta sobrescribir archivos. No lo apliques en `Program Files` ni en la carpeta del instalador. Se mantienen intactas las firmas de ClamAV, las configuraciones de IA y todas las demás funciones.

Después ejecuta `EMPEZAR_AQUI.bat`. También puedes ejecutar primero `PRUEBAS.bat` para confirmar el resultado de las 74 pruebas y la compilación.

## Validación pendiente

Los cambios y los ZIP se han verificado estáticamente. No se ha podido ejecutar el compilador Windows/WPF ni Inno Setup en el entorno de creación. No distribuir la versión hasta superar la compilación, validación offline y las pruebas funcionales en Windows.
