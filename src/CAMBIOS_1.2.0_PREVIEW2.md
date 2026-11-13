# PalClam 1.2.0 Preview 2

12 de noviembre de 2026. Base: 1.2.0 Preview 1.

- Se amplía la localización a la interfaz de todos los módulos: navegación, Protección, Guard, USB, Machine Guard, notificaciones, cuarentena, correo, Outlook, IA, Inspector, herramientas, Ayuda, historial, rendimiento, análisis, programación y Ajustes.
- 1.907 claves ES/EN/FR/DE/PT/IT y 11.442 textos. Inventario sintáctico, plantillas con campos opacos, recursos `.resx` y mapa de cobertura para mantenimiento. Se incluyen entradas de informes y mensajes dinámicos, no solo etiquetas.
- Nuevo valor predeterminado `UiLanguage=auto`. Resuelve la cultura de interfaz de la sesión de Windows al iniciar. La preferencia manual guardada prevalece; culturas regionales se normalizan y el fallback es español. Los perfiles Preview 1 con elección explícita se respetan.
- Adaptación visual sin cambiar los datos: los selectores conservan ItemsSource, SelectedItem, SelectedValue y DisplayMemberPath. Los modelos, comparaciones de estados, comandos y valores persistidos no se traducen.
- Traducción de cuadros de confirmación preservando sus botones y opción predeterminada; títulos/filtros de diálogos de archivos manteniendo sus patrones; bandeja y avisos de Windows traducidos antes de truncarlos.
- Rutas, nombres de archivos, asuntos, adjuntos, hashes, modelos de IA y respuestas externas se conservan. Las consultas guiadas de IA se traducen antes de presentarlas al usuario, sin enviarlas automáticamente.
- Instalador configurado con seis idiomas y detección de idioma de interfaz de Windows, manteniendo el AppId. Las tareas propias del instalador tienen mensajes por idioma.
- Compilación cruzada WPF y pruebas de localización ejecutadas en Linux con .NET 10. La ejecución WPF real y la aceptación Windows permanecen pendientes. La batería SmokeTests encuentra el mismo límite de rutas F:\ que la base bajo Linux; no se declara superada.

Traducción asistida por modelos locales durante la preparación, con revisión de terminología y advertencias importantes. La aplicación no incluye modelos de traducción ni consulta traductores en línea. Falta la revisión visual y lingüística final en Windows.

Publicación bloqueada y nueva acta `ACEPTACION_1.2.0_PREVIEW2.ini`, inicialmente PENDIENTE. Las actas anteriores no aceptan esta versión.
