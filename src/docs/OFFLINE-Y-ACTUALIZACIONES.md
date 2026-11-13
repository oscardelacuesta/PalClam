# Estrategia offline / online

**Instalación offline:** el BAT de publicación descarga ClamAV y las bases oficiales mediante FreshClam en el equipo de desarrollo. Los archivos están incluidos en el instalador Inno Setup; este no descarga componentes durante la instalación.

**Primer arranque:** PalClam detecta el motor que se instala en `{app}\ClamAV` y copia las bases incluidas desde `{app}\OfflineSignatures` a `%LOCALAPPDATA%\PalClam\Signatures`, sin reemplazar firmas posteriores. Puede analizar inmediatamente.

**Con conexión:** consulta la última versión estable de ClamAV. Si es más reciente que el motor efectivo (base o versión descargada) la descarga desde GitHub oficial, verifica el hash disponible en el asset, extrae de forma controlada, prueba `clamscan --version` y activa la nueva. Si falla o no hay Internet, sigue funcionando el motor de respaldo. FreshClam actualiza firmas por separado.

**Reinstalación o actualización de PalClam:** no debe reemplazar una base local más reciente. El motor descargado y las firmas se almacenan en el perfil, fuera del directorio del instalador. Conservar los datos de usuario al desinstalar es deliberado; permitir borrado voluntario en versión futura.

**Advertencia:** ClamAV puede no detectar todo el malware, especialmente si la base offline está desfasada. No vender como protección residente. No distribuir públicamente el instalador hasta revisar derechos de redistribución, licencias y pruebas de instalación.
