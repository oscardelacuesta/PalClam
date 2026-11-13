# PalClam 1.2.2 GPL3 — FIX4: datos de prueba Maildir en Windows

Se corrige la prueba que terminaba con `Maildir sin extensión: cur/new sí, tmp nunca`. **La versión del programa sigue siendo 1.2.2.** FIX4 identifica esta revisión de fuentes y construcción.

## Causa identificada

La prueba de FIX3 creaba el mensaje `cur/1730.M123:2,S`. Ese nombre utiliza el separador de flags habitual de Maildir en Unix. En Windows/NTFS, los dos puntos identifican un flujo alternativo de datos (ADS): `File.WriteAllText` escribe en el flujo `2,S` del archivo base `1730.M123`. Al enumerar la carpeta aparece el archivo base, cuyo contenido principal puede estar vacío, en lugar del mensaje que la prueba pretendía crear. Por eso el importador no obtiene los dos mensajes válidos esperados. El hash posterior de la ruta con dos puntos podía leer el flujo y no advertía este error en la preparación de la muestra.

Referencia de Microsoft sobre esa sintaxis: [File Streams](https://learn.microsoft.com/en-us/windows/win32/fileio/file-streams).

## Corrección

Se utiliza `1730.M123!2,S` como archivo ordinario de prueba en todos los sistemas. El importador trata los nombres como datos opacos y no depende de ese separador. Donde el sistema lo admite, una prueba adicional sigue verificando el nombre Unix con `:2,S`.

Se mantienen la exigencia de **dos mensajes**, la lectura de **cur/new**, la exclusión de **tmp**, los límites de importación y el cierre de temporales. Se refuerzan las comprobaciones:

- El archivo de muestra debe aparecer en la enumeración de la carpeta y contener los bytes esperados antes de importarlo; así se detecta una muestra que haya acabado en un flujo NTFS.
- Deben importarse exactamente los dos orígenes previstos, sin errores ni omisiones.
- Las copias deben conservar íntegros sus bytes y la relación con el origen.
- Una subcarpeta con estructura de buzón dentro de tmp también debe quedar excluida.

**El único C# modificado es `PalClam.FeatureTests/Program.cs`.** El importador y todo el código de la aplicación, proyectos, recursos, scripts BAT/PowerShell e instalador son idénticos a FIX3. No se suprime ni se fuerza el éxito de ninguna comprobación.

## Validación realizada

Entorno local: Linux, SDK .NET **10.0.404** descargado de Microsoft y verificado por SHA-512 frente a sus metadatos oficiales, runtime .NET 10 y PowerShell 7.6.5.

- PalClam.FeatureTests: **59 verificaciones superadas**, incluida la muestra compatible con Windows ejecutada como archivo normal, el nombre Unix con flags, conservación del correo, cancelación y eventos reales de archivos de prueba del monitor. El número de comprobaciones varía por sistema: algunas son específicas de Unix.
- PalClam.LocalizationTests: **49.358 comprobaciones superadas**.
- PalClam.DistributionTests: **22 comprobaciones superadas**.
- Verificadores de fuentes y GPLv3: 125 fuentes C#, 2064 claves y seis idiomas correctos.
- ZIP público de fuentes generado, extraído y verificado: sin BAT, instrucciones PowerShell y archivos requeridos presentes, hashes del manifiesto comprobados. Se vuelven a ejecutar los verificadores sobre esa extracción.
- Licencias y archivos originales de bibliotecas comprobados por hash; comparación exacta de los archivos de aplicación con FIX3 y reconstrucción exacta del completo al aplicar el parche.

El registro de Windows recibido del usuario confirma que FIX3 ya superaba allí las pruebas de procesos (24), arranque (52), empaquetado (76), recursos (49.358) y arquitectura/almacenamiento (22). Esas salidas proceden del usuario y no equivalen a una ejecución local de FIX4 en Windows.

**Límites de validación:** no hay Windows/NTFS, PowerShell 5.1 ni Inno Setup disponibles en este entorno. La batería general PalClam.SmokeTests compila, pero su ejecución en Linux se detiene en `USB Watch: no analiza unidades previas al inicio`: sus muestras F:\ y G:\ se interpretan mediante las reglas de rutas del sistema anfitrión. Se conserva ese resultado en el log y no se presenta como PASS. Las pruebas generales de Windows y WPF y la generación final de instaladores quedan pendientes en Windows; no se cambia ni se omite esa batería en el BAT.

Los registros de esta revisión están en `docs/validacion-1.2.2-FIX4`.

## Aplicación

Extrae el parche sobre tu carpeta actual **FIX3**, acepta sobrescribir y ejecuta **EMPEZAR_AQUI.bat**. El ZIP completo FIX4 es la alternativa para una carpeta nueva; no hace falta aplicar ambos.

Se conservan la versión 1.2.2, GPL-3.0-or-later del código propio, las licencias de terceros, el AppId, las cinco salidas previstas y las fuentes públicas sin BAT. El manifiesto vigente es `MANIFIESTO_1.2.2_FIX4.json`; los anteriores se mantienen como históricos.
