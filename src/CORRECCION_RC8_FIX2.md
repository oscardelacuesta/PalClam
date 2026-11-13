# PalClam 1.0.0 RC8 FIX2 — Compilación WPF/WinForms

## Motivo

En RC8 FIX1, el usuario ha confirmado que las 47 pruebas automáticas pasan, pero
`dotnet build` detecta 8 errores `CS0104` por tipos compartidos entre WPF,
Windows Forms y System.IO.

## Corrección precisa

- `PalClam/PerformanceWindow.cs`: 2 referencias a `System.IO.Path`,
  1 a `System.Windows.Controls.DataGrid`, 1 a `System.Windows.Point`.
- `PalClam/MainWindow.cs`: 3 referencias a `System.IO.Path`
  y 1 a `System.Windows.Point`.

No se han cambiado algoritmos de análisis, firmas, cuarentena, ajustes,
indicadores de rendimiento, paquetes NuGet ni scripts BAT.
La versión funcional sigue siendo `1.0.0-rc.8` (FIX2 es un parche de código, no un salto de versión).

## Aplicar

### Parche mínimo
Descomprimir el ZIP del parche en la raíz de la carpeta `PalClam_1.0.0_RC8_FIX1_...`,
sustituyendo los dos archivos indicados en `PalClam/`.

### Proyecto completo
Descomprimir el proyecto completo en una carpeta nueva.

Ejecutar `EMPEZAR_AQUI.bat` o primero `PRUEBAS.bat`. Si supera la
compilación, continuar con el instalador y las verificaciones de uso real.

**Validación disponible**: comparación estática de los archivos modificados,
conteo de las 8 sustituciones, integridad ZIP y conservación del resto
del proyecto. Este entorno no cuenta con .NET SDK de Windows y no ha
ejecutado el compilador WPF; no afirmar que compila sin realizar la prueba.
