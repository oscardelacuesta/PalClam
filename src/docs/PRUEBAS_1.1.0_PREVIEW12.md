# Aceptación manual — Preview 12

**Prerrequisitos:** Windows 10/11 x64, SDK .NET 10, motor ClamAV y firmas configuradas.

1. Ejecutar `PRUEBAS.bat`. No distribuir si falla algún paso; guardar salida.
2. Iniciar análisis largo (carpeta con archivos inocuos). Comprobar que el porcentaje, si aparece, se identifica como orientativo, no alcanza 100 antes de finalizar y la ETA no aparece prematuramente.
3. Pulsar **Pausar** mientras ClamAV analiza un lote: debe aparecer «Pausando tras lote»; pulsar **Anular pausa** y comprobar que sigue adelante.
4. Volver a pulsar **Pausar** y esperar a terminar lote: debe aparecer «Pausado» con la cola detenida; pulsar **Reanudar**, confirmar que deja de mostrar «lote en espera» y que continúa.
5. Interrumpir otro análisis: comprobar «Deteniendo…» hasta confirmación del motor y después «Incompleto / Cancelado», nunca 100%. 
6. Completar análisis inocuo: confirmar resumen con resultado, recuentos y advertencia sobre límites de protección; revisar el informe detallado.
7. Denegar acceso a una carpeta o provocar variación entre enumeración previa y resultados: no debe mostrarse porcentaje engañoso o resultado «sin detecciones» si hay errores de cobertura.
8. Repetir con barra flotante compacta/detallada y panel principal; los tres estados de pausa deben ser coherentes.
9. Alternar los seis temas y cambiar el tamaño del texto DURANTE un análisis: no debe cerrarse ni borrar el progreso.
10. Instalar sobre Preview 11 con `EMPEZAR_AQUI.bat` tras superar las pruebas; verificar que mantiene ajustes y que desinstala.

**Límite del test:** `FileSystemWatcher` de Guard no aporta bloqueo preventivo en Windows. El resultado de ClamAV no equivale a un antivirus de interceptación total.
