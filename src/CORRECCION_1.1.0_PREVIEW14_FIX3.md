# PalClam 1.1.0 Preview 14 FIX3 — Compilación WPF

Esta revisión se aplica sobre **Preview 14 FIX2**.

## Incidencia

El usuario informó de dos errores `CS0104` en `MainWindow.HeaderAurora.cs` (líneas 83 y 84): `Point` es ambiguo entre `System.Drawing.Point` y `System.Windows.Point`.

## Correcciones

- `PalClam/MainWindow.HeaderAurora.cs`: los dos puntos del degradado de la aurora ahora se crean con `new System.Windows.Point(...)`. No cambia la apariencia ni el comportamiento del efecto.
- `Tools/VerificarFuentes.ps1`: inspección preventiva de instancias `new Point(...)` sin cualificar en fuentes de PalClam; permite identificar la ambigüedad antes de la etapa de compilación.

Se conserva el resto de Preview 14 FIX2: panel adaptable, aurora animada, Guard 2.0, arranque personal y global, Inspector y opciones.

## Cómo verificar

1. Abrir el proyecto completo FIX3 en una carpeta nueva, o copiar el parche sobre Preview 14 FIX2.
2. Ejecutar `PRUEBAS.bat` en Windows con el SDK de .NET 10.
3. Confirmar que las 118 pruebas, el test de 360 temas y la compilación WPF terminan correctamente.
4. Probar la aurora y el cambio de tema durante un escaneo; generar el instalador solo si pasan todas las comprobaciones.

**Estado:** revisión estática y consistencia de archivos comprobadas. La compilación y ejecución de WPF en Windows no se han ejecutado en el entorno de preparación.
