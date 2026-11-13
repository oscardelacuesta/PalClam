# PalClam 1.1.0 Preview 6 — Dashboard PRO

## Diseño de la pantalla principal
- Nueva cabecera compacta, sin duplicar el escudo de la barra superior.
- Indicador del motor con estado real y mensajes de acción.
- Banda de diagnóstico con alertas, incidencias y análisis en curso.
- Tres tarjetas con el último estado, detecciones y fecha del análisis; sin cifras ficticias.
- Estado separado de motor, firmas y Guard.
- Solo cuatro accesos directos secundarios; el resto permanece en el menú.
- Navegación a análisis, cuarentena, historial y diagnóstico mediante botones existentes.
- Mantiene los seis temas y la escala de texto.

## Pausa
- Se retira el botón de pausa deshabilitado, que generaba confusión.
- Detener sigue operativo desde arriba y desde la barra fija.
- **No hay pausa real de clamscan**: requiere control de trabajos y pruebas específicas.
- No se suspende un proceso del motor mediante APIs de Windows no documentadas.

## Validación
- Se mantienen las 74 pruebas automáticas de Preview 5 y las verificaciones offline.
- Comprobar en Windows la interfaz de cada tema y 90 %, 100 %, 130 % de texto.
- El canal estable continúa bloqueado.
