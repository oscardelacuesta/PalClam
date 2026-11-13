# Pruebas manuales — PalClam 1.1.0 Preview 23

1. Extraer el ZIP completo en una nueva carpeta de Windows. Ejecutar `PRUEBAS.bat`. No instalar si falla alguna de las cinco etapas.
2. Ejecutar `EMPEZAR_AQUI.bat` solo si compila. Verificar que el instalador se llama `PalClam_1.1.0_Preview23_Setup_win-x64.exe` y conserva AppId.
3. Abrir Dashboard con historial vacío: **sin datos** en lugar de cifras o porcentajes inventados. Abrir Salud 360 y comparar con las etiquetas del Dashboard.
4. Ejecutar análisis rápido y personalizado con resultados conocidos, uno cancelado y otro sin detecciones. Comprobar duración, historial, detecciones y fecha del último análisis. El panel debe actualizarse sin bloquear la interfaz.
5. Abrir Centro de notificaciones, generar avisos de prueba y comprobar los seis avisos más recientes, sus fechas y gravedad. Marcar avisos revisados y verificar que el Dashboard no los cuenta como nuevos, pero **no los borra**.
6. Desactivar USB Watch y Download Guard, reiniciar Guard y comprobar que el panel muestra «Opcionales · no activados». Activarlos voluntariamente y verificar que no se declara activo cuando la vigilancia no funciona.
7. Sin servicio Windows instalado: mostrar «Opcional · no instalado». Con servicio detenido, fichero antiguo o eventos perdidos: nunca declarar vigilancia saludable. Probar solo en entorno aislado.
8. Forzar historial de más de 1 MB o un registro JSON incompleto: debe advertir de datos limitados o tratar el registro defectuoso sin errores en el Dispatcher. No usar malware real en las pruebas.
9. Probar los seis temas, ventanas de 960 px y 1220 px, zoom de texto y el cambio de Dashboard a otras pantallas. Confirmar ausencia de solapamientos/parpadeos y que los accesos directos funcionan.
10. Revisar `REVISAR_ENTREGA.bat`, instalación, desinstalación, firmas y servicio. Nunca presentar estado visual como certificación de protección preventiva.
