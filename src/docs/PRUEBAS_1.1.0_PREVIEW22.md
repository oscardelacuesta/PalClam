# Pruebas manuales — Smart Scan PRO, Preview 22

## Construcción
1. Descomprimir completo en directorio nuevo. Ejecutar `PRUEBAS.bat` y verificar las cinco fases.
2. Ejecutar `EMPEZAR_AQUI.bat` solo si pasan. Confirmar nombre Preview22 del instalador; comprobar que actualiza sin romper AppId.

## Rápido
3. Preparar archivos de prueba **inofensivos** en Descargas, Escritorio y Documentos; comprobar que se incluye cada ruta existente, sin duplicaciones. Verificar resultados y cancelación.
4. Si falta alguna carpeta, confirmar que no se presenta como cobertura de todo el perfil; reintentar tras crearla.

## Completo
5. Confirmar diálogo y opción NO (sin iniciar); repetir con SÍ en máquina de pruebas. Solo unidades fijas; red y USB no deben aparecer automáticamente. No forzar análisis en equipos productivos.
6. Probar rutas inaccesibles y exclusiones: un análisis con errores debe indicar resultado no concluyente; la ETA no debe inventarse.

## Personalizado
7. Analizar un archivo pequeño con prioridad Económico, Equilibrado y Prioritario; comprobar el valor en el historial.
8. Cambiar modo de pausa a Preciso y comprobar lote individual (más lento); cambiar a Equilibrado, comprobar lotes.
9. Pausar durante un lote, reanudar, detener y reintentar; confirmar que para rápido/completo el reintento conserva todas las rutas.

## Criterios de seguridad
10. No modificar ni ejecutar archivos como consecuencia del análisis. ClamAV no equivale a protección preventiva.
11. Comparar historial con distintas rutas y configuraciones: no deben mostrarse comparaciones equivalentes si cambia el conjunto de objetivos.
12. Instalar/retirar servicio experimental solo en equipo de pruebas con permisos limitados.

**No aprobar distribución** sin compilación real, pruebas de instalación y aceptación manual.
