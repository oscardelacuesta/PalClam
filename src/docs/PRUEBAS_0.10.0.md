# Pruebas manuales Windows (OBLIGATORIAS)

1. Ejecutar `COMPILAR.bat` con SDK .NET 10 en Windows 11 x64.
2. Abrir PalClam, verificar icono de bandeja y tooltip 'Inactivo'.
3. Iniciar Guard con una carpeta de pruebas; el icono debería pasar a verde; crear un archivo inocuo.
4. Iniciar escaneo, comprobar icono azul y progreso del botón de tarea. Finalizado, volver a verde.
5. Pausar Guard desde bandeja; comprobar amarillo y que **no** se escaneen cambios durante pausa; reanudar.
6. Cerrar ventana con opción activa, verificar que el proceso sigue vivo; doble clic abre. Salir desde menú debe terminar proceso y Guard.
7. Desmarcar 'Mantener en bandeja', cerrar y comprobar que el proceso termina.
8. Configurar Client ID registrado y comprobar autorización device code, consentimiento Mail.Read y 5 mensajes recientes; validar CSV/TXT del módulo local por separado.
9. Comprobar cancelación, mensajes mayores del límite, conexión perdida y limpieza de temporales.
10. Comprobar que ningún escaneo envía contenido a terceros salvo las peticiones de lectura a Microsoft Graph autorizadas.
11. Validar instalador con `CREAR_INSTALADOR.bat`; despliegue sin Internet y firmas offline; no afirmar 'protegido' cuando Guard está parado.

No se ha realizado una compilación real de WPF en este entorno. Las pruebas anteriores son pendientes, no resultados.
