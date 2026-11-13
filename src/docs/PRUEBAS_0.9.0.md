# Pruebas propuestas — PalClam Mail 0.9.0

1. Ejecutar `COMPILAR.bat` y comprobar que no existen errores del SDK .NET 10 ni de restauración de paquetes NuGet.
2. Crear instalador con `CREAR_INSTALADOR.bat` y verificar que incluye motor y bases offline.
3. Instalar en una máquina de pruebas sin Internet, ejecutar `PalClam Mail`, analizar un EML de prueba inocuo y verificar salida.
4. Exportar desde Outlook clásico un MSG con un adjunto TXT y un archivo ZIP inocuo. Confirmar recuento de adjuntos; repetir con la extracción desactivada.
5. Exportar un EML MIME con un adjunto PDF y comprobar que se identifica y examina. Probar subcarpetas, límites y cancelación.
6. Probar EICAR exclusivamente en entorno aislado y con sus instrucciones oficiales; comprobar que se registra como detección, sin ejecutar el archivo.
7. Analizar un archivo .pst/.ost: debe explicar que el formato no está soportado, nunca afirmar "limpio".
8. Probar correos corruptos, adjuntos grandes, enlaces punycode, fallos de permisos y archivos bloqueados: deben generar incidencias, no falsos resultados limpios.
9. Guardar TXT manualmente y revisar la información privada antes de compartir.
10. Verificar que preferencias de correo persisten después del cierre; comprobar que no se comunica con servicios de Outlook, Gmail o servidores externos.
11. Probar actualización y reinstalación sobre 0.8.0 sin perder configuración ni historial.

**Limitación de esta entrega:** no se ha ejecutado el compilador Windows en este entorno. El BAT en Windows es la prueba indispensable.
