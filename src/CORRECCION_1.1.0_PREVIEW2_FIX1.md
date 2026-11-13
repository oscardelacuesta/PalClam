# PalClam 1.1.0 Preview 2 FIX1 — Validacion de FreshClam

## Incidencia observada

La compilacion de .NET y el instalador Inno Setup terminan correctamente, pero
`VALIDAR_PAQUETE.bat` devuelve codigo 2 en `freshclam.exe --version` porque
FreshClam intenta abrir `ThirdParty\ClamAV\freshclam.conf` y no lo encuentra.

Esto ocurre porque el archivo de configuracion se genera para la preparacion de
firmas en `Tools\cache\freshclam-offline.conf`, y la aplicacion crea su propio
`freshclam.conf` en el perfil local del usuario. El ejecutable original de
ClamAV no tiene por que contener una configuracion predeterminada en su carpeta.

## Correccion

- `Tools\ValidarPaquete.ps1`: genera un fichero `.conf` temporal con
  `DatabaseDirectory` de las firmas offline y los mismos parametros esenciales
  que usa PalClam, llama a FreshClam con `--config-file=... --version` y
  elimina el fichero temporal en un `finally`. No actualiza firmas ni hace
  conexiones de red durante esta comprobacion.
- `VALIDAR_PAQUETE.bat`: corrige la cabecera y la referencia a la guia de
  pruebas de la version Preview 2.

Se mantienen todas las comprobaciones del ejecutable, firmas con SigTool,
archivo inofensivo con ClamAV y SHA-256 del instalador. No se modifica el
instalador, el motor ni el codigo de la aplicacion.

## Aplicacion del parche minimo

Copiar `Tools\ValidarPaquete.ps1` y `VALIDAR_PAQUETE.bat` en la carpeta raiz
de `PalClam_1.1.0_Preview2_Remediacion_Manual_BAT_Fuentes`, sobrescribiendo
los anteriores. El instalador ya generado se conserva.

Ejecutar `VALIDAR_PAQUETE.bat` y despues `REVISAR_ENTREGA.bat`.
No es necesario volver a compilar o ejecutar Inno Setup.

**Limitacion:** este cambio se ha revisado de forma estatica. La validacion
real requiere Windows y los binarios de ClamAV del equipo del desarrollador.
