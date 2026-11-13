# PalClam 1.1.0 RC1 Cierre visual — FIX1

## Problema detectado en Windows

`Tools/VerificarFuentes.ps1` bloquea la fase [1/5] por una referencia WPF sin calificar en `PalClam/UI/ModernScrollBarTheme.cs`.

## Corrección

Se reemplaza `ColorConverter.ConvertFromString` por `System.Windows.Media.ColorConverter.ConvertFromString` en `ModernScrollBarTheme.Put`. No se modifica la función, sus pinceles, los temas ni las plantillas XAML. El verificador queda intacto.

## Aplicación

- **Completo:** extrae el ZIP completo en una carpeta nueva.
- **Parche:** copia los archivos contenidos en el ZIP de parche encima del proyecto RC1 CIERRE VISUAL original, respetando las rutas.
- **Manual:** reemplaza la línea del método `Put` en `PalClam/UI/ModernScrollBarTheme.cs` por:

```csharp
var color = (Color)System.Windows.Media.ColorConverter.ConvertFromString(value)!;
```

Ejecuta `PRUEBAS.bat` y después el proceso habitual del instalador.

## Validación

Se han hecho comprobaciones estáticas del patrón del verificador, lectura XML de la plantilla XAML, integridad de ZIP y equivalencia del parche. La compilación y las pruebas de ejecución de WPF en Windows quedan pendientes.
