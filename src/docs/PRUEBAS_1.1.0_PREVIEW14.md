# Plan de pruebas — Preview 14 / Guard 2.0

1. Descomprimir el proyecto completo en carpeta nueva y ejecutar `PRUEBAS.bat`. No generar ni distribuir instalador si falla cualquier paso.
2. Iniciar Guard sobre una carpeta de prueba con archivos **inofensivos**. Verificar estado y contador de cola.
3. Copiar/guardar repetidamente un archivo; comprobar que los eventos se agrupan, el resultado queda registrado y la cola disminuye.
4. Pausar Guard con el botón: el estado debe indicar *procesamiento pausado*; modificar archivos y observar que la cola crece. El proceso en curso puede concluir. Reanudar: la cola debe vaciarse gradualmente.
5. Repetir para perfiles Eficiente, Equilibrado y Rápido guardando preferencias **antes de reiniciar** Guard. No deberían existir análisis simultáneos de Guard.
6. Alterar el tema visual durante la vigilancia, con y sin pausa, y cambiar el tamaño de ventana. Verificar que no se cierre PalClam.
7. Probar detener, salir y volver a iniciar. Guard debe dejar de vigilar al salir; tras reiniciar no recupera automáticamente archivos cambiados mientras estaba cerrado.
8. Verificar reacción ante archivos inaccesibles, borrados y superiores al límite: resultado *No analizado*, nunca *Limpio*.
9. Si se detecta pérdida de eventos, comprobar aviso *vigilancia incompleta* y ejecutar **análisis manual** para recuperar cobertura.
10. Crear el instalador solo tras las pruebas anteriores y verificar instalación/actualización, ClamAV, firma, cuarentena y desinstalación en Windows.

**Seguridad:** FileSystemWatcher es reactivo: Guard no intercepta ni bloquea la ejecución o lectura de un archivo. No sustituye Microsoft Defender ni otras defensas preventivas.
