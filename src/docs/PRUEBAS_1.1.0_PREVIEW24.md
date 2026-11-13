# Pruebas de aceptación — PalClam 1.1.0 Preview 24

1. Ejecutar `PRUEBAS.bat` (validación previa, SmokeTests, ThemeTests, WPF y servicio). No continuar ante errores.
2. Compilar e iniciar PalClam. En **PalClam AI**, abrir AI Security Analyst y pulsar las cuatro plantillas. Confirmar que no se realiza ningún acceso a la red al preparar el texto.
3. Crear un análisis local de prueba (archivo inofensivo) y generar resumen. El texto debe contener solo contadores y estado; **nunca** ruta, nombre, detalle de detección, SHA-256 ni fragmentos de archivos.
4. Desde **Centro de notificaciones**, seleccionar un aviso y pulsar «Explicar alerta seleccionada». Comprobar que solo incluye gravedad y fuente.
5. Desde **Security Health 360**, abrir IA; verificar que sin diagnóstico previo aparece «No se ha ejecutado». Ejecutar comprobación y volver a preparar el resumen.
6. Desde **Cuarentena**, abrir IA: usar pruebas controladas, no malware real; comprobar que solo muestra contadores y que no hay restauración ni eliminación.
7. Seleccionar OpenAI/Claude/Gemini/OpenRouter, escribir consulta y probar sin marcar consentimiento: **no enviar**. Tras cualquier cambio de texto, modelo, endpoint o proveedor, el consentimiento debe reiniciarse.
8. Escribir manualmente `C:\Users\ejemplo\fichero.txt` y marcar consentimiento remoto: debe aparecer segunda confirmación de privacidad. Responder «No»: no se envía nada.
9. Probar Ollama o LM Studio en bucle local: revisar respuestas, cancelar, comprobar que no se modifican firmas o cuarentena. Para remoto usar clave de pruebas y datos ficticios, verificando el endpoint.
10. Verificar panel Dashboard, Guard, USB Watch y Smart Scan; comprobar que no hay regresiones de diseño ni de los seis temas.
11. Ejecutar `EMPEZAR_AQUI.bat`; revisar instalador Inno Setup, servicio opcional, archivos de documentación y desinstalación.

**No publicar** hasta completar pruebas reales Windows. AI Security Analyst es orientativo y no aporta bloqueo preventivo ni sustituye Microsoft Defender.
