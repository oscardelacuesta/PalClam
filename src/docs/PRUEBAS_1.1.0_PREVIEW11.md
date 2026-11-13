# Plan de aceptación — Preview 11

En un Windows 10/11 x64 con SDK .NET 10:

1. Ejecutar `PRUEBAS.bat`. Deben pasar el chequeo de fuentes, SmokeTests, 360 cambios de tema WPF y compilación Release Windows.
2. Abrir Panel principal a resolución 960 × 660: el resumen del motor queda debajo del título, sin solapar botones; Diagnóstico aparece debajo del estado y las métricas se apilan y los módulos muestran sus nombres completos.
3. Ampliar la ventana a 1220 × 830 y luego maximizar: comprobar cambio a diseño horizontal y tarjetas alineadas.
4. Repetir pasos 2-3 con tamaño de texto 90%, 100%, 120% y 130%; revisar que no se ocultan botones.
5. Alternar seis temas consecutivamente mientras el panel muestra un análisis real iniciado en `Análisis de archivos`; comprobar que contadores, pausa entre lotes, detener y progreso mantienen su comportamiento.
6. Revisar colores de estado cuando faltan firmas, cuando Guard está detenido, cuando hay resultado con detecciones y cuando se cancela. No confundir «motor listo» con protección preventiva.
7. Probar teclado y foco en botones de Inicio/Diagnóstico/Historial/Cuarentena y acceso desde menú lateral.
8. Comprobar Inno Setup sobre instalación anterior y permanencia de los datos del usuario (no borrar perfiles).
9. Si hay cierre, consultar `%LOCALAPPDATA%\PalClam\Diagnostics\fallos.log` y conservar el registro sin incluir datos privados en informes públicos.

**Pendiente:** no afirmar validación satisfactoria hasta realizar estos pasos en Windows y con motor ClamAV real.
