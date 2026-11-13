# PalClam 1.2.2 — autoría, licencias, privacidad y alcance

**Autor:** Óscar de la Cuesta Campillo — palentino.es. Web del producto: https://palclam.com/.

El código propio de esta entrega se distribuye bajo **GNU GPL versión 3 o posterior (GPL-3.0-or-later)**. Texto íntegro: LICENSE-GPL-3.0.txt y COPYING. Aviso de copyright, permisos y ausencia de garantía: COPYRIGHT.md. El código de la interfaz, el servicio y los scripts incorpora declaraciones SPDX; los ensamblados publicados incluyen la licencia completa y sus metadatos.

## Componentes independientes y bibliotecas

ClamAV, clamscan, freshclam y clamd conservan GPLv2. PalClam utiliza procesos independientes y el protocolo externo de clamd; no enlaza con libclamav. Sus binarios originales y su fuente correspondiente se acompañan sin cambiar su licencia. Redistribuir programas separados no convierte el motor GPLv2 en GPLv3.

Las once bibliotecas NuGet conservan sus condiciones. Para OpenMcdf se aplica MPL-2.0 y la distribución adicional bajo GPLv3 de la sección 3.3, sin modificar su fuente ni avisos originales. UTF.Unknown se utiliza bajo GPLv3 a partir de su opción original GPLv2-o-posterior. El resto mantiene sus avisos MIT y de componentes incorporados. El detalle, textos y fuentes están en Licenses, Source/Libraries y THIRD-PARTY-NOTICES.md.

El runtime .NET autocontenido conserva las licencias recuperadas de los paquetes realmente usados. Inno Setup conserva su licencia propia. Las marcas de terceros pertenecen a sus titulares; PalClam es un proyecto independiente sin afiliación, patrocinio ni certificación de Cisco, Talos o Microsoft.

## Redistribución y datos

Entrega las licencias y fuentes correspondientes con los paquetes, o proporciona acceso equivalente a las fuentes junto a las descargas de la versión exacta. El proceso actual las incorpora en Source y Licenses, tanto en instaladores como en portables. No se incluyen claves privadas de firma ni información personal.

Historial, preferencias, informes y cuarentena se guardan en el perfil de Windows en la edición instalada y en Data en la portable. Las claves IA utilizan DPAPI del usuario de Windows. Las actualizaciones acceden a Internet; Outlook/Graph requiere autorización para los mensajes seleccionados y PalClam AI requiere el consentimiento correspondiente antes de enviar texto a una API remota.

Guard y el monitor anti-ransomware observan cambios y comportamiento; no proporcionan bloqueo preventivo ni descifrado. Un análisis cancelado o incompleto no demuestra que los archivos estén limpios. La aceptación sobre interfaz, Outlook, instalación y licencias de las salidas finales se documenta en ACEPTACION_1.2.2.ini. Los documentos anteriores son históricos y no cambian la licencia de esta entrega.
