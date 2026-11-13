# PalClam 1.1.0 Preview 9 FIX3 — Estabilidad de temas durante el escaneo

Fecha: 11/11/2026 · Revisión técnica de Preview 9 FIX2 (190 archivos).

## Incidencia reportada

Al cambiar el tema de color mientras ClamAV analiza archivos, la ventana de PalClam se cierra. No se dispone de una traza del fallo del equipo, por lo que el diagnóstico es una **causa de código probable**, no una reproducción confirmada en Windows.

## Corrección principal

- `MainWindow.OptionsTabs.cs`: las propiedades de color de `Setter` y `Trigger` utilizan ahora `CloneCurrentValue()`. WPF sella los estilos y puede congelar sus objetos `Freezable`; congelar el pincel compartido impedía modificar `Color` en caliente. Las pestañas renuevan su estilo al cambiar de paleta.
- Rediseño de **Opciones**: seis pestañas con iconos vectoriales de línea, chips suaves, pestaña activa con subrayado fino, hover, foco de teclado y disposición que pasa a varias filas en ventanas estrechas. No depende de fuentes de iconos ni emojis; conserva eventos y ajustes existentes.
- `MainWindow.cs`: al restablecer preferencias se agrupan los cambios de tema y tamaño de fuente para evitar repintados/guardados intermedios y registrar un fallo inesperado.
- `MainWindow.Appearance.cs`: comprobar pinceles congelados antes de mutar; aplicar colores sobre instantánea de la colección; evitar operaciones redundantes de cambio de escala si solo se ha seleccionado otro color; bloquear reentrancia de eventos; revertir preferencias en caso de error y registrar la excepción **sin cancelar el análisis**.
- `FloatingScanWindow.cs`: minimizar el trabajo de `ApplyAppearance` al actualizar métricas cada 250 ms; los colores compartidos se actualizan sin reasignar las propiedades de la ventana en cada tick. El cambio real de escala sí actualiza el monitor.
- `Program.cs` y nuevo `Services/CrashDiagnostics.cs`: registro local de errores de WPF y tareas, limitado en tamaño, sin tráfico de red. No se fuerza a continuar tras una excepción fatal arbitraria.
- `Tools/VerificarFuentes.ps1`: comprobaciones de regresión para bloquear estilos que congelan pinceles, exigir diagnóstico y verificar las seis pestañas con iconos, foco y selección visibles.
- `docs/PRUEBAS_1.1.0_PREVIEW9.md`: plan de reproducción durante un análisis y verificación de los seis temas, barras, pausa y tamaño.

## Compatibilidad y alcance

Permanece `1.1.0-preview.9` y el mismo `AppId` del instalador. Se conservan motor ClamAV, bases de firmas, Guard, IA, correo, Outlook, cuarentena y scripts existentes. No cambia el formato de configuración ni los datos del usuario.

**Importante:** la compilación WPF, la prueba interactiva del cambio de tema durante el escaneo y el instalador Inno requieren un equipo Windows con SDK .NET 10 y no están validados todavía en este entorno. Ejecutar `PRUEBAS.bat` y el plan de pruebas anterior antes de instalar o distribuir.
