# PalClam 1.0.0 RC5 — cierre técnico de la candidata

Base: RC4. Se mantiene C#/.NET 10, WPF, instalador BAT/Inno Setup, ClamAV local, soporte offline, Guard, Mail, Outlook, bandeja y sección «Acerca de».

## Correcciones de fiabilidad

- Política compartida `ScanVerdictPolicy` para análisis manual y programado.
- ClamAV `exit code = 1` se considera detección, incluso sin ruta o mensaje `FOUND` disponible. Si no se conoce el número, se registra como mínimo una detección (sin inventar detalles).
- `exit code = 2`, código desconocido, errores y cancelaciones siguen siendo no concluyentes; nunca se convierten en resultados limpios.
- Análisis programados: se leen también `Infected files`, `Scanned files` y `Total errors`, y se evita analizar como limpio un destino totalmente excluido.
- Pruebas de regresión ampliadas con ocho casos de estado de análisis.
- Pie y reportes corregidos para mostrar RC5 en vez de RC1/RC3.
- Verificación adicional del paquete offline y del instalador a través de `VALIDAR_PAQUETE.bat`.
- Publicación en el canal estable bloqueada mientras sea RC5.

## Estado de validación

Se han revisado los archivos y empaquetado; no hay compilación WPF ni instalación real en Windows desde este entorno. Ejecutar `PRUEBAS.bat`, `CREAR_INSTALADOR.bat` y el plan de pruebas manual para decidir la promoción a 1.0.0 estable.

## Limitaciones

PalClam no proporciona interceptación preventiva en Windows ni sustituye Microsoft Defender. La vigilancia Guard funciona mientras está abierta la aplicación y debe tenerse en cuenta la posibilidad de eventos perdidos.
