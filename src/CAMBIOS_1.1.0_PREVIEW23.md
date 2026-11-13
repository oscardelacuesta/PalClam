# PalClam 1.1.0 Preview 23 — Security Dashboard 2.0

## Diseño y mejoras
- El panel de inicio conserva la cabecera y los accesos principales, pero ahora integra cinco tarjetas: motor, firmas, Guard personal, Machine Guard y USB/Descargas.
- Diseño adaptable de las cinco tarjetas (3 / 2 / 1 columnas) según ancho y escala de texto, sin recrear los controles al cambiar de tema.
- Nueva sección «Actividad de seguridad» con resumen **limitado a los últimos 30 análisis válidos**, su resultado, detecciones comunicadas, incidencias y tiempos de los seis más recientes.
- Gráfico de duración basado exclusivamente en `Seconds` del historial existente, sin estimar porcentajes de protección.
- CPU, RAM y número de muestras solo cuando el historial contiene telemetría real; no extrapola uso de disco ni cobertura.
- Hasta 80 avisos recientes de Guard personal, Machine Guard y USB Watch. Distingue avisos sin revisar, niveles y fuentes; los registros no se borran.
- El estado de Machine Guard se confirma con su registro de servicio y latido reciente; no se anuncia vigilancia por el simple hecho de existir un fichero de estado.
- Las lecturas de historial son de solo lectura, acotadas a 1 MB, en segundo plano, y se refrescan como máximo cada 20 s con el Dashboard visible. Las consultas de avisos usan los límites de SecurityNoticeFeed.
- Errores, pérdidas de datos o archivos inaccesibles se muestran como información parcial.
- Se conservan exactamente ClamAV, ClamAV Service, Smart Scan, cuarentena, IA, seis temas y funciones anteriores.

## Precauciones
PalClam sigue siendo un analizador bajo demanda y una vigilancia reactiva opcional; no sustituye Windows Defender, no intercepta ejecución ni bloquea malware preventivamente.

## Pruebas
- Se añaden seis casos de prueba de dashboard al proyecto SmokeTests: resultados reales, recuento de avisos revisados, estado vacío, JSON incompleto y ausencia de historial.
- Validación estática de cableado, lectura acotada, interfaz, pruebas e instalador.
- Falta compilar y ejecutar en Windows 10/11 con SDK .NET 10 y verificar el instalador Inno Setup.
