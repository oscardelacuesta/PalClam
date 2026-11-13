# PalClam 1.0.0 RC8 FIX1 — Corregida ruta de PowerShell en el precontrol

## Error reportado al ejecutar `EMPEZAR_AQUI.bat`

```
[1/3] Verificando consistencia de fuentes, version y seguridad del paquete...
Caracteres no válidos en la ruta de acceso.
[ERROR] No superar esta comprobacion: no distribuir la version.
```

## Causa probable

En `PRUEBAS.bat` se pasaba `-ProjectRoot "%~dp0"` a `powershell.exe`. La expansión de `%~dp0` **termina en barra invertida (`\`)**, situada justo antes de la comilla de cierre. La interpretación de las comillas en los argumentos nativos de Windows puede incorporar caracteres indebidos a la ruta. El validador no podía resolverla.

El mismo patrón estaba en `VALIDAR_PAQUETE.bat`, que se ejecuta después de generar el instalador.

## Corrección

En ambos BAT se elimina el argumento `-ProjectRoot "%~dp0"`. Los scripts PowerShell ya tenían el parámetro opcional y calculan por defecto la raíz del proyecto de forma robusta:

```powershell
param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
```

Se mantiene la ruta absoluta entre comillas para localizar el script (`-File "%~dp0Tools\...ps1"`). **No se elimina ninguna comprobación de versión, firma, hash, paquete offline ni seguridad.**

## Uso

Extrae el ZIP entero en una carpeta nueva para no mezclar BAT antiguos con los corregidos. Ejecuta `EMPEZAR_AQUI.bat` desde la raíz.

Para corregir una copia RC8 ya extraída, también puedes sustituir únicamente `PRUEBAS.bat` y `VALIDAR_PAQUETE.bat` por los de FIX1.

La versión interna continúa siendo `1.0.0-rc.8`; FIX1 es una revisión del paquete de construcción, **no una versión instalada diferente**.

## Validación

Se ha comprobado estáticamente que ambas llamadas ya no pasan una ruta terminada en barra invertida y que los validadores PowerShell disponen de raíz predeterminada. **Pendiente de ejecutar en Windows**, al no disponer este entorno de PowerShell para Windows, .NET 10 ni Inno Setup.
