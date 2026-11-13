# Localización integral de interfaz — Preview 2

## Preferencia y detección

`UiLanguage` admite `auto`, `es`, `en`, `fr`, `de`, `pt` e `it`. Su valor predeterminado ahora es `auto`. `ResolvePreference` usa CurrentUICulture, que en el proceso WPF hereda la lengua de visualización de la sesión de Windows. No cambia CurrentCulture ni CurrentUICulture. Las variantes en-US, fr-CA, de-DE, pt-BR o it-IT se reducen al idioma soportado. Una cultura no soportada recurre a español.

Las configuraciones antiguas sin UiLanguage usan la nueva detección. Una elección explícita previa, incluido `es`, permanece manual y prevalece sobre Windows. Volver a Automático guarda `auto`, no el resultado detectado: el siguiente arranque vuelve a consultar Windows. La escritura se realiza antes de aplicar cambios; los errores mantienen la selección anterior.

## Recursos

Se conservan los recursos nombrados de Preview 1 y se añaden claves `Display.<hash>` para las frases de interfaz. El hash procede de la frase española y su plantilla de campos, de forma que no depende de un número de línea. Las fuentes españolas y sus traducciones están en `PalClam/Localization/Strings*.resx`. Los comentarios indican referencias de inventario. `docs/COBERTURA_LOCALIZACION_1.2.0_PREVIEW2.json` permite localizar cada entrada y sus módulos.

Las plantillas nacen de expresiones de cadena analizadas con Roslyn: literales, interpolaciones y concatenaciones. Los campos contiguos cuya separación no se puede recuperar se agrupan como un dato opaco. `DisplayMetadata.json` marca exclusivamente los argumentos que representan etiquetas de estado conocidas. El resto —rutas, identidad, asunto, hash, texto externo— se conserva.

`DisplayCatalog` resuelve frases exactas y plantillas ancladas completas; no sustituye palabras arbitrarias dentro de datos. Prioriza los patrones con más literal y evita patrones demasiado genéricos. Usa expresiones regulares sin retroceso y con tiempo limitado. Un fallo de formato o presentación conserva el mensaje original y no cancela el análisis. Los informes mixtos pueden traducirse por líneas.

La prueba .NET comprueba todas las plantillas con argumentos de control opacos, cada idioma, parámetros, fallback y serialización. Las traducciones incorporadas se han preparado con asistencia local y revisión de términos/advertencias. No hay traducción por IA durante la ejecución ni dependencia de Internet para mostrar otro idioma. Queda pendiente la revisión visual y lingüística de aceptación.

## Presentación y datos

`PresentationLocalization` instala adaptadores WPF y conserva las cadenas de los modelos. Los TextBlock de interfaz mantienen su texto de origen para cambios de idioma. Las referencias son débiles y los observadores se retiran al descargar controles; se depuran referencias caducadas.

Los controles enlazados a datos de usuario no se traducen automáticamente. Las columnas de estado y descripción tienen adaptadores específicos. En correo se conservan asuntos, rutas y adjuntos. En el Inspector, hashes y nombres son datos opacos. Las respuestas de proveedores de IA no se reescriben. Los enlaces de ayuda se conservan como enlaces; los Run se traducen sin sustituir el Hyperlink.

Las etiquetas visuales de selectores se adaptan mediante enlaces de presentación, manteniendo ItemsSource, SelectedItem, SelectedValue, ItemTemplate y DisplayMemberPath. Las cadenas `Equilibrado`, `Preciso`, ids de temas o perfiles continúan intactas en la configuración y lógica.

Los cuadros MessageBox conservan los botones y la opción predeterminada de seguridad. Los filtros de archivo traducen solo la descripción; no los patrones. Los controles nativos de Windows conservan los textos que proporciona el propio sistema operativo.

La integración opcional de Explorer escribe la etiqueta traducida al activarla. Sus claves, propietario y comando no cambian. Una entrada creada con otro idioma se actualiza al desactivar y volver a activar esa opción; cambiar idioma no reescribe automáticamente el registro.

El cambio de idioma no reconstruye MainWindow ni inicia, pausa, cancela o reinicia análisis, Guard, correo o consultas de IA. La prueba WPF preparada combina idioma/tema y progreso simulado; el ensayo con ClamAV real en Windows sigue siendo obligatorio.

## Mantenimiento

1. Incorporar frases españolas y campos a los recursos; no usar claves de configuración ni valores técnicos como traducciones persistidas.
2. Añadir los cinco recursos de destino y comprobar los parámetros. Para revisar fuentes: `dotnet run --project Tools/LocalizationInventory/LocalizationInventory.csproj -- PalClam inventario.json`.
3. Ejecutar `PalClam.LocalizationTests`, el verificador de recursos y `PRUEBAS.bat` en Windows.
4. Revisar idioma, ventana pequeña, siete temas, DPI 125/150 %, fuentes, textos largos y controles nativos. Las referencias de inventario se deben regenerar tras mover código.

RTL no se incorpora en esta versión de seis idiomas. Los contadores nuevos que requieran reglas lingüísticas de plural específicas deben ampliar sus formas y pruebas; no se cambia la interpretación de las cifras del motor.

## Invariantes

No se modifica la lógica de ClamAV, cuarentena, privilegios de Machine Guard, consentimiento de IA ni firma de actualizaciones. Los estados internos siguen en su representación original y los parsers leen esa representación. El instalador mantiene el AppId y no utiliza su idioma para sobrescribir una elección WPF guardada.
