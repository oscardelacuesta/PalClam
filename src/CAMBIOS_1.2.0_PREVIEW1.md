# PalClam 1.2.0 Preview 1 — primera fase multilenguaje

Fecha: 12 de noviembre de 2026. Base: PalClam 1.1.0 Cierre Visual FIX3.

## Implementado

- Recursos `.resx` ES/EN/FR/DE/PT/IT: 37 claves, 222 textos. Español neutro y predeterminado; fallback explícito a español. Una clave inexistente se muestra como `⟦clave⟧` para poder detectarla.
- Selector en Ajustes → Apariencia. `UiLanguage` es la única preferencia nueva. Los perfiles anteriores sin esta clave usan español; las culturas regionales soportadas se normalizan.
- Cambio en vivo mediante Binding e INotifyPropertyChanged. Se actualizan los controles existentes, también en páginas ocultas. No se reconstruyen vistas ni se cambia la cultura de proceso.
- Cabecera y estados globales de análisis, seis títulos de pestaña, entrada de Ajustes, introducción de Ayuda y sus enlaces iniciales localizados. El aviso de cobertura parcial se muestra en los seis idiomas.
- Persistencia antes de aplicar el idioma. Si falla la escritura, se conserva la selección anterior y se registra un diagnóstico local.
- Pruebas .NET de recursos, fallback, formatos y serialización; prueba WPF ampliada a seis idiomas y siete temas con progreso simulado. Conectadas a `PRUEBAS.bat` (seis fases).
- Versiones, scripts e Inno Setup adaptados a 1.2.0 Preview 1. AppId original conservado. Nuevo acta y verificador de aceptación; publicación bloqueada.

## Alcance y verificación

Traducción inicial, **no completa**. Navegación general, catálogo detallado de Ayuda, módulos especializados, correos, mensajes de IA, diagnósticos y la mayoría de las opciones continúan en español.

Se han ejecutado comprobaciones estáticas con Python. Este entorno Linux no dispone de SDK .NET, WPF ni PowerShell: **no se ha compilado ni ejecutado el código C# o los BAT/PS1** y no se ha generado instalador. Las pruebas Windows y de ClamAV siguen pendientes.

Los archivos de motor, firmas, actualizaciones firmadas, consentimiento IA, cuarentena y servicio no cambian su lógica. La versión del proyecto del servicio se alinea con 1.2.0; su código queda intacto. Los catálogos de siete temas y aurora también permanecen intactos. Guard sigue siendo reactivo y no bloquea preventivamente.

Detalles: `docs/LOCALIZACION_1.2.0.md`, `docs/PRUEBAS_1.2.0_PREVIEW1.md` y `docs/RESULTADOS_1.2.0_PREVIEW1.md`.
