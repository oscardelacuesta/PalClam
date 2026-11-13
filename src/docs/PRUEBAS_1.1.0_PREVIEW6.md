# PalClam 1.1.0 Preview 6 — Pruebas visuales y funcionales

1. Ejecutar `PRUEBAS.bat` y confirmar que no haya errores WPF.
2. Ejecutar `CREAR_INSTALADOR.bat` y `REVISAR_ENTREGA.bat` y comprobar la versión Preview 6.
3. Abrir Inicio con ventana 960×660 y a 1220×830; comprobar que las 3 tarjetas y el panel caben en el scroll sin superposición.
4. Repetir con texto a 90 %, 100 % y 130 % y con los seis temas.
5. Confirmar que estado del motor y firmas reflejan su disponibilidad real.
6. Simular un análisis con EICAR de prueba: los contadores y el mensaje de detección deben actualizarse.
7. Durante el análisis: la cabecera indica «Análisis en curso»; el botón abre la vista de análisis, donde Detener queda visible arriba y abajo.
8. Cancelar análisis: debe mostrarse incompleto, nunca limpio ni con protección preventiva.
9. Usar accesos a Cuarentena, Diagnóstico, Histórico, Correo, IA y Ayuda.
10. Confirmar que ya no aparece «Pausar · no disponible» como botón de control.
11. La pausa/reanudación real del escáner no está implementada; no debe simularse.
12. Validar firmas ClamAV offline y desinstalación sobre una máquina de pruebas.
