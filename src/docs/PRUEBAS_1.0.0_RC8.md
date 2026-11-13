# PalClam 1.0.0 RC8 — Aceptación manual antes de la versión estable

**Registra fecha, Windows, edición, versión .NET SDK, CPU, RAM, resultado y evidencias de cada comprobación.** No distribuir como estable si queda una incidencia crítica abierta.

- [ ] `PRUEBAS.bat` devuelve 0: precontrol de fuentes, pruebas de lógica y compilación WPF.
- [ ] `CREAR_INSTALADOR.bat` genera `dist\installer\PalClam_1.0.0_RC8_Setup_win-x64.exe` y `VALIDAR_PAQUETE.bat` devuelve 0.
- [ ] En equipo de prueba, instalar **sobre RC7** con PalClam cerrado desde su icono residente: conservados los datos y la configuración.
- [ ] Abrir PalClam RC8 y comprobar «Acerca de», iconos residentes (reposo, analizando, detenido, alerta y Guard) y estado de firmas.
- [ ] Con la red desconectada, comprobar `clamscan --version`, análisis de un documento inofensivo y advertencia de firmas antiguas sin confundirla con infección.
- [ ] Con conexión, actualizar firmas con FreshClam y comprobar rutas y fecha; probar recuperación de errores de conexión.
- [ ] Analizar carpeta de archivos inofensivos: progreso, últimos archivos completados, CPU/RAM, velocidad, gráfico y resumen final consistentes.
- [ ] Cancelar otro análisis: estado «Incompleto», sin mostrar «Limpio» ni mantener rojo el botón Detener.
- [ ] Repetir análisis exactamente igual y comparar; cambiar PUA, prioridad o firmas y comprobar que deja de ser comparable.
- [ ] Probar detección EICAR obtenida de la **fuente oficial**, sin malware real; aislar solo con confirmación y restaurar; verificar huella y ausencia de sobrescritura.
- [ ] Guard: activar/desactivar, simular cambios en carpeta de prueba, comprobar notificaciones y desbordamiento de eventos; verificar que nunca promete bloqueo preventivo.
- [ ] Mail: `.eml` y `.msg` de pruebas; analizar adjuntos, errores y límites sin abrir archivos ejecutables.
- [ ] Outlook Graph: probar con cuenta de pruebas y consentimiento delegado; verificar desconexión y no exposición de tokens en informes.
- [ ] Programador de tareas: ejecutar una tarea de prueba y verificar historial correcto sin ventanas interactivas.
- [ ] Actualizador: verificar rechazo de manifiesto sin firma, hash incorrecto y ruta externa; probar actualización firmada desde entorno de pruebas sin tocar el canal estable.
- [ ] Desinstalar: sin tareas programadas huérfanas y sin pérdida no consentida de datos; reinstalar y comprobar comportamiento.
- [ ] Auditar licencia, distribución de firmas, componentes y denominación comercial antes de publicar.

**Criterio para promover a 1.0.0 estable:** 0 errores críticos o altos, pruebas automáticas y paquete correctos, instalación/desinstalación y funcionalidades sensibles verificadas en Windows, y revisión legal documentada. Solo entonces activar la publicación estable.
