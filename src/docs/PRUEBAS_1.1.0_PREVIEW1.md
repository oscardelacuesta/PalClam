# Plan de aceptación — PalClam AI 1.1.0 Preview 1

Comprobar en Windows 10/11 x64 con .NET 10 SDK e Inno Setup.

## Compilación y conservación de 1.0

- [ ] `PRUEBAS.bat` supera los tests sin conectarse a servicios de IA.
- [ ] `CREAR_INSTALADOR.bat` genera `PalClam_1.1.0_Preview1_Setup_win-x64.exe`.
- [ ] La nueva instalación conserva las preferencias e historial de 1.0.0.
- [ ] Análisis de archivos, firmas offline, actualizador, Guard, Mail, Outlook y cuarentena funcionan igual que en 1.0.0.
- [ ] No se cambia el AppId ni se publican versiones Preview al canal estable.

## Privacidad, claves y consentimiento

- [ ] Abrir PalClam AI no genera llamadas HTTP; tampoco transmite datos de análisis.
- [ ] La clave de nube se puede introducir en memoria sin guardar.
- [ ] «Guardar clave» crea el almacén cifrado DPAPI CurrentUser; no aparece la API key en `settings.json` ni informes.
- [ ] «Eliminar clave» impide que se recupere tras reiniciar.
- [ ] Modificar el texto, el destino o el proveedor exige renovar el consentimiento para nube.
- [ ] Las consultas remotas no se envían si falta consentimiento o clave API.
- [ ] Ningún fichero ni contenido de correo se adjunta a las solicitudes.
- [ ] El resumen anónimo no incluye ruta, nombres de ficheros ni contenido personal.
- [ ] Las URLs remotas no aceptan HTTP y los proveedores oficiales mantienen su dominio.
- [ ] Ninguna redirección HTTP es seguida con la API key.
- [ ] Al cancelar, la interfaz queda utilizable; se advierte que el proveedor puede haber recibido ya el texto.

## Proveedores y modelos

- [ ] Ollama local funcionando con un modelo descargado; sin clave.
- [ ] LM Studio local funcionando con servidor iniciado; sin clave.
- [ ] OpenAI API con API key del desarrollador y modelo autorizado.
- [ ] OpenRouter con API key y modelo válido según catálogo.
- [ ] Gemini mediante endpoint de compatibilidad con OpenAI.
- [ ] xAI Grok mediante Responses API.
- [ ] Claude mediante API Messages nativa.
- [ ] Groq, Mistral, DeepSeek y Together AI según cuota y modelo de la cuenta.
- [ ] Perplexity Sonar con el modelo correspondiente.
- [ ] Endpoint OpenAI-compatible propio mediante HTTPS.
- [ ] El catálogo de modelos puede fallar con proveedores sin acceso/listado; el ID manual funciona.
- [ ] Se muestran errores 401, 403, 404, 429 sin imprimir claves privadas.
- [ ] La IA no modifica el resultado antivirus ni toma acciones sobre archivos.

**Aprobar solo después de las pruebas reales.** No hay datos de modelos simulados ni pruebas en red incluidas en este ZIP.
