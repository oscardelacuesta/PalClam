# PalClam 1.2.2 GPL3 — FIX3 de compresión y cierre de archivos

Se corrige el fallo `No se encuentra el tipo [IO.Compression.ZipArchiveMode]` de DistributionSource.ps1 comunicado al ejecutar FIX2 con Windows PowerShell. La versión del programa sigue siendo **1.2.2**; FIX3 identifica los scripts de construcción.

## Causa y corrección

Los cuatro scripts que usan ZIP cargaban únicamente `System.IO.Compression.FileSystem`. En Windows PowerShell 5.1 / .NET Framework hace falta cargar explícitamente también `System.IO.Compression`, donde está `ZipArchiveMode`. Ahora se cargan ambos ensamblados, en ese orden, antes de usar sus tipos. Se emplean sus nombres completos `System.IO.Compression.*` en fuentes, portables, validación y pruebas.

Referencia del ensamblado: [documentación oficial de Microsoft, ZipArchiveMode](https://learn.microsoft.com/en-us/dotnet/api/system.io.compression.ziparchivemode?view=netframework-4.8.1).

Además, FIX2 abría el archivo `.partial` y después construía el ZipArchive, pero protegía el cierre con un `try/finally` que empezaba demasiado tarde. Si fallaba el constructor o la resolución de su tipo, el archivo seguía abierto durante la limpieza. Esta fuga explica el aviso secundario de archivo utilizado por otro proceso del log recibido.

Ahora el archivo y el ZIP se inicializan dentro del mismo bloque protegido, con referencias inicialmente nulas y cierres anidados. Se hace lo mismo con el stream y el escritor del manifiesto. Se conserva el error original y una salida fallida no sustituye un ZIP anterior. No se omiten controles ni se convierten fallos funcionales en éxitos.

## Comprobaciones realizadas

Entorno local: **PowerShell 7.6.5 en Linux**.

- **76 comprobaciones de empaquetado y licencias**, con portables de prueba x64/x86 y controles negativos de arquitectura, licencias y dependencias. Los archivos PE, firmas y runtimes de esas pruebas son datos sintéticos temporales; no son ejecutables de entrega.
- Tres fallos inyectados en copias temporales del script real: constructor del ZIP, constructor del escritor y escritura del manifiesto. En cada caso se comprueba el código de error, el diagnóstico original, el cierre del archivo antes de salir del proceso, la eliminación del parcial y la conservación de una salida anterior.
- Comparación del mismo fallo de inicialización: FIX2 deja el stream abierto; FIX3 lo cierra. La sonda se ejecuta antes de la terminación del proceso, para que la terminación no oculte la fuga.
- **22 comprobaciones de procesos y limpieza**, **52 de arranque y rutas**, verificadores de fuentes/GPLv3 y **2064 claves en seis idiomas**. Las dos pruebas adicionales de bloqueo real específicas de Windows siguen incluidas; no se declaran ejecutadas aquí.
- Generación y extracción del ZIP real de fuentes públicas: ningún BAT, instrucciones PowerShell conservadas, manifiesto y hashes comprobados; verificadores de fuentes y GPLv3 sobre la extracción.
- C#, proyectos, XAML, recursos y propiedades de compilación comparados byte a byte con FIX2. Las licencias y fuentes de bibliotecas conservan sus hashes. Parche superpuesto a FIX2 reconstruye exactamente el completo FIX3.

Los registros están en `docs/validacion-1.2.2-FIX3`. Las salidas negativas registradas son fallos esperados de las pruebas.

**Límite:** este entorno no dispone de Windows PowerShell 5.1 ni de Inno Setup. No se ha ejecutado aquí el BAT completo de Windows, ni se han generado nuevos instaladores finales. La compilación y los ejecutables finales se generan en tu equipo con el BAT. No se repite la compilación C# porque estos cambios afectan únicamente a scripts y documentación.

## Aplicación

Sobre tu carpeta actual **FIX2**, extrae el parche FIX3, acepta sobrescribir los archivos y ejecuta **EMPEZAR_AQUI.bat**. El completo FIX3 permite empezar directamente en una carpeta nueva. No es necesario aplicar ambos ZIP.

Se conserva GPL-3.0-or-later para el código propio, las licencias originales de terceros, las cinco salidas previstas, el AppId y las fuentes públicas sin BAT. El manifiesto de esta revisión es `MANIFIESTO_1.2.2_FIX3.json`; los de revisiones anteriores se conservan como históricos.
