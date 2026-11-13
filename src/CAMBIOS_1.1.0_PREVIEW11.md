# PalClam 1.1.0 Preview 11 — Fase 2: mejora visual del panel principal

Fecha: 11/11/2026. Base: Preview 10 completo. Mismo AppId Inno Setup y persistencia de configuraciones.

## Cambios

1. **Panel responsivo sin recreación.** El héroe del centro de seguridad alterna entre dos columnas y disposición vertical. Las tres métricas del último análisis se apilan en modo compacto; los componentes se apilan cuando el texto necesita anchura. La banda de estado desplaza Diagnóstico a la segunda línea en modo compacto. Se reposicionan las mismas instancias WPF, no se pierde el estado ni se reconstruyen pestañas.
2. **Escala accesible.** Los umbrales de anchura consideran 90–130% de tamaño de texto. La política se comprueba con SmokeTests incluyendo anchos límite y valores inválidos.
3. **Iconos vectoriales.** Motor, firmas y Guard utilizan trazados WPF independientes de fuentes tipográficas y adaptables a los seis temas.
4. **Legibilidad.** Títulos, descripciones y estados del motor permiten ajustar líneas; mejor separación de tarjetas, estado visual con punto de color dibujado y no con fuente emoji.
5. **Estabilidad.** El motor de ClamAV, Guard, cuarentena, correo, IA, escaneo, barra flotante, mecanismo de temas de Preview 10 y su test WPF de 360 alternancias permanecen sin cambios funcionales.
6. **Identidad de la compilación.** 1.1.0-preview.11 en csproj, fuentes y scripts; Inno conserva el AppId previo.

## Límites

No se ha ejecutado .NET 10/WPF ni ClamAV en el entorno Linux de preparación. Requiere ejecutar `PRUEBAS.bat`, probar tamaños de ventana en Windows 10/11, los seis temas y un análisis real antes de distribuir. Guard continúa siendo vigilancia reactiva, no bloqueo preventivo.
