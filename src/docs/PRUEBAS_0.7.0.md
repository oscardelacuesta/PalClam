# Pruebas de aceptación en Windows para PalClam 0.8.0

**No se ha realizado una compilación Windows en el entorno que preparó este ZIP.**

1. Ejecuta `COMPILAR.bat`. Confirma cero errores; abre `EJECUTAR.bat` y visita cada sección.
2. Ejecuta `CREAR_INSTALADOR.bat` (con SDK .NET 10 e Inno Setup). Comprueba el EXE de `dist\installer` e instálalo en otra cuenta sin Internet; debe tener motor y firmas offline.
3. Prueba con el archivo estándar inofensivo EICAR descargado desde la página oficial, nunca con malware real. Analiza el archivo, selecciónalo y pulsa **Aislar detección seleccionada**; confirma el movimiento y la fila en **Cuarentena**.
4. Intenta restaurar mientras exista otra copia en la ruta original: la restauración debe rechazarse sin sobrescribir. Borra solo tu copia de prueba y restaura desde la interfaz si compruebas el falso positivo.
5. Programa un análisis diario a una hora cercana, inicia sesión y comprueba que aparece en **Historial local** como `[PROGRAMADO]`; revisa el informe. Luego elimina la tarea.
6. Termina un análisis manual y verifica si aparece el aviso de Windows; si no aparece, revisa configuración de notificaciones del sistema.
7. Cancela un análisis de carpeta: no debe presentarse como limpio. Confirma que no se han desactivado Defender ni servicios del sistema.
8. Desinstala y revisa que se elimine la tarea programada (si existe).

**Importante**: la preparación offline del paquete depende de que FreshClam pueda obtener firmas en el equipo de construcción. Conexión necesaria inicialmente para crearlo; no para instalarlo y analizar con el paquete resultante.
