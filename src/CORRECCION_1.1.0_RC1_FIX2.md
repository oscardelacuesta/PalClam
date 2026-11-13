# PalClam 1.1.0 RC1 FIX2 — corrección de lectura UTF-8 del verificador

## Síntoma

Al ejecutar `PRUEBAS.bat`, la fase `[1/5]` puede detenerse con:

> `[ERROR PREVIO] RC1: Health 360 o USB/Download Guard incompletos o sin pruebas.`

## Causa verificada

`PalClam.SmokeTests/Program.cs` utiliza UTF-8 **sin marca BOM** y contiene los
textos reales de prueba `USB Watch: sin duplicados y reinserción detectable` y
`salud 360: Guard degradado se señala`. En **Windows PowerShell 5.1**, leer ese
archivo con `Get-Content -Raw` sin especificar la codificación puede interpretarlo
como ANSI. Los textos se alteran (`reinserciÃ³n`, `se seÃ±ala`) y `-notmatch`
provoca un falso positivo. Hay otras comprobaciones posteriores con tildes
(`Contador de servicio inválido`) que sufrirían el mismo defecto.

## Modificación

- `Tools/VerificarFuentes.ps1`: todas las lecturas de archivo con
  `Get-Content -LiteralPath ... -Raw` pasan a usar explícitamente
  `-Raw -Encoding UTF8` (77 apariciones). Compatible con PowerShell 5.1 y 7.
- No se ha eliminado ninguna comprobación ni se han cambiado sus expresiones
  regulares o condiciones de bloqueo.
- Sin modificaciones del antivirus, de sus tests C#, del motor, del servicio,
  de las firmas, la cuarentena o el instalador.

## Validación requerida en Windows

1. Extraer RC1 FIX2 completo en **carpeta nueva** o aplicar el parche sobre RC1 FIX1.
2. Ejecutar `PRUEBAS.bat`. La fase `[1/5]` debe superar el bloqueo por tildes.
3. Completar las fases `[2/5]` a `[5/5]` y comprobar el log real.
4. Solo si todo pasa, ejecutar `CREAR_INSTALADOR.bat` y validar el paquete.

**Nota:** esta corrección se ha comprobado estáticamente y comparando los
archivos fuente del ZIP. La ejecución real de PowerShell 5.1 y la compilación
.NET 10/WPF en Windows siguen pendientes y no se deben dar por aprobadas.
