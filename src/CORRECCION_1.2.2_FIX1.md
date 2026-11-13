# PalClam 1.2.2 GPL3 — FIX1 de arranque y fuentes públicas

La aplicación conserva la versión 1.2.2 y su código C# no cambia. Esta corrección afecta a scripts, preparación de fuentes, instalador y documentación.

## Error de arranque

Windows PowerShell 5.1 puede evaluar los valores por defecto de parámetros de scripts avanzados antes de establecer PSScriptRoot cuando se utiliza powershell.exe -File. Se elimina el cálculo de ProjectRoot del bloque param en 16 scripts. La ruta se resuelve después de enlazar los parámetros, mediante PSCommandPath o MyInvocation.MyCommand.Path. Los BAT del desarrollador pasan además ProjectRoot explícitamente con una ruta que no acaba en barra invertida antes de las comillas.

El paquete completo contiene un solo EMPEZAR_AQUI.bat. Al aplicar el parche, el arranque retira el alias antiguo EMPEZAR-AQUI.bat únicamente si sus bytes coinciden con el archivo original conocido. Un archivo modificado por el usuario o un enlace no se elimina.

## Fuentes públicas sin BAT

El desarrollador conserva sus accesos BAT. El ZIP de fuentes públicas y la carpeta Source instalada no reciben esos accesos. Se eliminan las entradas BAT del archivo Inno y se retiran en actualizaciones solo los nombres de wrappers anteriores instalados por PalClam, sin tocar Data ni carpetas personales.

Se incluyen BUILD_FROM_SOURCE.md, el proceso PowerShell real y el archivo Installer/PalClam.iss. Se comprueba la ausencia de BAT en el ZIP; README.md de las fuentes públicas contiene instrucciones PowerShell. También se incluyen las fuentes exactas de OpenMcdf/UTF.Unknown requeridas por la comprobación previa. PUBLIC_SOURCE_MANIFEST.json registra los hashes reales de ese paquete sin wrappers.

Los verificadores pasan a comprobar la implementación PowerShell y la puerta de publicación bloqueada en Tools/PublicacionBloqueada.ps1, manteniendo las comprobaciones de seguridad, pruebas, versiones y aceptación. Los BAT son accesos opcionales, no dependencias de la construcción pública.

## Comprobaciones de esta corrección

- 50 comprobaciones de arranque, parámetros y rutas: simulación de variables de reflexión vacías durante binding, parámetros corregidos, invocación -File, carpeta con espacios, consola en otro directorio, raíz explícita/implícita, error ante raíz incorrecta y retirada segura del alias.
- 51 comprobaciones aisladas de empaquetado y licencias: ambas arquitecturas portables, sin datos personales, GPLv3 íntegra, fuentes sin BAT, scripts reales e instrucciones de reconstrucción.
- Verificador de fuentes y recursos: 2.064 claves en seis idiomas, controles de seguridad y publicación bloqueada, tanto en el paquete del desarrollador como después de extraer las fuentes públicas sin BAT.
- GPLv3: 125 fuentes C# y metadatos/recursos comprobados en las fuentes extraídas. Se corrige el filtro para excluir bin/obj/dist solo dentro del proyecto, no por el nombre de una carpeta superior.
- PowerShell: análisis sintáctico de todos los scripts, eliminación de defaults dependientes de variables de reflexión y comprobación de fallos sin anunciar éxito.
- Manifiesto, hashes y reconstrucción del parche recalculados sobre el ZIP completo 1.2.2.

Se utiliza PowerShell 7.6.5 en Linux. El comportamiento que provoca el error en 5.1 se modela con variables vacías y está documentado por Microsoft en https://github.com/PowerShell/PowerShell/issues/4688 . No se ha ejecutado Windows PowerShell 5.1 nativo ni ISCC en este entorno: el usuario debe repetir EMPEZAR_AQUI.bat en Windows. Las pruebas nativas de instalación/Outlook/interfaz siguen pendientes. No se recompilan de nuevo los ejecutables porque no cambia código C#, recursos ni propiedades de proyecto; siguen aplicando las evidencias de 1.2.2 sin presentarlas como pruebas nuevas.

La licencia propia continúa GPL-3.0-or-later. Las licencias originales de terceros no se modifican. Las fuentes incluyen los scripts e instrucciones necesarios para compilar/instalar; excluir BAT no elimina las obligaciones de fuente correspondiente de la GPL.
