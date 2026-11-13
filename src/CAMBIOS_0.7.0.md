# PalClam 0.7.0 — Cuarentena, programación y avisos

## Novedades

1. Cuarentena manual y reversible, que guarda cada elemento con un identificador opaco; registro de original, firma y SHA-256. Nunca se borra automáticamente ni se sobrescribe un destino al restaurar.
2. Análisis programados diarios o semanales, basados en `schtasks.exe` de Windows, por usuario y con sesión iniciada. Se pueden crear, consultar y eliminar desde PalClam; la desinstalación intenta eliminar la tarea.
3. Ejecución programada sin ventana mediante `PalClam.exe --scheduled-scan`; salida registrada en el historial y en informe local.
4. Avisos de bandeja al finalizar análisis manuales, controlables en Opciones.
5. Conserva interfaz, icono original, actualizaciones, ClamAV y firmas offline, menú contextual y compilación/instalador por BAT.
6. Corrección: no declarar 'sin detecciones' si el motor reportó una amenaza aunque retorne código 0.

## Limitaciones

- La cuarentena es **manual**, no aislamos de manera automática los archivos que ClamAV detecte.
- No se restauran rutas que ya estén ocupadas y no se crean carpetas que hayan desaparecido.
- En archivos ZIP las detecciones internas pueden no corresponder a archivos físicos separables: no se aíslan individualmente.
- La programación no ejecuta análisis si la sesión no está iniciada; si el equipo está apagado no se garantiza recuperar la ejecución perdida.
- No ofrece protección en tiempo real ni análisis de correo todavía. Los avisos Windows dependen de la configuración del sistema.
- Todavía hace falta compilar y probar .NET/WPF/Inno Setup en Windows 10/11.
