# Arquitectura de localización — 1.2.0 Preview 1

## Recursos y resolución

`PalClam/Localization/Strings.resx` contiene el español neutro. Los satélites `.en`, `.fr`, `.de`, `.pt` e `.it` tienen las mismas 37 claves. `NeutralLanguage=es` evita que Windows decida el idioma de fallback.

`TextCatalog` es independiente de WPF. Normaliza el idioma contra una lista cerrada y consulta ResourceManager con cultura explícita y sin búsqueda automática de padres. Si falta un valor o un satélite, consulta el recurso español neutro. `Resolve` expone el idioma utilizado y si hubo fallback; no registra textos de usuario. Los identificadores desconocidos producen un marcador visible.

Los textos parametrizados usan `{0}`. Se comprueban claves, duplicados, valores vacíos y parámetros en los seis recursos. `Count` reserva la convención `.One` para 1 y `.Other` para otros enteros; todavía no se usa en la interfaz. Antes de localizar contadores con reglas lingüísticas adicionales deben ampliarse sus formas y pruebas, incluyendo cero y valores grandes. Los fixtures de pruebas contienen claves deliberadamente ausentes en inglés, sin introducirlas en los recursos del producto.

## Interfaz y estado

`UiText` implementa un indexador público e INotifyPropertyChanged. `LocalizedUi.Bind` enlaza una clave a una propiedad de dependencia existente; los enlaces WPF evitan suscripciones globales fuertes a ventanas. Los parámetros no se usan como claves y se conservan sin traducir.

`MainWindow.Localization.cs` reúne el selector y los constructores de texto/botón traducidos. `LT` conserva tamaños, pinceles y márgenes de `T`. Los botones mantienen el estilo existente y usan TextBlock con ajuste de líneas.

La selección se guarda con `SettingsStore.Save` antes de notificar el cambio. Los errores de escritura restituyen la selección anterior. La carga normaliza `UiLanguage`; si falta, el valor predeterminado es `es`. No se cambia CurrentCulture ni CurrentUICulture: los analizadores, comandos, fechas técnicas y números del motor conservan su comportamiento.

Los estados internos de análisis siguen siendo los existentes. Solo su indicador global elige una clave de presentación y mantiene el Binding si esa clave no cambia. Cambiar idioma no llama a BuildLayout, Navigate, cancelación, pausa ni configuración del motor. Esta separación requiere, además, la prueba manual con ClamAV real indicada en el plan Windows.

## Cobertura de esta entrega

| Grupo | Cobertura |
| --- | --- |
| Cabecera | Lema, Ayuda, Ajustes, accesibilidad, tooltips y cinco estados de análisis |
| Ajustes | Título, introducción, encabezado visual, selector, mensajes y seis títulos de pestaña |
| Ayuda | Presentación, dos accesos iniciales, primeros pasos, orientación visual y enlace Ver función |
| Pendiente | Navegación, contenido detallado de Ayuda, opciones restantes, Smart Scan, resto de módulos, errores, correo, IA e informes |

El aviso de traducción parcial aparece junto al selector. Los nombres técnicos, ids de página/tema/perfil, rutas, claves JSON previas, comandos, argumentos, hashes, proveedores y URLs no se traducen. Se añade únicamente la clave de configuración `UiLanguage`.

## Siguientes fases

1. Validar esta base en Windows: compilación, satélites, persistencia, Binding y análisis activo. Resolver cualquier regresión antes de ampliar cobertura.
2. Localizar navegación y Smart Scan mediante claves explícitas. Conservar ids y estado de controles; añadir pruebas de textos dinámicos.
3. Migrar el catálogo completo de Ayuda y los módulos de motor/firmas, cuarentena, Guard, correo, diagnóstico e IA por separado. Mantener los mensajes de máquina separados de su presentación.
4. Completar traducciones y QA visual: seis idiomas, siete temas, ventana mínima, texto largo y DPI 125/150 %. RTL futuro requiere pruebas propias; no se declara compatible en esta entrega.

## Construcción y entrega

El proyecto WPF incluye automáticamente los `.resx`. Los proyectos de pruebas enlazan las mismas fuentes y recursos, con el nombre de manifiesto `PalClam.Localization.Strings`. La publicación autocontenida conserva los recursos y el instalador recoge recursivamente la salida. Inno Setup mantiene el AppId y su elección de idioma es independiente de la preferencia WPF; sus mensajes propios no se amplían en esta primera fase.

Se conserva la documentación histórica de aceptación 1.1.0. `REVISAR_ENTREGA.bat` consulta el acta nueva `ACEPTACION_1.2.0_PREVIEW1.ini` mediante `VALIDAR_ENTREGA_120.bat`; la anterior no acredita esta versión. Publicación estable bloqueada, aunque los ensayos automáticos terminen correctamente.
