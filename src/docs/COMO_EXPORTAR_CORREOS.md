# Analizar mensajes exportados en PalClam Mail

1. Abre tu cliente de correo, por ejemplo Outlook.
2. Guarda o exporta el mensaje individual en `.msg` (Outlook clásico) o `.eml` (si tu cliente permite guardar como correo MIME). Las plantillas `.oft` también pueden analizarse cuando su contenido sea compatible.
3. En PalClam abre **PalClam Mail**, elige el archivo o una carpeta de mensajes y pulsa **Analizar correo**.
4. Revisa las columnas Estado, Mensaje, Asunto, Adjunto y Detalle. Los avisos de enlaces son heurísticos y no equivalen a malware confirmado.
5. Si quieres conservar resultados, pulsa **Guardar informe TXT**. Puede incluir información personal del mensaje.

**No es una conexión directa al correo:** PalClam 0.9.0 no abre el perfil Outlook, no lee PST/OST, no consulta Microsoft Graph, ni solicita contraseñas. Para analizar un buzón, exporta primero los mensajes compatibles.

Las opciones se guardan localmente en `%LOCALAPPDATA%\PalClam\settings.json`.
