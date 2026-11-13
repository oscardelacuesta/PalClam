# PalClam 1.1.0 Preview 13 FIX2 — Compilación WPF / Windows Forms

## Problema observado en Windows

`PRUEBAS.bat` ha superado 107 SmokeTests y la prueba WPF real de 360 alternancias,
pero la etapa `[4/4]` se ha detenido con 11 errores `CS0104`, `CS0176` y `CS0120`.

La causa es una ambigüedad de nombres entre Windows Forms, `System.Drawing` y WPF,
agravada por la propiedad de instancia `HorizontalAlignment` heredada de WPF `Window`.
La importación `using System.Windows;` no resuelve esa colisión dentro de `MainWindow`.

## Corrección concreta

- `PalClam/UI/OptionsTabAppearance.cs`: se califica el enum `System.Windows.HorizontalAlignment` y `System.Windows.Media.ColorConverter`.
- `PalClam/MainWindow.Dashboard.cs`: se califican seis usos del enum de alineación en la vista de inicio.
- `PalClam/MainWindow.Inspector.cs`: se califican dos usos del enum de alineación en el Inspector.
- `Tools/VerificarFuentes.ps1`: comprobación automática que rechaza nuevas referencias no calificadas antes del resto de pruebas.

**Se corrigen 10 referencias de código**: 9 alineaciones y 1 convertidor de color.
El compilador generó 11 diagnósticos porque uno de los usos produjo dos errores (`CS0120` y `CS0176`).

Se conserva la versión `1.1.0-preview.13`, el AppId del instalador, las funciones de Preview 13 FIX1,
los cambios visuales, el Inspector, la IA, Guard, ClamAV y las pruebas existentes.

## Verificación

- Inspección estática de los nombres sin calificar en todos los `.cs` del proyecto: superada.
- Comprobación de integridad de ambos ZIP: superada.
- Aplicación del parche sobre FIX1 y comparación exacta con el ZIP completo: superada.
- `PRUEBAS.bat`, compilación .NET 10/WPF e Inno Setup en Windows: **pendientes**.

La prueba del usuario de FIX1 confirma que las primeras tres etapas ya pasaron;
FIX2 requiere repetirlas y compilar en Windows, pues este entorno no tiene el SDK .NET 10.
