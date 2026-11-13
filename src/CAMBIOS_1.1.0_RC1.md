# PalClam 1.1.0 RC1 — candidata de estabilización

Base: Preview 25, con los módulos anteriores conservados.

## Incluido

- Versionado coordinado `1.1.0-rc.1` en WPF, servicio e Inno Setup.
- Pantallas, exportaciones y scripts muestran RC1 y emplean su instalador.
- `PRUEBAS.bat` mantiene los cinco pasos: verificación de fuentes, SmokeTests,
  ThemeTests WPF, compilación de WPF y compilación/autoprueba del servicio.
- Instalador conserva el `AppId` anterior y el servicio sigue siendo opcional;
  no se registra en el equipo sin acción expresa con UAC.
- `VALIDAR_PAQUETE.bat` valida archivos y firmas ClamAV offline después de preparar
  los binarios en Windows. No es una prueba de aceptación de uso real.
- Nueva puerta documental `VALIDAR_CANDIDATA_RC1.bat`, integrada en
  `REVISAR_ENTREGA.bat`: empieza bloqueada hasta que se aporten evidencias
  reales de cada comprobación de `docs/ACEPTACION_1.1.0_RC1.ini`.
- `PUBLICAR_ACTUALIZACION.bat` permanece bloqueado, sin modificar el canal estable.

## Alcance de validación en este entorno

Se inspeccionaron y verificaron los archivos de fuentes y la correspondencia
ZIP/parche. **No se han ejecutado .NET 10/WPF, Windows Service Control Manager,
FreshClam ni Inno Setup en Windows**, por lo que RC1 NO está homologada.
El usuario debe ejecutar las pruebas y completar el acta en un equipo de prueba.

## Límites funcionales

Guard personal y Machine Guard siguen siendo vigilancia reactiva, sin bloqueo
preventivo de acceso a archivos. Un resultado sin detecciones no certifica que un
archivo sea seguro. PalClam AI no ejecuta acciones y necesita consentimiento
explícito para los proveedores remotos.
