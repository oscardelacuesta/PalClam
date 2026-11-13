# Pruebas manuales — PalClam Preview 17

1. En una carpeta nueva ejecuta `PRUEBAS.bat`: deben finalizar las pruebas funcionales,
   360 cambios de tema WPF, compilación de `PalClam.exe` y `PalClam.Service.exe`.
2. Ejecuta `EMPEZAR_AQUI.bat` para crear el instalador. Para Machine Guard selecciona
   instalación compartida en Program Files y usa una máquina virtual de laboratorio.
3. En Guard, comprueba los controles **Iniciar / Detener / Reiniciar**, aceptando UAC;
   verifica el resultado en `services.msc`. Repite cancelando UAC: no debe cambiar el servicio.
4. Verifica que un ejecutable personal no puede activar operaciones globales.
5. Con Machine Guard activo y PalClam en bandeja (sin ventana visible), provoca eventos **inofensivos** que causen
   registros normales; comprueba que no aparecen avisos por eventos anteriores a abrir WPF.
6. Prueba una incidencia controlada del motor de pruebas sin archivos maliciosos reales.
   Las amenazas reales se deben comprobar solo en un entorno aislado y autorizado.
7. Confirma que los avisos duplicados se limitan y todos los eventos permanecen en historial.
8. Cambia los seis temas durante un análisis, comprueba que la aurora no bloquea la cabecera
   y que la tarjeta de estado no recorta el texto.
9. Detén Machine Guard: debe indicarse sin latido/estado no operativo; Guard personal
   debe poder continuar por separado.
10. Comprueba desinstalación y limpieza de servicio/arranque sin perder registros.

**No activar el servicio experimental en producción sin completar estas pruebas.**
