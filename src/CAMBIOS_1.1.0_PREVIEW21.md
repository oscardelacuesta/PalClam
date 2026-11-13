# PalClam 1.1.0 Preview 21 — Remediation Center PRO

## Cambios

- La pantalla **Cuarentena** pasa a denominarse **Remediation Center PRO**; mantiene todos los mecanismos reversibles y confirmaciones de restauración y eliminación anteriores.
- **Reanálisis voluntario en cuarentena**, individual o por lotes de un máximo de 20 objetos. Se utiliza `clamscan.exe` y las firmas locales existentes; el objeto físico permanece en su ruta de cuarentena con nombre opaco. El proceso NO ejecuta, restaura ni destruye el archivo. Tiempo límite por objeto: 5 minutos.
- **Comprobación del SHA-256 y del estado** antes del reanálisis y después de finalizarlo, antes de grabar el resultado; se rechazan enlaces, archivos cambiados, restaurados o eliminados y fichas no válidas.
- **Veredictos prudentes:** detectado, sin detecciones (no implica seguridad), o no concluyente. Un código de error o resultado ambiguo nunca se marca como limpio. La detección original permanece intacta.
- **Marca de revisión manual** para estudiar un posible falso positivo; no modifica la detección original ni habilita restauración automática.
- **Selección múltiple** limitada al reanálisis y la marca de revisión. Restaurar y eliminar siguen siendo operaciones individuales, con confirmación explícita.
- **Historial complementario local** de aislamiento, restauración, eliminación y revisión en JSONL; exportación a TXT UTF-8 con datos locales de la cuarentena y el historial (sin incluir el contenido de los archivos). No es un registro antifalsificación.
- Acceso desde el Centro de notificaciones. Compatible con el inventario creado por versiones anteriores: los nuevos campos son opcionales en JSON.
- Actualizado versionado, documentación y referencias BAT/Inno Setup de Preview 21, manteniendo el `AppId` original.

## Límites expresos

- La ausencia de detección no prueba que un archivo esté libre de malware; ClamAV puede omitir contenido por límites de análisis y permisos.
- Guard y Machine Guard siguen siendo vigilancia reactiva, no bloqueo preventivo ni equivalente a un controlador minifilter.
- En esta Preview **no** se realiza eliminación, restauración ni cuarentena automática, y **no** se envían datos a la IA ni a servicios remotos.
- El informe incluye rutas locales que pueden ser sensibles: revisarlo antes de compartirlo.
- El historial JSONL no es un sistema de auditoría inmutable ni sustituye un SIEM.
- No hay revisión automátizada de falsos positivos con IA ni gestión masiva destructiva en esta entrega.
- El paquete está preparado estáticamente, **no se ha compilado en Windows** durante esta preparación. Validar `PRUEBAS.bat` e Inno Setup antes de distribuir.
