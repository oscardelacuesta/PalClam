# PalClam Preview 14 — Aurora, tarjeta de estado e inicio con Windows

## Pruebas en Windows

1. Ejecutar `PRUEBAS.bat`: SmokeTests, cambios de tema WPF y compilación .NET 10.
2. Panel principal: provocar resultado no concluyente; verificar que «Repetir análisis» cabe entero con textos 90–130 %, ventana compacta y grande.
3. Cambiar los seis temas mientras el programa analiza. Las ondas deben cambiar de color sin perder botones ni provocar cierres.
4. Opciones > Apariencia: desactivar/activar aurora, minimizar y restaurar. Se detiene al ocultar o minimizar la ventana.
5. Opciones > General: activar «Iniciar PalClam al entrar en Windows». Comprobar en el Administrador de tareas > Aplicaciones de inicio. Cerrar sesión e iniciarla de nuevo: icono en bandeja, sin ventana.
6. Mostrar PalClam desde la bandeja, cerrar con X: sigue en bandeja solo si «Mantener en la bandeja» está activado. «Salir de PalClam y detener Guard» termina el proceso.
7. Desactivar el inicio con Windows: verificar que desaparece de HKCU\Software\Microsoft\Windows\CurrentVersion\Run (valor PalClam).
8. Verificar que Guard solo se inicia al arrancar si está marcado «Activar Guard al abrir PalClam» y hay carpetas configuradas.
9. Instalación en una ruta que contenga espacios. Confirmar que el comando de registro está correctamente entrecomillado.
10. Probar «Restablecer preferencias»: debe desactivar el autoarranque y restaurar la aurora. Registrar fallos, si los hay.

## Límites

- Inicio con Windows es *opcional*, por usuario, y no crea un servicio ni da privilegios de administrador.
- Arranque silencioso no significa protección preventiva: Guard sigue siendo vigilancia reactiva.
- No se han validado automáticamente en este entorno la compilación WPF ni el inicio de sesión real.
