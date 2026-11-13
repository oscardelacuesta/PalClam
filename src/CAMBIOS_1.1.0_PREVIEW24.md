# PalClam 1.1.0 Preview 24 — AI Security Analyst

## Novedades
- Consultas guiadas de IA para el último análisis, incidencias Guard/USB/Machine Guard, estados de Health 360 y cuarentena.
- Generador local de contexto por **lista blanca**: solo estados predefinidos y cantidades, nunca rutas, nombres, etiquetas de detección, SHA-256, claves, correos, contenido o informes enteros.
- Desde el centro de notificaciones se puede seleccionar una alerta y preparar un contexto limitado a gravedad y origen.
- Botones también en Health 360 y en cuarentena; ningún clic genera tráfico API.
- Revisión del texto visible, consentimiento por consulta remota y aviso extra cuando el usuario escribe manualmente posibles datos privados.
- Las respuestas no ejecutan comandos, no alteran firmas, cuarentena ni Defender; en ningún caso convierten "sin detecciones" en "seguro".
- Se reutilizan proveedores, clave DPAPI, límites de texto, timeout y cancelación de PalClam AI existente.
- Corregida una declaración duplicada del parámetro `HistoryInvalidEntries` en DashboardOverviewPolicy (error de compilación heredado de Preview 23).
- 7 nuevos casos de pruebas automáticas de privacidad y composición de contexto, además del verificador actualizado.

## Límite conocido
Esta Preview **no está compilada ni verificada en Windows** desde el entorno de desarrollo. Se requiere `PRUEBAS.bat`, compilación WPF, pruebas con un proveedor local y remotos voluntarios, e instalador de pruebas antes de distribuir.
El texto escrito manualmente por el usuario debe revisarse: el detector de privacidad es heurístico y no puede garantizar la ausencia de secretos.
