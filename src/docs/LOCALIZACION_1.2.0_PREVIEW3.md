# Localización de Preview 3

2.055 claves y 12.330 textos en ES/EN/FR/DE/PT/IT. Se preserva el contenido de las 1.907 claves de Preview 2 y se añaden 148 para correo, LED, monitor y estados. La cobertura nueva se relaciona con el código en COBERTURA_NUEVOS_TEXTOS_1.2.0_PREVIEW3.json. La cobertura completa anterior sigue en el informe de Preview 2.

Traducción de presentación, sin cambiar valores internos, opciones, rutas, asuntos, mensajes externos o argumentos del motor. La detección de idioma Windows, la prioridad de selección manual y los fallbacks no cambian. El código no realiza llamadas a servicios de traducción en ejecución. Los parámetros numéricos se mantienen opacos y los patrones de archivo conservan sus separadores y extensiones.

Se revisaron los textos de límites, sospecha frente a detección confirmada, ausencia de bloqueo, privacidad, contadores y etiquetas de los nuevos estados. Esto no sustituye la revisión visual y lingüística de todas las pantallas en Windows con DPI y temas diferentes.

Comprobaciones: python Tools/validate_localization.py y PalClam.LocalizationTests. En Windows, PRUEBAS.bat incluye los recursos reales y las pruebas WPF de alternancia de temas/idiomas.
