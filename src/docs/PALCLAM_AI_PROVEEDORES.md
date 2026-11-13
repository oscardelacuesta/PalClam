# PalClam AI — proveedores y privacidad

**Versión 1.1.0 Preview 1.** Esta función es opcional y no hace llamadas al abrir la pantalla.

| Proveedor | Transporte | Endpoint base |
|---|---|---|
| Ollama | API local `/api/chat` | `http://127.0.0.1:11434/api` |
| LM Studio | Chat compatible local | `http://127.0.0.1:1234/v1` |
| OpenAI | Chat completions | `https://api.openai.com/v1` |
| OpenRouter | Chat compatible | `https://openrouter.ai/api/v1` |
| Google Gemini | Chat compatible beta | `https://generativelanguage.googleapis.com/v1beta/openai` |
| xAI Grok | Responses API | `https://api.x.ai/v1` |
| Anthropic Claude | Messages API | `https://api.anthropic.com/v1` |
| Groq Cloud | Chat compatible | `https://api.groq.com/openai/v1` |
| DeepSeek | Chat compatible | `https://api.deepseek.com` |
| Mistral AI | Chat completions | `https://api.mistral.ai/v1` |
| Perplexity Sonar | Chat compatible | `https://api.perplexity.ai` |
| Together AI | Chat compatible | `https://api.together.xyz/v1` |
| API compatible personalizada | Chat compatible | URL HTTPS indicada por el usuario |

**Importante:** el catálogo de modelos, precios, disponibilidad, requisitos y capacidades
varían. El usuario escribe el ID o pulsa «Consultar modelos disponibles», una llamada
explícita que puede requerir API key. No se incluyen cuotas ni modelos garantizados.

El contenido de la consulta se revisa en un cuadro editable. Para servicios de nube,
se debe marcar el consentimiento explícito para CADA solicitud. PalClam no sube
archivos ni adjuntos, no envía los cuerpos de los correos y no transmite rutas por defecto.

Las claves API pueden guardarse cifradas con Windows DPAPI CurrentUser en
`%LOCALAPPDATA%\PalClam\ai-secrets.dpapi.json`. No se sincronizan entre usuarios;
si se pierde el perfil de Windows es posible que haya que volver a introducirlas.
No hay transcripción local de consultas ni respuestas; el proveedor remoto puede
conservarlas según su política y los costes de API son independientes de ChatGPT Plus.

**Seguridad:** las respuestas del modelo son datos no fiables; no ejecutan código,
no cambian las firmas, los análisis, la cuarentena ni la configuración de Guard.

Fuentes oficiales útiles:
- https://docs.x.ai/developers/model-capabilities/text/best-practices
- https://docs.anthropic.com/
- https://ai.google.dev/gemini-api/docs/openai
- https://docs.ollama.com/api
- https://lmstudio.ai/docs/developer/openai-compat
- https://console.groq.com/docs/openai
- https://api-docs.deepseek.com/
- https://docs.mistral.ai/
