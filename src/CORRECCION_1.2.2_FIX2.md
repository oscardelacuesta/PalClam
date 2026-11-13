# PalClam 1.2.2 GPL3 — FIX2 de pruebas, errores y temporales

Esta revisión corrige el manejo de procesos y limpieza del banco de pruebas. La aplicación conserva la versión 1.2.2, su código C#, recursos, propiedades de proyecto y licencia GPL-3.0-or-later. Se conserva FIX1: una entrada EMPEZAR_AQUI.bat, rutas resueltas después del binding, fuentes públicas sin BAT y construcción PowerShell equivalente.

## Error observado

El usuario comunica un fallo Remove-Item sobre una carpeta temporal de las pruebas de empaquetado. La salida recibida no contiene la ruta completa ni el error interno suficiente para atribuirlo con certeza a un bloqueo concreto. El código FIX1 tenía dos problemas que podían ocultar esa causa:

1. Un Remove-Item sin protección en finally podía sustituir un error original por el error de limpieza.
2. Las pruebas capturaban procesos con 2>&1 bajo ErrorActionPreference=Stop. Windows PowerShell 5.1 puede interrumpir esa captura al recibir stderr, antes de recoger todas las líneas y el estado final.

Referencias de Microsoft: https://github.com/PowerShell/PowerShell/issues/4002 y https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_preference_variables .

## Cambios

TestSupport.psm1 inicia los procesos de prueba mediante System.Diagnostics.Process, sin intérprete de comandos intermedio. Drena stdout y stderr simultáneamente y espera la finalización real antes de devolver el código y la salida completos. La ruta y argumentos admiten espacios; se prueba también la composición de argumentos compatible con .NET Framework/PowerShell 5.1.

Los temporales utilizan nombres más cortos y un marcador de propiedad. Solo se eliminan directorios de pruebas identificados dentro de Temp. Se rechazan carpetas ajenas y enlaces. Hay reintentos acotados y se conserva el marcador si existe un archivo bloqueado. Un error de limpieza devuelve un estado, sin sustituir la excepción original.

Si todas las comprobaciones pasan y únicamente queda pendiente borrar un temporal, se muestra un aviso con su ruta y se continúa. Si falla una comprobación real, el resultado sigue siendo no cero y la compilación se detiene. No se eliminan ni se saltan las comprobaciones de arquitectura, firmas, licencias, privacidad o empaquetado.

Las pruebas de empaquetado guardan cada etapa y la salida de sus procesos en dist/validation/packaging-<identificador>.log, fuera de la carpeta que limpian. Un fallo de escritura del log tampoco sustituye el diagnóstico principal. Se aplica el mismo manejo de limpieza a las pruebas de arranque.

La creación de fuentes cierra el ZIP y su archivo subyacente con finally anidados; una limpieza de un archivo parcial no puede ocultar el error de generación. Los directorios reales de publicación se siguen limpiando de forma obligatoria antes de reutilizarlos: se reintentan bloqueos transitorios, pero si no se consigue limpiar se detiene la entrega para no mezclar archivos antiguos.

## Comprobaciones realizadas

Entorno: PowerShell 7.6.5 en Linux.

- 22 comprobaciones nuevas de procesos, captura completa de stderr con salida 0 y 17, rutas/argumentos con espacios y barra final, error de arranque de proceso, reintentos, bloqueo persistente simulado, conservación del fallo original, carpetas ajenas y cierre de archivos.
- Las pruebas incluyen dos comprobaciones adicionales de bloqueo real específicas de Windows, que se ejecutarán allí: archivo abierto sin compartir y conservación del marcador mientras permanece bloqueado. No se declaran ejecutadas en Linux.
- 52 comprobaciones de arranque/rutas.
- 51 comprobaciones de empaquetado/licencias, sin suprimir pruebas negativas ni usar un éxito incondicional.
- Verificadores completos de fuentes, seis idiomas y GPLv3, tanto sobre el árbol de desarrollo como sobre las fuentes públicas extraídas sin BAT.
- Análisis sintáctico de PowerShell y hashes/manifiestos recalculados.
- C#, proyectos, recursos y propiedades de compilación comparados byte a byte con FIX1: sin modificaciones, por lo que no se repite la compilación de esos binarios.

Windows PowerShell 5.1 nativo e Inno Setup no están disponibles en este entorno. Se corrigen los patrones identificados y se prueban sus condiciones; no se presenta esto como una ejecución completa en Windows ni se garantiza que el fragmento recibido permita conocer la causa exacta del bloqueo inicial. Si una prueba funcional vuelve a fallar, esta revisión deja su etapa, código y salida completos.

## Aplicación

Aplicar el parche sobre la carpeta de fuentes de FIX1, sobrescribir los archivos y ejecutar EMPEZAR_AQUI.bat. No hace falta borrar Temp manualmente, ejecutar como administrador ni desactivar el antivirus. También se ofrece el completo en una carpeta nueva.

El producto conserva 1.2.2; FIX2 identifica la revisión de construcción. El manifiesto vigente es MANIFIESTO_1.2.2_FIX2.json. No se cambian AppId, configuración, motores, firmas, claves de actualización ni datos del usuario. Las fuentes públicas siguen excluyendo BAT y contienen las instrucciones y scripts necesarios para GPLv3.
