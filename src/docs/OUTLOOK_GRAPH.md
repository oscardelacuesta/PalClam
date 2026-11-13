# Conexión opcional con Outlook / Microsoft 365

1. En **Microsoft Entra > App registrations**, registrar una nueva aplicación llamada `PalClam - lector local`.
2. Permitir cuentas de organización y cuentas Microsoft personales cuando se necesiten ambas.
3. En **Authentication**, habilitar el flujo de cliente público (device code flow).
4. En **API permissions**, añadir Microsoft Graph > **Delegated** > `Mail.Read`. No solicitar `Mail.ReadWrite` ni permisos de aplicación. En entornos empresariales, el administrador podría bloquear o exigir consentimiento.
5. Copiar **Application (client) ID** (UUID público) a **Outlook en línea** dentro de PalClam.
6. Pulsar **Conectar Outlook** y abrir la URL oficial mostrada, introduciendo el código temporal. PalClam nunca debe solicitar la contraseña.
7. Seleccionar 5/10/25/50 mensajes de Bandeja de entrada y pulsar **Analizar**. Este acceso se hace solo cuando el usuario lo pide.

**Privacidad:** leer el contenido del buzón requiere `Mail.Read`. Los mensajes MIME descargados son temporales y se eliminan después del análisis cuando el sistema permite borrarlos; se muestran avisos si falla la limpieza. Los tokens se mantienen únicamente durante la ejecución. La aplicación no envía mensajes ni accede a contactos. No analiza buzones locales PST/OST. Desconectar invalida el estado de sesión dentro del programa.

**Limitaciones:** la autorización depende de la política de la cuenta/organización; algunas cuentas pueden prohibir el flujo por código. No es vigilancia de correo en tiempo real. El límite de tamaño y la posibilidad de archivo inaccesible pueden dejar correos sin revisar. Los informes de resultados pueden contener asuntos sensibles: no compartirlos sin revisión.

Documentación: https://learn.microsoft.com/en-us/graph/api/message-get?view=graph-rest-1.0 y https://learn.microsoft.com/en-us/graph/permissions-reference
