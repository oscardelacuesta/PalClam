# PalClam 1.1.0 Preview 14 FIX2

Revisión de Preview 14 FIX1 centrada en **arranque multiusuario** y corrección de la validación previa.

## Error PREVIO de `PRUEBAS.bat`

La comprobación `Tools/VerificarFuentes.ps1` seguía exigiendo el literal `new GridLength(244)` que dejó de existir cuando ampliamos la tarjeta a 286 en FIX1. La prueba se ha sustituido por comprobaciones de la distribución flexible y la ausencia de anchura fija. Mantiene las verificaciones de estructura, versión, seguridad y WPF.

Además se elimina el desbordamiento real del bloque «Repetir análisis»: antes se fijaba `readiness.Width = 286` dentro de una columna de 286 **con margen izquierdo de 15**, ocasionando recortes. Ahora la tarjeta usa el ancho útil de la columna, sin imponer una anchura fija.

## Inicio automático

- **Desactivado** (ningún inicio registrado en esta instalación).
- **Solo mi usuario** mediante `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- **Todos los usuarios** mediante `HKLM\Software\Microsoft\Windows\CurrentVersion\Run`, solo tras elevación UAC.
- La instalación para todos los usuarios debe ubicarse bajo Program Files. No se permite registrar en HKLM un ejecutable de Descargas o de una ruta de perfil individual.
- La escritura global se hace mediante un proceso auxiliar puntual del mismo ejecutable, sin iniciar WPF ni Guard elevado. La aplicación normal NO necesita permisos de administrador.
- En la desinstalación se eliminan solamente las entradas que corresponden a esa instalación.

**Alcance real:** Windows inicia PalClam oculto en la bandeja cuando cada cuenta comienza sesión. No es un servicio de Windows que funcione antes del inicio de sesión. Cada usuario conserva sus preferencias y debe autorizar/configurar su propia vigilancia Guard.

## Instalador

Inno Setup permite elegir **instalación personal o para todos los usuarios**: `PrivilegesRequiredOverridesAllowed=dialog` y carpeta `{autopf}\PalClam` adaptada al modo de instalación. Para migrar de una instalación personal existente a la compartida, es preferible desinstalar la personal primero y ejecutar el instalador en modo "todos los usuarios"; las configuraciones guardadas bajo el perfil no se copian automáticamente a otras cuentas.

## Validación recomendada en Windows

1. Ejecutar `PRUEBAS.bat` y confirmar las 4 etapas.
2. Comprobar que "Repetir análisis" no aparece recortado (ventanas 960–1400 y escalas 90–130 %).
3. Comprobar movimiento de auroras y cambios de los seis temas durante análisis.
4. Instalación personal: activar/desactivar el inicio de cuenta; comprobar `HKCU\...\Run`.
5. Instalación compartida: elegir "todos los usuarios", aceptar UAC, comprobar `HKLM\...\Run`, cerrar sesión en dos cuentas distintas y verificar bandeja. Denegar UAC y comprobar que el estado se revierte en Opciones.
6. Desinstalar y verificar que solo se borra la entrada Run correspondiente al ejecutable desinstalado.

**No se ha ejecutado la compilación WPF, Inno Setup ni las pruebas multiusuario en este entorno.** No distribuir antes de validarlas en Windows.
