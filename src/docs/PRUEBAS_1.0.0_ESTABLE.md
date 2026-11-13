# Aceptación funcional — PalClam 1.0.0 estable

Antes de distribuir, registrar para cada prueba: fecha, edición de Windows, memoria, CPU, versión de motor y firmas, resultado y evidencia.

1. `PRUEBAS.bat` completa **47 pruebas** (o más) y compila el proyecto WPF sin errores.
2. `CREAR_INSTALADOR.bat` entrega el EXE offline y `VALIDAR_PAQUETE.bat` valida ClamAV, firmas, archivo inofensivo y SHA-256.
3. Actualización desde RC8 FIX2 conservando ajustes e historial, sin conflicto de iconos residentes o tareas programadas.
4. Instalación limpia en un equipo Windows x64 sin .NET SDK, con arranque y desinstalación correctos.
5. Análisis offline de carpeta inofensiva; firmas y versión del motor visibles; sin mostrar resultado limpio si hay errores.
6. FreshClam: actualización online y errores de red; verificar no sobrescritura de firmas más nuevas.
7. Detección del fichero de prueba **EICAR obtenido de su fuente oficial**, sin malware real. Aislar y restaurar sin sobrescribir otro archivo.
8. Cancelación y reintento: «Detener» rojo solo durante la ejecución; estado «Incompleto» tras cancelar.
9. Scan Engine PRO: CPU, RAM, archivos/s y MiB lógicos/s; no atribuir falsas certezas al archivo en proceso.
10. Gráficas, historial, CSV, comparador; rechazar comparaciones con distinta configuración, firmas o resultado de detección.
11. Guard: activar/desactivar, cambios en archivos y tratamiento de eventos perdidos; no prometer protección en tiempo real preventiva.
12. Mail local (`.eml`, `.msg`, `.oft`); adjuntos y errores; Outlook Graph con cuenta de pruebas, consentimiento y desconexión.
13. Tareas programadas, avisos, iconos de bandeja, informe y funcionamiento en distintas resoluciones.
14. Actualizador: manifiesto mal firmado, SHA distinto, URL ajena y ausencia de conexión deben rechazarse sin ejecutar instaladores.
15. Código GPLv2, atribuciones ClamAV y dependencias, derechos sobre bases de firmas y marca revisados para distribución.
16. Verificar que el paquete no contenga claves privadas, tokens ni datos personales, y que el manifiesto firmado use la clave publicada.

**Condición de liberación:** 0 incidencias críticas/altas conocidas; informe automático satisfactorio; aceptación manual documentada en `docs/ACEPTACION_1.0.0.ini`. No subir archivos de `dist/publish` hasta comprobarlo.
