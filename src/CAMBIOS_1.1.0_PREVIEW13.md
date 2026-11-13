# PalClam 1.1.0 Preview 13 — Inspector de archivos

Continuación de Preview 12; se conservan ClamAV, temas, opciones, Guard, IA, correo, Outlook, historial, cuarentena y barra flotante.

## Funcionalidad nueva

- Nueva sección **Inspector de archivos** desde el menú lateral.
- Selección de un archivo con Examinar o arrastrándolo sobre la ventana mientras está abierta esta sección.
- Metadatos: nombre, tamaño, extensión *declarada* (no es detección de tipo real), fechas de creación y modificación.
- SHA-256 del contenido mediante lectura asíncrona y secuencial. Permite cancelar, no lee el archivo completo en memoria, y avisa cuando el archivo cambia durante la inspección.
- Identificación básica del **certificado Authenticode embebido**, sin validar firma criptográfica del archivo, cadena de confianza ni revocación. No se debe interpretar como "firma válida" o "archivo seguro". Otros formatos de firma (PDF, etc.) no están cubiertos.
- Advertencia heurística ante algunos nombres de doble extensión, expresamente **no** una detección de malware.
- Botón **Analizar con ClamAV** que reutiliza el motor y flujo de análisis de Preview 12, sin introducir un segundo ejecutor concurrente. Recupera el resultado de historial recién generado y lo muestra en la vista del Inspector; el informe completo sigue estando disponible desde la misma sección.
- Ninguna acción automática de aislamiento o eliminación. No se ejecuta el archivo inspeccionado.
- Pruebas SmokeTests añadidas para SHA-256 conocido, metadatos, cancelación, carpetas y doble extensión.

## Correcciones complementarias

- Se preservan los seis temas y los controles WPF sin reconstruirlos al cambiar de tema.
- Corregido `GENERAR_SOLO_INSTALADOR.bat`: el nombre del instalador ahora coincide con `Installer/PalClam.iss`. Antes incluía un espacio espurio en `Preview 12`.
- Identificador de Inno Setup inalterado para mantener la actualización sobre versiones anteriores.
- Documentación, versión, comprobaciones de fuentes y validación de paquete adaptadas a Preview 13.

## Límites y validación

- No se ha ejecutado una compilación real WPF ni Inno Setup en este entorno (sin Windows/.NET SDK).
- Tampoco se ha validado la lectura de certificados Authenticode en un sistema Windows ni se ha ejecutado un análisis ClamAV real con esta Preview.
- La lectura de un archivo localizado en una unidad de red implica el tráfico correspondiente a esa unidad; PalClam no lo sube a servicios de IA por abrir el Inspector.
- El certificado presente y su fecha de vigencia NO demuestran que una firma esté verificada, ni que un ejecutable sea seguro.
- Consultar `docs/PRUEBAS_1.1.0_PREVIEW13.md` antes de distribuir.
