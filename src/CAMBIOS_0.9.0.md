# PalClam 0.9.0 — PalClam Mail

## Novedades

- Nueva sección **PalClam Mail** accesible desde el lateral, panel principal y Opciones.
- Análisis local de archivos de correo exportados **.eml, .msg y .oft** y carpetas completas; selección o arrastre de archivo.
- Extracción temporal opcional de adjuntos e imágenes incrustadas (.eml), análisis del mensaje original con ClamAV y de los adjuntos extraídos.
- Resultados diferenciados por mensaje y adjunto, incidencias, progreso por mensajes completos y cancelación.
- Heurísticas propias opcionales para URLs con IP, punycode y credenciales; la etiqueta **Aviso enlace** no afirma malware ni phishing.
- Opciones de subcarpetas, archivos inline, heurísticas ClamAV, límites de tamaño, máximo de mensajes y exportación TXT.
- **No** se abre Outlook ni se conecta a Microsoft 365, Gmail, IMAP o POP3. Sin acceso directo a buzones .pst/.ost. Las integraciones online requieren una implementación posterior explícita y consentimiento del usuario.
- Se conservan todas las funciones de PalClam 0.8.0, motor de respaldo offline, actualización del motor y de firmas, instalación Inno Setup y compilación con BAT.

## Advertencias técnicas

- Prototipo no compilado ni probado en Windows en este entorno. Validar primero con correo de prueba, preferentemente en VM.
- Un resultado "Sin detecciones" nunca garantiza que el correo esté libre de malware o phishing.
- Los adjuntos extraídos no se ejecutan; se borran al terminar el mensaje. Un fallo de borrado queda notificado.
- Archivos omitidos, mensajes incrustados no soportados, fallos de parser o límites excedidos se indican como incompletos.
- Los informes pueden contener asuntos y nombres de archivos; su guardado automático está **desactivado** de fábrica.
- El funcionamiento sin Internet en el equipo destino requiere generar previamente el instalador completo con ClamAV y firmas offline.
- La compilación inicial necesita NuGet para recuperar MimeKit y MsgReader.

## Próximos pasos recomendados

1. Probar correos .eml y .msg normales, adjuntos ZIP/EICAR de prueba y mensajes malformados.
2. Añadir integración voluntaria con Outlook clásico/nuevo Outlook mediante APIs adecuadas, previa revisión de permisos.
3. Fortalecer aislamiento del parser de archivos no fiables y controles de recursos, y desarrollar pruebas automatizadas.
