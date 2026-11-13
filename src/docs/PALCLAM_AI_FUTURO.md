# Hoja de ruta futura: PalClam AI

**Actualización 1.1.0 Preview 1:** el primer módulo IA ya está implementado (ver `PALCLAM_AI_PROVEEDORES.md`). Esta hoja conserva los objetivos de siguientes versiones, sin asumir funciones avanzadas no incluidas.

Principios para diseño:
- Proveedor y modelo seleccionables, soporte de múltiples perfiles y pruebas de conexión.
- Claves API bajo DPAPI Windows CurrentUser / almacenamiento seguro; no en settings.json ni logs; no incluirlas en ZIP o instalador.
- Ollama local con dirección de conexión configurable, restringiendo por defecto a localhost.
- Envío de datos a proveedor en nube exclusivamente con consentimiento explícito e información clara de destinatario, tamaño y contenido. Por defecto enviar solo indicadores minimizados (hashes, nombre de firma, cabeceras sanitizadas), no adjuntos completos ni cuerpos de correo.
- La respuesta de IA es una opinión explicativa y no sustituye firmas ClamAV, heurísticas, reputación ni la decisión del usuario.
- No poner automáticamente en cuarentena, eliminar, restaurar ni ejecutar archivos basándose solo en la salida de una IA.
- Defensa ante prompt injection: tratar correos, archivos y respuestas del proveedor como datos no fiables, nunca como instrucciones de alto privilegio.
- Registro opt-in y límites de costes, tokens y latencia. Gestionar errores de conexión y privacidad.
