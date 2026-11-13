# PalClam 0.8.0 — PalClam Guard

Esta versión conserva todas las funciones de 0.7.0 y añade vigilancia **opcional** de archivos mientras PalClam está abierto.

## Novedades

- Nueva sección lateral **PalClam Guard** y acceso desde el panel principal y Opciones.
- Vigilancia de 1 a 5 carpetas, incluidas sus subcarpetas, con `FileSystemWatcher` de .NET 10. Los archivos preexistentes no se analizan hasta que cambian; para un examen global usa el análisis manual.
- Arranque con PalClam **desactivado por defecto**. El usuario puede activarlo desde Guard.
- Eventos de creación, escritura y cambio de nombre con coalescencia de eventos repetidos, comprobación de estabilidad, cola limitada y una única ejecución de `clamscan` simultánea.
- Respeta las exclusiones existentes y los ajustes de análisis de comprimidos / PUA al iniciar Guard; cambios posteriores requieren reiniciar la vigilancia.
- Límite configurable de tamaño (50, 100 o 250 MB). Los archivos omitidos, inaccesibles o modificados durante el examen no se marcan como limpios.
- Contadores, lista de eventos, avisos Windows, registro local JSONL de incidencias e informe TXT.
- Las detecciones se pueden examinar de nuevo y aislar **manualmente**, previa confirmación, siempre que el archivo no haya cambiado. No se elimina ningún archivo.
- Marca explícita de vigilancia **incompleta** en caso de desbordamiento del búfer de Windows o cola llena; solicita análisis manual.
- Conserva instalador offline de ClamAV, actualización de firmas y motor, historial, cuarentena reversible, programación y compilación por BAT.

## Limitaciones y seguridad

**NO es un antivirus con bloqueo en tiempo real**: no intercepta lecturas ni impide ejecutar un archivo mientras el análisis está pendiente. No está activo si PalClam está cerrado, no garantiza todos los eventos, y no sustituye Microsoft Defender. `clamscan` carga firmas por ejecución; vigilar carpetas con cambios masivos puede consumir mucha RAM/CPU. Un proceso `clamd` persistente, cuidadosamente configurado, podría optimizar versiones futuras.

Este ZIP contiene **código fuente**. El EXE y el instalador deben compilarse en Windows con SDK .NET 10 e Inno Setup, respectivamente. No se ha validado todavía una compilación y ejecución Windows desde el entorno de elaboración.
