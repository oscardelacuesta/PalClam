# Pruebas Windows — PalClam Preview 8

1. Ejecutar `PRUEBAS.bat` y comprobar los casos de pausa, reanudación y cancelación; después `EMPEZAR_AQUI.bat`.
2. Carpeta de 300 archivos inocuos, modo equilibrado: empezar, Pausar mientras examina un lote; debe decir «Pausando tras lote…», acabar ese lote, mostrar «Pausado» sin arrancar siguiente, y continuar al pulsar Reanudar.
3. Con archivos pequeños, modo preciso: comprobar «ARCHIVO EN EXAMEN» antes de la respuesta del motor. Comparar con «último archivo confirmado» una vez leído el resultado.
4. Aviso: en un ZIP grande la pausa puede tardar; no debe afirmar que se ha pausado antes de terminar el lote.
5. Detener mientras está en Pausado: no debe quedar bloqueado ni marcar el análisis como limpio.
6. Navegar a Inicio, Opciones y otra pantalla durante el análisis: franja inferior siempre visible; en Inicio tarjeta grande y controles funcionales.
7. Activar barra flotante en Opciones, iniciar análisis, moverla, activar topmost y pulsar Detalles. Ocultar no detiene el proceso. Terminar: desaparece.
8. Probar ventana 960x660 y texto 90/100/130 %, en los seis temas, también barra flotante.
9. Probar carpetas sin permiso y archivos ilegibles: indicador Incompleto, no Sin detecciones.
10. Probar 2 archivos de prueba EICAR y una muestra inocua en distintos lotes: conservar detecciones, no aislar ni eliminar sin consentimiento.
11. Comparar rendimiento con Preview 7, especialmente modo preciso. Verificar CPU/RAM, silencio y velocidad de archivos en pausa.
12. Comprobar instalador offline, motor y firmas con `REVISAR_ENTREGA.bat`. Revisar desinstalación y actualizaciones.

**No distribuir como versión estable** sin pasar estas pruebas en Windows. El sistema conserva Microsoft Defender activo.
