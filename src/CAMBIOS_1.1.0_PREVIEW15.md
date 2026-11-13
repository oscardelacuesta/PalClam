# PalClam 1.1.0 Preview 15 — Machine Guard (experimental)

## Nuevas funcionalidades

- Servicio independiente `PalClamGuardSvc`, escrito en .NET 10, sin WPF ni paquetes NuGet adicionales.
- Binario propio `PalClam.Service.exe`, publicado autocontenido y en archivo único.
- Instalación voluntaria por administrador **solo en instalación compartida** de Program Files. Se ejecuta como **LocalService**, no LocalSystem.
- Vigilancia reactiva de una carpeta común del equipo: `%ProgramData%\PalClam\GuardWatch`.
- Un análisis ClamAV simultáneo como máximo, cola acotada a 256 archivos y espera para estabilizar archivos en escritura.
- Omitir archivos >100 MB, directorios y enlaces de reanálisis; no se aceptan rutas fuera de la carpeta común.
- Registrar detecciones, fallos de lectura y pérdida de eventos en el área común de diagnóstico, sin operaciones destructivas.
- Panel del servicio bajo Guard 2.0: instalar, retirar, consultar estado y recuentos; sólo indica actividad reciente si hay latido válido.
- Parada cooperativa al detener el servicio Windows. La desinstalación conserva ficheros vigilados y registros.
- Prueba `--selftest` de límites de directorio/archivo y nueva etapa del BAT.

## Alcance y límites obligatorios

- **No se instala ni se activa automáticamente.** Requiere elección explícita y UAC.
- Guard personal sigue siendo independiente; la vigilancia del servicio no alcanza los perfiles de usuario ni el contenido previo a su instalación.
- El servicio usa las firmas offline distribuidas con esta versión; no actualiza las bases automáticamente, no comprueba si están obsoletas y no sustituye a un antivirus con protección en tiempo real.
- No se garantiza cobertura: apagado, cambios perdidos, eventos repetidos, cola llena, enlaces o archivos inaccesibles obligan a efectuar análisis manual.
- La existencia de una detección no implica una eliminación ni una cuarentena: las alertas se registran.
- Deben validarse los permisos de `LocalService`, las firmas, el registro, el instalador y la desinstalación en Windows antes de distribuir.

## Compatibilidad

- Mismo AppId de Inno Setup.
- WPF y Guard 2.0 anteriores conservados sin modificar su motor de análisis.
- Versión: `1.1.0-preview.15`. Instalador: `PalClam_1.1.0_Preview15_Setup_win-x64.exe`.
