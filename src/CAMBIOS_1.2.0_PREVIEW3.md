# PalClam 1.2.0 Preview 3

Base: 1.2.0 Preview 2, confirmada por el autor como compilada y operativa en su equipo. Esta entrega contiene código fuente; el instalador se genera en Windows con los BAT habituales. No es una versión aceptada como estable.

- Segundo icono de bandeja como LED de disco: reposo, lectura, escritura, ambas, análisis, alerta pendiente, poco espacio y contadores no disponibles. Colores, intervalo, sensibilidad, unidad, ámbito de actividad, avisos y pulsación en Opciones → General. Contadores PDH de solo lectura, con nombres ingleses independientes del idioma de Windows; no se infiere actividad a partir de tamaños de archivo.
- Anti-ransomware → Protección: monitor opcional de comportamiento en carpetas locales elegidas, exclusiones, inicio con PalClam, ventana temporal, archivos distintos y señales mínimas. Correlaciona renombrados compatibles con cifrado y muestras de alta aleatoriedad en textos; avisa también de actividad o eliminación masiva si se solicita. No bloquea escrituras ni confirma malware, no analiza sectores de disco ni BitLocker y no descifra archivos.
- Historial anti-ransomware local y rotado, alertas de Windows y rojo del LED ante sospecha. Pérdida de eventos, errores y carpetas sin vigilancia se presentan como cobertura incompleta. No se toman medidas destructivas automáticamente.
- Correo: MBOX tradicional (también archivos sin extensión de Thunderbird), Maildir cur/new y copia desde una carpeta elegida en Outlook clásico ya abierto. Los mensajes originales no se modifican. Se mantienen límites por mensaje, adjunto y número; temporales de importación hasta 512 MB y lectura MBOX hasta 1 GB. Las omisiones invalidan un resultado limpio.
- Microsoft Graph: selección entre Entrada, Enviados, Correo no deseado, Archivo y Borradores. Permiso delegado Mail.Read y tokens solo en memoria. Nuevo Outlook utiliza esta vía; PST/OST se consultan a través de carpetas montadas en Outlook clásico, sin parser directo.
- Recursos nuevos en ES/EN/FR/DE/PT/IT conservando detección de Windows y prioridad de elección manual. Los datos del usuario y las rutas se mantienen opacos.
- Dependencia System.Security.Cryptography.Pkcs fijada en 10.0.14 para resolver los avisos conocidos detectados en la dependencia transitiva del lector MIME.
- Nuevas pruebas portables de políticas, límites, importación, conservación de originales, cancelación y monitor de carpetas con datos sintéticos. No se ejecuta malware.

Mantiene AppId, temas, aurora, separaciones de privilegios, firmas de actualización y la publicación bloqueada. La aceptación nueva está en docs/ACEPTACION_1.2.0_PREVIEW3.ini y no reutiliza la aceptación de versiones previas.
