# Matriz de aceptación: PalClam 1.0.0 RC5

**Importante:** los BAT y las pruebas automatizadas no equivalen a una validación antivirus completa. Estas pruebas deben ejecutarse en Windows 11 (y, si se anuncia compatibilidad, en las ediciones de Windows 10 compatibles).

## Construcción

1. Ejecutar `PRUEBAS.bat`: todas las pruebas, incluidas las de veredicto, deben pasar; `dotnet build` debe devolver código cero.
2. Ejecutar `CREAR_INSTALADOR.bat`: prepara ClamAV y firmas de respaldo, genera el instalador y ejecuta el validador de paquete.
3. Confirmar que `dist\installer\PalClam_1.0.0_RC5_Setup_win-x64.exe` existe y su SHA-256 coincide con `dist\installer\PalClam_1.0.0_RC5_Setup_win-x64.sha256.txt` o el informe de validación.

## Pruebas manuales obligatorias

- Instalar RC5 sobre RC4 con PalClam cerrado completamente desde la bandeja. Verificar que mantiene historial, preferencias y cuarentena.
- Desinstalar en un equipo de pruebas. Confirmar que no deja tareas programadas ni verbos propios del menú contextual (los datos personales de perfil se deben tratar por separado y con consentimiento del usuario).
- Arrancar SIN conexión: motor y firmas base presentes, escaneo de archivo inofensivo completo.
- Reconectar: comprobar FreshClam, motor y verificación de actualizaciones (sin publicar todavía ninguna RC).
- Analizar una carpeta con varios archivos. Verificar progreso aproximado, log, informe, estado al acabar y que el botón rojo desaparece.
- Cancelar un análisis: el resultado debe ser «Cancelado / Incompleto», nunca limpio; reintentar.
- Usar el archivo de prueba **EICAR** únicamente de forma voluntaria y en carpeta de laboratorio: confirmar detección y cuarentena con confirmación humana; restaurar únicamente el archivo de prueba.
- Comprobar análisis programados: resultado en historial, estado correcto, código de salida 1 no se presenta como limpio, errores de lectura no se presentan como éxito.
- Activar y pausar Guard en carpeta de laboratorio; comprobar notificaciones y comportamiento ante pérdida de eventos, sin confundirlo con protección preventiva.
- Analizar EML y MSG de prueba, adjuntos, cancelación y límites; conectar Outlook solo tras autorización Mail.Read en cuenta de laboratorio.
- Verificar iconos de bandeja y opciones «Salir», «Pausar vigilancia», «Abrir», y sección Acerca de con enlaces y documentos legales.
- Revisar que ni el instalador ni la actualización silenciosa cierren sesiones o borren datos inesperadamente.

## Publicación

Aún quedan pendientes: revisión legal del nombre y de la redistribución de firmas, pruebas de compatibilidad Windows, soporte y respuesta a incidentes, verificación de los paquetes y firma Authenticode si procede. `PUBLICAR_ACTUALIZACION.bat` está bloqueado para RC5.
