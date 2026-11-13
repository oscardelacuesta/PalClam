# Pruebas de aceptación — Preview 18

1. Ejecutar `PRUEBAS.bat`: verificar integridad, SmokeTests, 360 cambios de tema WPF, build WPF y build/autoprueba del servicio.
2. Abrir **Protección → Centro de notificaciones**. Debe abrir sin servicios instalados ni registros previos.
3. Generar un aviso controlado con Guard personal; comprobar que aparece en la lista y se conserva al cerrar/abrir PalClam.
4. Con el servicio experimental instalado en un entorno de pruebas, verificar lectura del archivo `eventos.log` desde el perfil normal, sin pedir elevación para abrir el historial.
5. Filtrar por `DETECCIÓN`, `Advertencia`, `Error`, origen personal/servicio y texto libre. Verificar que ningún filtro cambia el registro subyacente.
6. Marcar los eventos como revisados y seleccionar **Solo sin revisar**. Verificar que desaparecen de esta vista, pero se conservan al seleccionar **Todas** y sobreviven al reinicio.
7. Exportar selección filtrada. Comprobar que el TXT refleja solo lo visible; advertir de rutas personales antes de compartir.
8. Comprobar entradas dañadas o sin permisos: la interfaz muestra incidencia parcial en vez de declarar que todo está limpio.
9. Repetir con 6 temas y escala 90 %, 100 % y 130 %, minimizando a bandeja. Verificar que Guard y los análisis no se detienen.
10. Ejecutar `EMPEZAR_AQUI.bat` únicamente si `PRUEBAS.bat` pasa, y validar el instalador en Windows.

**Límites:** el centro consulta los registros existentes; no aporta detección ni cobertura adicionales. El servicio experimental mantiene sus restricciones y no debe considerarse antivirus preventivo.
