# PalClam 1.1.0 Preview 1 — PalClam AI MultiProveedor

**Preview para pruebas, no distribución estable automática.** Basada en PalClam 1.0.0.

## Funciones nuevas

- Pestaña **PalClam AI** con selector de **13 proveedores**: Ollama, LM Studio, OpenAI API,
  OpenRouter, Google Gemini, xAI Grok, Anthropic Claude, Groq, DeepSeek, Mistral,
  Perplexity, Together AI y API compatible con OpenAI.
- Tres protocolos efectivos: chat completions compatible; Responses de Grok;
  Messages nativo de Claude; adicionalmente API `/api/chat` de Ollama.
- Modelo editable y carga opcional del listado de modelos del proveedor al pulsar un botón.
  No se inventan modelos o planes de pago.
- Claves API independientes, guardadas **solo a petición**, cifradas con DPAPI CurrentUser.
  Nunca se guardan sin cifrar en settings.json ni se incorporan a instaladores.
- Consentimiento **por cada petición externa**, desmarcado al editar el texto y después de enviar.
- Textos supervisados por el usuario: botón para cargar resumen anónimo del último análisis,
  sin rutas, nombres de archivos, cuerpos de correo ni adjuntos.
- Permite cancelar; limita tiempo de respuesta, tamaño del texto y respuesta. Sin ejecutar
  comandos ni permitir acciones del modelo sobre archivos.
- Bloquea redirecciones HTTP y URLs HTTP externas; endpoints oficiales fijados a sus destinos;
  los motores locales solo aceptan loopback.
- Sin telemetría propia ni guardado de solicitudes y respuestas; la política de privacidad
  de cada proveedor remoto es independiente y puede haber costes.

## No incluido en esta Preview

- No analiza archivos binarios por IA, no envía adjuntos, no ejecuta cambios en cuarentena.
- No incluye chat multi-turno persistente, herramientas remotas ni agentes que ejecuten código.
- No incorpora detección automática con IA, ni cambia veredictos de ClamAV.
- No integra la suscripción ChatGPT de consumo: los servicios remotos requieren sus propias API keys.
- No activa clamd multihilo experimental.

## Verificación

Ejecuta `PRUEBAS.bat` y `CREAR_INSTALADOR.bat`. Los casos de prueba locales no acceden a Internet;
la conectividad real de cada proveedor debe validarse manualmente.
`PUBLICAR_ACTUALIZACION.bat` bloquea explícitamente la rama Preview.

La licencia del código propio permanece GPL-3.0-or-later y se conserva el AppId del instalador.
