# PalClam 1.1.0 Preview 20 — Signature Center PRO

## Nuevas funciones

1. **Signature Center PRO** en Motor y firmas. Vista separada de `LocalAppData\PalClam\Signatures` y `ProgramData\PalClam\MachineGuard\Signatures`, con comprobación sin modificar bases.
2. Versiones y cantidades de firmas consultables con `sigtool.exe` si el fichero es accesible. El inventario muestra qué archivos hay presentes, pero nunca afirma por sí solo que sean bases válidas o las más recientes.
3. Actualización online explícita de Machine Guard: `Tools/MachineService.ps1 -Action UpdateOnlineSignatures`. Solo para administrador, únicamente instalación compartida; FreshClam oficial actualiza una copia temporal. Se comprueban las versiones de main/daily (bytecode opcional); no se permite retroceder a versiones compartidas anteriores. Sustitución por directorios con copia de respaldo y reinicio del servicio si estaba activo.
4. Se conservan la sincronización offline del instalador y las actualizaciones periódicas por cuenta de usuario. **No se habilita actualización automática del servicio** en esta Preview experimental.
5. El directorio compartido de firmas concede lectura a Usuarios y escritura a Administradores/SYSTEM; el servicio mantiene permiso de lectura. Esto permite visualizar metadatos desde una cuenta normal sin darle permiso para alterar firmas.
6. Se actualizan las etiquetas visibles de versión y se conservan todas las funciones A+B de Preview 19.

## Seguridad y limitaciones

- El código de WPF no contiene un canal de comandos de alto privilegio: invoca un script acotado con `runas` y confirmación, sujeto a UAC.
- Los datos de versión proporcionados por `sigtool` no son un certificado de seguridad; FreshClam y las bases deben proceder del proveedor oficial y el análisis del motor debe probarse aparte.
- Un error de descarga, comprobación o sustitución interrumpe el cambio y mantiene o restaura el directorio compartido anterior.
- El servicio no sustituye Microsoft Defender, ni intercepta preventivamente la apertura o ejecución de ficheros.

## Pruebas

Se agregan cuatro pruebas puras de inventario, sin uso de red, permisos administrativos o actualización de bases reales. Revisar `docs/PRUEBAS_1.1.0_PREVIEW20.md` para comprobar la actualización online y el instalador en Windows.
