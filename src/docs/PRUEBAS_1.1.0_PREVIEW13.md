# Plan de validación — PalClam Preview 13: Inspector

## Requisitos

Windows 10/11 x64, SDK .NET 10 e Inno Setup 6 o 7 para generar el instalador. Para analizar con ClamAV deben existir motor y firmas locales disponibles. No desactivar Microsoft Defender.

## Automáticas

1. Ejecutar `PRUEBAS.bat`: verificación de fuentes; SmokeTests (incluye SHA-256 de archivo `abc`, cancelación, rutas erróneas, doble extensión); pruebas WPF de temas; `dotnet build` Windows.
2. Compilar mediante `EMPEZAR_AQUI.bat` y revisar el instalador. No confundir los ZIP de fuentes con un ejecutable instalado.
3. Ejecutar `REVISAR_ENTREGA.bat` solo después de preparar motor, firmas y empaquetado offline.

## Manuales: Inspector

1. Abrir **Inspector de archivos**. Confirmar que la nueva página aparece en el menú y se ve bien con los seis temas y tamaños de texto 90-130 %.
2. Pulsar **Examinar archivo** y elegir un TXT inofensivo: observar nombre, tamaño, fechas y SHA-256 de 64 caracteres hexadecimales. Copiar el hash y compararlo con `Get-FileHash -Algorithm SHA256`.
3. Arrastrar otro archivo sobre el Inspector: debe actualizarse la información del Inspector sin iniciar ClamAV automáticamente. Arrastrar una carpeta: debe informar que solo acepta un archivo.
4. Inspeccionar un archivo grande, cancelar la lectura y repetir. No debe mostrar una huella parcial como válida ni bloquear la interfaz.
5. Seleccionar `factura.pdf.exe` (archivo inofensivo creado exclusivamente para esta prueba): debe mostrar el aviso de **doble extensión**, sin declarar una detección.
6. Elegir un ejecutable de Windows firmado y otro no firmado. Verificar que el Inspector **no anuncia firma digital válida**, solo certificado encontrado o ausencia de uno identificable. No presupone que los programas sean seguros.
7. Iniciar un análisis con ClamAV desde el Inspector; comprobar que la barra de estado y el flujo del análisis se actualizan y que el resumen nuevo (sin detecciones, detecciones o error) aparece en el Inspector al terminar. Comprobar el botón de informe si se generó un registro.
8. Repetir el anterior sin motor o firmas disponibles: no debe recuperar como nuevo un resultado antiguo de la misma ruta.
9. Durante el análisis, alternar varias veces los seis temas, ampliar/reducir la ventana y probar pausar/reanudar y detener desde la barra de análisis. Confirmar que no hay cierres, solapamientos ni estados engañosos.
10. Confirmar que no se ejecuta el archivo inspeccionado, no se mueve a cuarentena sin intervención y no se envía automáticamente a IA o a Internet.
11. Repetir con un fichero en una unidad de red: el procesamiento se hace en el equipo, pero leer una ruta remota utiliza la red correspondiente.
12. Confirmar que las funciones previas (Guard, Correo, Outlook, IA, Opciones, historial y cuarentena) conservan su comportamiento.

No distribuir hasta tener la compilación y estas pruebas completas. Los errores persistentes de WPF se registran en `%LOCALAPPDATA%\PalClam\Diagnostics\fallos.log` cuando es posible.
