# PalClam 1.1.0 Preview 22 — Smart Scan PRO

Evolución incremental sobre Preview 21. Sin eliminación ni cuarentena automática.

- Análisis rápido: Descargas, Escritorio, Documentos e Inicio del usuario si están disponibles. No equivale a un análisis completo.
- Análisis completo: unidades locales fijas que Windows clasifica como disponibles. Solicita confirmación antes de empezar. No incluye unidades de red ni soportes USB.
- Personalizado: archivo/carpeta elegida; prioridad Económico / Equilibrado / Prioritario, con opciones de pausa Preciso/Equilibrado existentes.
- Un solo motor de exploración por sesión y lotes oficiales de ClamAV; elimina objetivos duplicados y rutas contenidas en otras seleccionadas.
- Estimación agregada hasta 500.000 archivos, limitada y no concluyente ante errores de enumeración; ETA se oculta cuando la referencia es imprecisa.
- Se conserva la pausa cooperativa entre lotes, cancelación, exclusiones y telemetría del motor, diferenciada de bytes reales de disco.
- El historial registra el preset, prioridad, raíces reales y configuración. El botón Reintentar conserva el plan completo anterior.
- Pruebas de política para perfiles, duplicados, límite de raíces, estimación agregada y exclusiones.

**Sin cobertura total garantizada:** accesos denegados, archivos bloqueados, archivos comprimidos y rutas excluidas pueden limitar el análisis. ClamAV no bloquea preventivamente archivos en Windows.

**Pendiente:** Compilación .NET 10/WPF, pruebas físicas con unidades y validación de Inno Setup en Windows. Ejecutar `PRUEBAS.bat` antes de crear el instalador.
