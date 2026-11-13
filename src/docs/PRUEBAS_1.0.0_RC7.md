# Validación manual — PalClam RC7

1. Ejecutar `PRUEBAS.bat` y exigir código de salida 0 antes de `CREAR_INSTALADOR.bat`.
2. Instalar sobre RC6 con PalClam completamente cerrado desde la bandeja del sistema.
3. Abrir «Análisis de archivos» y analizar una carpeta de prueba con varios cientos de archivos inofensivos.
4. Confirmar que las tres tendencias gráficas cambian al obtener datos (sin falsas cifras iniciales).
5. Verificar contador de archivos, CPU/RAM y resultado al terminar. Cancelar otro análisis y comprobar «Incompleto».
6. Abrir «Historial gráfico / comparar análisis»: primera ejecución RC7 debe indicar falta de comparación.
7. Repetir **mismo objetivo**, sin modificar opciones ni firmas, y comprobar porcentaje orientativo de diferencia.
8. Cambiar opción de PUA o prioridad, repetir y verificar «no comparable».
9. Actualizar firmas, repetir y verificar «firmas diferentes».
10. Exportar CSV, inspeccionar los datos y comprobar que no se ha enviado nada a la red.
11. Probar Guard, Mail, Outlook y cuarentena: regresión sin cambios de comportamiento.
12. Crear instalador y validar modo offline, desinstalación y persistencia de datos locales.

**Importante:** la identidad de firmas se calcula por metadatos de los ficheros y no garantiza que el contenido analizado sea idéntico. La memoria caché y la carga del equipo también afectan al rendimiento. No publicar RC7 como estable sin pruebas reales en Windows.
