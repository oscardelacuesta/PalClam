# PalClam 1.1.0 Preview 13 FIX1 — Prueba de exclusiones

## Síntoma corregido

`PRUEBAS.bat` interrumpía el paso [2/4] con `System.InvalidOperationException: Prueba fallida: estimación respeta exclusiones`, en `PalClam.SmokeTests/Program.cs` (línea 358 de Preview 13 original).

## Causa

En Preview 13 se añadió la prueba del Inspector, que crea `ejemplo.txt` en la carpeta temporal `tmp`. Más adelante, la prueba de exclusiones creaba otros tres archivos bajo `tmp`, excluía uno y esperaba obtener **2** archivos. El contador devolvía legítimamente **3** (incluía `ejemplo.txt`), por lo que el fallo era del **aislamiento de la prueba**, no una prueba de funcionamiento incorrecto de ClamAV.

## Corrección

- La prueba de exclusiones trabaja ahora dentro de `tmp/estimacion-exclusiones` y cuenta solo los tres archivos que ella crea.
- Comprueba que con una exclusión se cuentan **2**, y sin exclusiones **3**.
- Añade regresiones para un archivo excluido (0) y una ruta vecina con prefijo parecido (1).
- Si falla la cuenta, muestra los valores obtenidos, rutas inaccesibles y estado de truncamiento.
- La prueba posterior de SHA-256 utiliza ahora `includedFile`, porque `ok.txt` ya no está directamente bajo `tmp`.
- Conservar el archivo `ejemplo.txt` garantiza que el Inspector y las exclusiones no vuelvan a interferir accidentalmente.

## Alcance y validación

No se modifica `ScanFileEstimator`, `ScanExclusions`, ClamAV, WPF, el instalador ni la lógica antivirus. **Se mantiene la versión de aplicación `1.1.0-preview.13`** y el mismo AppId. Es una corrección de pruebas, identificada como FIX1 en el ZIP.

La estructura de los ZIP y la aplicación del parche se han verificado fuera de Windows. **No se ha podido ejecutar .NET 10 ni las pruebas WPF en este entorno**. En Windows debes volver a ejecutar `PRUEBAS.bat` y no distribuir si cualquier etapa falla. La prueba anterior era la primera que se detenía; las pruebas siguientes aún pueden revelar otras incidencias.
