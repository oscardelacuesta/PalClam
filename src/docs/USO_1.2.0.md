# Uso de PalClam 1.2.0

## LED de disco

Está habilitado por defecto como segundo icono de bandeja. Si Windows lo oculta, arrástralo desde los iconos ocultos de la barra. Doble clic abre su configuración; el menú derecho muestra lectura/escritura y espacio libre. El icono principal de PalClam sigue disponible.

Opciones → General → LED de disco permite activar/desactivar, elegir unidad (auto = volumen de Windows), sumar todos los discos físicos o consultar solo el volumen elegido, intervalo 250/500/1000/2000 ms, sensibilidad en KB/s, colores #RRGGBB, pulsación y avisos. Guardar y aplicar persiste los ajustes.

| Estado | Color predeterminado |
|---|---|
| Reposo | Gris #7D8798 |
| Lectura | Verde #42D77D |
| Escritura | Azul #4EA5FF |
| Lectura y escritura | Turquesa #21CDD2 |
| Análisis | Violeta #AA75FF |
| Alerta pendiente | Rojo #FF475A |
| Poco espacio | Ámbar #FFB347 |
| Sin contadores | Gris oscuro #5E6675 |

La prioridad es alerta → espacio → análisis → actividad → reposo. La alerta permanece hasta «Reconocer alerta». El menú distingue una sospecha de ransomware. El análisis incluye archivos, correo y trabajos activos de Guard personal; tener Guard vigilando sin trabajo no equivale a analizar. Los avisos del servicio Windows también mantienen la alerta existente, pero no se inventa un estado de análisis para el servicio si no lo comunica.

El espacio corresponde siempre al volumen elegido, aunque la actividad sume todos los discos físicos. Se actualiza cada 10 segundos. Se avisa si queda igual o menos del porcentaje O de los GB elegidos; poner un criterio a 0 lo desactiva. No se confunde ausencia de datos con disco lleno o reposo. La primera lectura PDH necesita dos muestras. PDH y el espacio se consultan en segundo plano; sin contadores se muestra no disponible.

## Anti-ransomware

Protección → Anti-ransomware, o su acceso desde Opciones → Protección. Añade carpetas concretas de datos (Documentos/Escritorio), exclusiones si procede, y pulsa Guardar e iniciar. Desactivado por defecto; el inicio automático es voluntario y solo funciona mientras PalClam está abierto, incluida la bandeja. No es un servicio independiente.

Por defecto observa 15 s, requiere 20 archivos distintos y al menos 5 señales compatibles con cifrado. Los eventos repetidos de un archivo no cuentan como archivos nuevos. Los renombrados con una extensión añadida o compatible con cifrado y las muestras de contenido muy aleatorio aportan señales; ninguna identifica por sí sola un malware.

El muestreo es opcional: máximo 64 KB del principio del archivo, mínimo 1024 bytes y hasta 20 muestras por segundo. En formatos de texto o nombres compatibles con cifrado se usa entropía de Shannon ≥7,6 bits/byte como indicio. No existe una comparación histórica completa ni un inventario inicial del disco. ZIP, imágenes y documentos comprimidos no se clasifican automáticamente como textos cifrados. Con las mismas extensiones, cifrado lento, parcial, fuera del prefijo o fuera de las carpetas vigiladas pueden pasar inadvertidos.

Con avisos de actividad masiva habilitados, los renombrados/eliminaciones que alcancen el mínimo o los cambios que dupliquen ese mínimo generan advertencia aunque no haya señal de cifrado. Una sospecha de cifrado genera su propio aviso aunque ya haya habido un aviso de actividad. El enfriamiento por categoría es de 60 s. Los umbrales son heurísticos, no garantías de detección; copias, sincronizaciones y cambios legítimos pueden generar falsos positivos.

No bloquea escrituras, termina procesos, desconecta la red, modifica originales, crea señuelos, guarda claves, realiza copias ni descifra archivos. No vigila sectores de disco ni el estado BitLocker. No permite seleccionar la raíz de una unidad ni unidades de red/removibles para esta primera versión. Los enlaces y puntos de reanálisis se omiten. Una carpeta movida o eliminada puede dejar de estar cubierta: revisa los avisos y reinicia tras corregir las rutas.

Cola limitada a 4096 eventos y memoria de correlación limitada. Un desbordamiento de FileSystemWatcher o de la cola se avisa como vigilancia incompleta; no se presenta como funcionamiento correcto. Las muestras ocupadas/inaccesibles se contabilizan. Detenerlo no borra alertas ni cambia la preferencia de inicio automático.

Los avisos quedan en el panel y en el Centro de notificaciones. La sospecha se presenta como advertencia, nunca como detección ClamAV, y activa el rojo del LED. Se conservan localmente dos registros rotados de aproximadamente 1 MB con fecha, categoría, contadores y una ruta de ejemplo; no contenido de archivos. Pueden contener nombres privados. Reconocer la alerta no elimina el historial. Los avisos de Windows dependen de la configuración de notificaciones del sistema.

## Correo de Outlook y otros clientes

PalClam Mail → Origen del correo:

| Origen | Selección |
|---|---|
| EML / MSG / OFT | Mensaje o carpeta exportada |
| MBOX | Archivo de buzón, incluso sin extensión |
| Maildir | Raíz con cur y new; tmp se omite siempre |
| Outlook clásico | Outlook ya abierto y selección nativa de carpeta tras autorizar |

Thunderbird: «Localizar correo de Thunderbird» abre el selector en el directorio de perfiles cuando existe. Elige el archivo real del buzón dentro de Mail/ImapMail; no el índice .msf ni las bases SQLite. No se enumeran ni analizan automáticamente todos los perfiles. Si Thunderbird usa Maildir, selecciona esa modalidad y la carpeta correspondiente. Cierra el cliente para una copia consistente.

MBOX compatible con separadores From de fecha tradicional, conservando los bytes del mensaje salvo la línea de envoltura. No es un lector de buzones binarios de todos los proveedores ni de cada variante histórica MBOX. Si el formato no se reconoce, exporta los mensajes a EML. No se altera el buzón ni se desescapan o normalizan cuerpos. Los cambios de tamaño/fecha observados durante la copia producen incidencia; no se garantiza una instantánea consistente si el cliente sigue escribiendo.

Los nombres temporales son generados y no usan el asunto ni nombres aportados por los mensajes. Los resultados se relacionan con el origen y el número de mensaje. Límites configurados de mensaje/adjunto/cantidad más máximo de importación 512 MB y lectura de MBOX 1 GB. Las importaciones excesivas, inaccesibles, canceladas y parcialmente copiadas quedan como no concluyentes. Se limpian temporales en éxito/error/cancelación; si falla su borrado se muestra su ubicación. Una terminación forzada del proceso puede dejar temporales, como en el módulo anterior.

Outlook clásico: usa la sesión ya abierta, pide elegir una carpeta y exporta copias MSG Unicode. El tamaño y la cuota de copias aceptadas se validan al terminar SaveAs; Outlook controla esa escritura y puede superar temporalmente la estimación previa. No abre una sesión oculta ni solicita contraseña; puede aparecer un aviso de Outlook. El proveedor existente puede descargar contenido, por lo que «análisis local» no implica que Outlook no use su conexión. No cambia leído/no leído, elimina, mueve ni envía mensajes. Incluye carpetas PST/OST montadas accesibles por Outlook; no abre sus archivos directamente. Se toman hasta N elementos en el orden de la colección, sin prometer los más recientes. Los elementos protegidos o no exportables se omiten con incidencia. El selector COM no se puede cancelar desde PalClam: ciérralo primero; la cancelación se comprueba entre mensajes y al acabar SaveAs. No borra temporales mientras una exportación siga activa.

Nuevo Outlook, Outlook.com y Microsoft 365: acceso desde «Outlook / Microsoft 365 (Graph)» con app pública propia registrada en Entra y Mail.Read. Selecciona Entrada/Enviados/Correo no deseado/Archivo/Borradores y 5/10/25/50 mensajes recientes. No hay sincronización completa ni vigilancia continua. Tokens en memoria; permiso de lectura; sin envío, borrado o movimiento de correo. No se añaden servidores IMAP/POP3, contraseñas ni conectores Gmail en esta entrega.

## Verificación Windows pendiente

Ejecuta PRUEBAS.bat y luego CREAR_INSTALADOR.bat. Comprueba LED/PDH, permisos y selector COM, Graph con cuenta autorizada, importación de buzones de prueba y comportamiento bajo ráfagas de archivos. Usa archivos sintéticos; no pruebes ransomware real. Revisa temas, DPI e idiomas y los casos de cancelación/cierre. Conserva evidencia antes de marcar el acta nueva como CONFORME. Publicar sigue bloqueado.

## Referencias técnicas consultadas

- https://learn.microsoft.com/en-us/windows/win32/api/pdh/nf-pdh-pdhaddenglishcountera
- https://learn.microsoft.com/en-us/windows/win32/api/pdh/nf-pdh-pdhgetformattedcountervalue
- https://learn.microsoft.com/en-us/office/vba/api/outlook.olsaveastype
- https://learn.microsoft.com/en-us/office/vba/api/outlook.mailitem.saveas
- https://learn.microsoft.com/en-us/office/vba/api/outlook.namespace.pickfolder
- https://learn.microsoft.com/en-us/office/dev/add-ins/outlook/one-outlook
- https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=net-10.0
- https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.error?view=net-10.0
- https://learn.microsoft.com/en-us/azure/azure-netapp-files/advanced-ransomware-protection (referencia de señales de comportamiento; PalClam no implementa sus modelos ni snapshots)
- https://github.com/advisories/GHSA-846f-f764-76mx
- https://github.com/advisories/GHSA-2fmv-rv2c-59x7
