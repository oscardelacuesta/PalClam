# Pruebas de PalClam 1.0.0 RC4 en Windows

Ejecutar `PRUEBAS.bat` primero. Debe completar las pruebas y compilar WPF con .NET 10. Después `CREAR_INSTALADOR.bat` y comprobar que el instalador se ejecuta, se desinstala y actualiza la misma instalación de RC3.

1. Sin analizar, abrir «Análisis de archivos»: **no** debe verse el botón rojo Detener. Debe aparecer el mensaje de inactividad.
2. Comenzar un análisis sobre una carpeta de prueba: debe aparecer el botón **rojo** «Detener análisis», con progreso.
3. Pulsar Detener: el botón debe pasar **a ámbar** («Deteniendo…») y quedar deshabilitado mientras el motor confirma la cancelación.
4. Tras cancelar: el botón rojo debe **desaparecer**, el estado debe indicar «Cancelado» y el resultado **no debe aparecer como limpio**. Debe ofrecer Reintentar análisis incompleto.
5. Reintentar y dejar terminar: la acción roja debe desaparecer y se debe mostrar el estado final según las detecciones; comprobar informe y contadores.
6. Abrir «Herramientas > Acerca de PalClam» y el enlace del autor. Deben abrirse la web profesional, ClamAV y los documentos de licencia y atribuciones del instalador.
7. Comprobar módulos existentes: Guard, cuarentena, Mail, Outlook, actualización de firmas y motor, programador de análisis, tray.
8. Sin Internet: usar el motor offline con firmas disponibles y observar los avisos de antigüedad de firmas.

**Importante**: no distribuir como versión estable hasta validar estas pruebas. No usar muestras reales de malware para estas pruebas; EICAR es suficiente para ensayos inofensivos.
