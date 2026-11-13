# PalClam 1.1.0 Preview 10 — Fase 1: estabilidad al cambiar temas

Fecha: 11/11/2026. Base: proyecto completo 1.1.0 Preview 9 FIX3.

## Objetivo

Evitar los cierres o bloqueos al alternar temas mientras se muestran resultados de análisis y conservar las seis pestañas de Opciones, el progreso, la barra flotante y los ajustes existentes.

## Implementación

- **Pestañas sin recreación:** `MainWindow.OptionsTabs.cs` mantiene la misma plantilla y los mismos `TabItem`, por lo que el cambio de color no desmonta controles ni reemplaza `Style` en caliente.
- **Aislamiento de Freezables:** `PalClam/UI/OptionsTabAppearance.cs` no inserta pinceles mutables globales en `Style`/`Setter`/`Trigger`; asigna a cada pestaña copias locales congeladas de su color actual. Los cambios de color de MainWindow no las modifican, sino que sustituyen sus valores de forma explícita y controlada.
- **Estados visuales:** selección, foco de teclado y hover conservados. Indicador inferior de selección e iconos vectoriales de Preview 9 FIX3 permanecen intactos.
- **Cambio de paleta más seguro:** `MainWindow.Appearance.cs` precalcula todos los colores antes de mutar la paleta en vivo, valida pinceles y degradado no congelados, y conserva la reversión y el diagnóstico local de FIX3.
- **Menos repintados:** al seleccionar un nuevo tema solo se actualizan los tres colores locales de las seis pestañas; no se regeneran sus estilos ni contenidos. Un cambio solo de color no vuelve a escalar las fuentes ni detiene ClamAV.
- **Prueba WPF reproducible:** el nuevo `PalClam.ThemeTests` ejecuta 360 cambios entre los seis temas, actualiza resultados y porcentaje simulado, y comprueba selección y colores sobre una ventana WPF real. Se incluye en `PRUEBAS.bat`.
- **Versionado:** 1.1.0-preview.10, nombre del instalador Preview10, mismo AppId de Inno Setup. Las configuraciones del perfil no se migran ni borran.

## Alcance y limitaciones

El motor ClamAV y su política de detección no han sido modificados. Conservamos Guard, firmas, correo, Outlook, IA, cuarentena, historial, análisis programados, pausas por lotes y barra flotante. El nuevo proyecto de prueba de WPF **simula actualizaciones de progreso**: no sustituye el análisis ClamAV real ni prueba Windows Forms/NotifyIcon. No se ha podido ejecutar .NET 10/WPF en el entorno de preparación (Linux, sin SDK); deben ejecutarse `PRUEBAS.bat` y la aceptación manual sobre Windows antes de instalar, distribuir o afirmar que el fallo original está definitivamente solucionado.

## Diagnóstico si hay incidencias

El registro de excepciones continúa en `%LOCALAPPDATA%\PalClam\Diagnostics\fallos.log` (sin envío automático). Para reproducir: iniciar un análisis de carpeta y alternar los seis temas desde cabecera y Opciones; comprobar que el proceso sigue en ejecución, que los contadores avanzan y que los resultados no se pierden. Si se cierra, adjuntar el registro (revisándolo antes por privacidad) y la salida de `PRUEBAS.bat`.
