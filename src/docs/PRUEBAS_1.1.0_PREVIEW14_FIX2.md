# PalClam Preview 14 FIX2 — plan de validación en Windows

## Requisitos

- SDK .NET 10 y compilación WPF en Windows x64.
- Para probar inicio de todas las cuentas, instalador Inno Setup y una cuenta con permisos de administrador.
- Preferiblemente, otra cuenta de prueba de Windows; no instalar en producción antes de validar.

## Compilación

1. Ejecutar `PRUEBAS.bat` en el proyecto completo.
2. Confirmar verificación de fuentes, SmokeTests, ThemeTests (360 alternancias) y compilación de PalClam.
3. No ignorar una prueba fallida ni desactivar `VerificarFuentes.ps1` para permitir la instalación.

## Panel principal y aurora

1. Verificar que «Repetir análisis» tiene margen y texto legible sin recorte a anchos de 960, 1100, 1220 y 1400 px.
2. Probar tamaños de texto 90 %, 100 %, 120 % y 130 %.
3. Verificar movimiento de las cintas de aurora en los seis temas, incluyendo durante un análisis prolongado.
4. Verificar que el título PalClam no presenta hover de botón y que sigue abriendo Inicio.

## Inicio individual

1. Con instalación personal, seleccionar «Solo mi usuario» en Opciones → General.
2. Verificar entrada `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, valor `PalClam`, ruta entrecomillada y `--startup-hidden`.
3. Cerrar sesión, volver a entrar, comprobar icono en bandeja y que la ventana no se abra sola.
4. Seleccionar «Desactivado» y confirmar que desaparece únicamente la entrada propia de HKCU.

## Inicio para todas las cuentas

1. Desinstalar primero una copia personal existente si se va a migrar al modo compartido. Abrir el instalador y seleccionar «Instalar para todos los usuarios»; comprobar instalación en `Program Files`.
2. En Opciones → General seleccionar «Todos los usuarios», autorizar UAC y verificar HKLM Run de 64 bits.
3. Cambiar de cuenta y confirmar que arranca en la bandeja de la segunda sesión sin mostrar la ventana.
4. Confirmar que Guard NO se activa en una cuenta nueva sin consentimiento y configuración individual.
5. Repetir la elección de modo global denegando UAC, comprobar que la selección visual vuelve al estado verdadero.
6. Cambiar a «Solo mi usuario» y después «Desactivado», verificando que desaparece el inicio global de esta instalación, sin borrar registros de otra ubicación.
7. Desinstalar la copia compartida y comprobar que desaparece su entrada HKLM. Las entradas HKCU de otras cuentas no se editan automáticamente.

**Nota de alcance:** `HKLM\Run` inicia la aplicación al entrar en cada sesión; no se ejecuta como un servicio antes del inicio de sesión y no sustituye Microsoft Defender.
