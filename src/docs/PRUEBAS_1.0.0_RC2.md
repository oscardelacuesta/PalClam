# Validación de PalClam 1.0.0 RC2 en Windows

## Compilación e instalación

- Ejecutar `PRUEBAS.bat`: **12 pruebas lógicas y compilación Windows/WPF**.
- Ejecutar `CREAR_INSTALADOR.bat`: verificar que construye EXE con Inno Setup, motor y firmas offline.
- Cerrar PalClam RC1 desde la bandeja. Instalar RC2 sobre RC1 y verificar que conserva preferencias e historial.
- Probar desinstalador, accesos directos y ausencia de binarios duplicados o desactualizados.

## Interfaz RC2

- Comprobar encabezado, barra lateral sin scrollbar gris y los cuatro grupos plegables; abrir cada una de las **12 páginas**.
- Reducir ventana a 960x660: comprobar acceso con rueda/teclado y que no hay recortes que impidan trabajar.
- Comprobar que la vista principal no afirma que existe protección en tiempo real.
- Generar un análisis inofensivo: azul durante ejecución, verde al terminar sin detecciones.
- Cancelar un análisis de prueba: aviso ámbar de resultado incompleto, nunca limpio.
- Verificar que **Detener análisis** y **Detener Guard** son rojos.
- Pausar y reanudar Guard: botón ámbar, estado en el panel y cambio en el icono residente.
- Revisar los estados de motor y firmas al iniciar sin Internet, manteniendo copia offline.

## Seguridad funcional (sin muestras maliciosas)

- Usar únicamente el archivo de prueba inofensivo EICAR siguiendo documentación oficial; comprobar detección y cuarentena reversible.
- Guard: carpeta temporal con archivos inofensivos; comprobar eventos, pausa, reanudación y aviso de eventos perdidos.
- Mail: probar archivos EML/MSG inofensivos y advertencias sobre adjuntos omitidos.
- Outlook: conexión autorizada, lista de mensajes y desconexión sin guardar contraseñas.
- Comprobar actualizaciones firmadas; no probar instaladores sin verificar integridad.
- No publicar esta candidata en el canal estable.

Si alguna prueba falla, conservar el log de la consola BAT y el informe correspondiente.
