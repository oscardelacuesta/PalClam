# PalClam 1.1.0 Preview 2 — Remediación bajo consentimiento

- Se mantiene PalClam AI con 13 proveedores; no modifica detecciones ni archivos sin aprobación.
- Cuarentena: botón rojo «Eliminar definitivamente seleccionado». Requiere confirmación explícita con opción NO por defecto.
- Análisis: botón «Aislar y eliminar…». Requiere dos confirmaciones: primero aislar; después borrar el archivo de cuarentena.
- Una cancelación en la segunda confirmación mantiene el archivo aislado y restaurable.
- Ninguna ruta de los metadatos de cuarentena se usa como destino de borrado. El borrado solo se realiza sobre el archivo opaco interno derivado de un GUID válido.
- Se conserva en el inventario de cuarentena la fecha de eliminación, aunque el archivo físico ya no exista.
- No se promete saneamiento completo del sistema, eliminación forense ni bloqueo preventivo de malware. Guard es observación de cambios, no mecanismo anti-kill.
- Se amplían pruebas automáticas con muestras **inofensivas generadas exclusivamente por las pruebas**, en una carpeta temporal de cuarentena de pruebas separada de la del usuario, limpiada al terminar.
- Self-Defense: trabajo futuro opcional (servicio Windows independiente con recuperación, ACL, auditoría). **No está implementado en Preview 2**. Los servicios protegidos PPL requieren condiciones especiales de Microsoft (ELAM/firma).
- Mantener Microsoft Defender y su protección antimanipulación; PalClam no debe desactivarlo.

Versión Preview: no distribuir sin validar compilación .NET10 WPF, instalación, consentimiento, registro de eliminación y funcionamiento offline en Windows.
