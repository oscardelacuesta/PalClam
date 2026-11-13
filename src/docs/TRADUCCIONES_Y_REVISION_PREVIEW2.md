# Preparación y revisión de traducciones

Los recursos cubren la interfaz propia en ES/EN/FR/DE/PT/IT. Las cadenas se inventariaron mediante análisis sintáctico C#, conservando campos y cadenas de máquina. Las traducciones de preparación utilizaron modelos locales de Argos/OPUS mediante CTranslate2. Se revisaron términos, encabezados, estados, identidad del autor y advertencias de privacidad/capacidad; las frases largas conservan revisión lingüística y visual pendiente.

Los modelos, paquetes Python y herramientas de inferencia no se incorporan al ejecutable ni son dependencias de compilación de PalClam. La aplicación muestra recursos ya incluidos y no contacta con servicios de traducción. Tampoco traduce contenido personal, respuestas de IA ni archivos.

Referencias de las herramientas y modelos de preparación:

- Argos Translate: https://github.com/argosopentech/argos-translate
- Índice oficial de paquetes: https://github.com/argosopentech/argospm-index
- CTranslate2: https://opennmt.net/CTranslate2/
- OPUS-MT: Jörg Tiedemann y Santhosh Thottingal, «OPUS-MT — Building open translation services for the World», EAMT 2020. Los modelos descargados incluyen su atribución/licencia; no se redistribuyen sus pesos en este paquete.

Las comprobaciones de claves y formatos no certifican la calidad lingüística completa. Antes de una publicación estable deben revisarse las pantallas y frases en cada idioma, además de la matriz Windows y las pruebas funcionales de seguridad.
