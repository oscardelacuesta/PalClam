# PalClam — Hoja de ruta actualizada

## 1.0.0 — Consolidación

- RC1–RC5: instalación, ClamAV offline, actualizaciones verificadas, cuarentena, Guard, Mail, Outlook y experiencia visual.
- RC6: monitor de proceso ClamAV, consumo de CPU y RAM, velocidad lógica, prioridades y advertencias de estabilidad.
- RC7: gráficas y comparación histórica con validaciones de equivalencia.
- RC8 FIX1/FIX2: verificación de fuentes y paquetes; corrección de rutas PowerShell y conflictos de tipos WPF/Windows Forms.
- **1.0.0 (fuentes preparados):** versión final sin etiquetas RC; pruebas de aceptación y comprobación legal requeridas antes de la distribución pública.

## 1.1.0 — PalClam AI (Preview 1 implementada; pruebas de integración pendientes)

- **Opcional y sin conexión automática al abrirla.** 13 proveedores: Ollama, LM Studio, OpenAI API, OpenRouter, Gemini, Grok/xAI, Claude, Groq, DeepSeek, Mistral, Perplexity, Together AI y compatible personalizada.
- Gestión segura de credenciales por usuario (cifrado a nivel de Windows). Nunca incluir claves en registros o exportaciones.
- Ningún archivo, adjunto ni información personal se envía a proveedores externos sin autorización explícita y clara.
- La IA puede explicar resultados, recomendar pasos y ayudar a interpretar amenazas, pero **no determinar por sí sola el estado limpio/contaminado** ni borrar archivos automáticamente.
- Límites de tamaño, coste, red, errores y tiempos de espera; registros auditables de cada solicitud, excluyendo secretos.

## Posterior — Motor persistente y concurrencia

Evaluación separada de `clamd` y cola multihilo con aislamiento local y pruebas de seguridad; sin prometer mayor velocidad por defecto.

PalClam complementa la protección del sistema, no sustituye Microsoft Defender.


## Preview 8 (11-11-2026)
Control de pausa cooperativa real entre lotes, seguimiento y barra flotante opcional. Más adelante: rendimiento de `clamd` local seguro y comprobaciones Windows.
