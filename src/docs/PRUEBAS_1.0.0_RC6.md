# Validación PalClam 1.0.0 RC6 — Windows 10/11

1. Ejecutar `PRUEBAS.bat`. Deben pasar las pruebas anteriores más 8 nuevas; no debe haber errores de WPF.
2. Generar el instalador con `CREAR_INSTALADOR.bat` y comprobar el paquete offline.
3. Abrir `Opciones` → `Scan Engine PRO`. Probar prioridades Económico, Equilibrado y Prioritario. Se aplican al próximo análisis, no al que está activo.
4. Analizar una carpeta con archivos de varios tamaños. Comprobar archivos/s, MiB lógicos/s, CPU y RAM. El archivo mostrado debe identificarse como el último **completado**.
5. Comparar CPU de clamscan y memoria con el Administrador de tareas. La CPU en PalClam se expresa como porcentaje del equipo completo; debe variar con la carga.
6. Analizar un ZIP grande: comprobar que ausencia temporal de resultados no se identifica automáticamente como error o amenaza.
7. Cancelar el análisis: estado INCOMPLETO, sin barra verde 100 %, sin botón rojo persistente. El historial guarda el estado y las métricas disponibles.
8. Escanear una carpeta excluida o inaccesible: no debe mostrar estado limpio falso.
9. Desactivar la telemetría desde Opciones y repetir. Comprobar que el escaneo sigue funcionando sin muestreo.
10. Probar instalación RC6 sobre RC5: conserva configuración, historial y firmas. Probar bandeja, Guard, Outlook, Mail y cuarentena.

Antes de publicar la versión estable faltan: pruebas de carga prolongadas, comparaciones de rendimiento HDD/SSD, validación legal del nombre y firmas, y pruebas de actualización y recuperación entre versiones. No publicar RC6 como estable.
