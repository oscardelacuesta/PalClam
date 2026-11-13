# PalClam 0.10.0 — Outlook autorizado y bandeja residente

- Icono dinámico en la bandeja de Windows: inactivo, segundo plano, Guard, analizando, pausado, aviso y actualizando.
- Doble clic para abrir, opción de mantener la ventana cerrada en segundo plano y salida explícita.
- Pausa/reanudación de Guard desde el menú del icono. La pausa no significa bloqueo antivirus.
- Lectura opcional de los correos recientes de Outlook.com/Microsoft 365 con Microsoft Graph + permiso delegado `Mail.Read`.
- Autorización mediante código de dispositivo usando MSAL y Client ID público proporcionado por el usuario.
- El correo se descarga como MIME temporal y se procesa con el mismo analizador de PalClam Mail.
- Tokens solo en memoria, sin contraseñas ni claves privadas en ajustes. No se modifican los buzones.
- Opciones: mantener en bandeja, ID de aplicación y límite de correos por lote.
- Roadmap de PalClam AI (no implementado): proveedores OpenAI, Ollama local, OpenRouter y Gemini; análisis opcional sin acciones destructivas autónomas.

**Aviso:** la lectura de Outlook requiere una app registrada en Microsoft Entra y consentimiento real; sin Client ID válido no se activa. La vigilancia de Guard requiere PalClam en ejecución y no bloquea archivos. No se ha probado con Windows real desde el entorno de creación.
