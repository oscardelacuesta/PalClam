# PalClam 1.0.0 RC3 — Consolidación y seguridad del actualizador

La RC3 conserva el diseño de RC2, el motor ClamAV de respaldo offline y los módulos
Guard, Mail, Outlook, cuarentena, análisis programados y iconos residentes.
No incluye IA ni introduce módulos nuevos.

## Cambios concretos

1. Los metadatos firmados de actualización se validan mediante una función única.
   La URL debe coincidir EXACTAMENTE con la ruta oficial y el nombre del instalador
   para la versión declarada. No se aceptan otros dominios, redirecciones, parámetros,
   fragmentos, versiones RC ni rutas alternativas.
2. La verificación se repite ANTES de descargar, aunque ya se hubiese comprobado
   el manifiesto durante la consulta de actualizaciones.
3. Se añaden ocho pruebas de regresión offline al paquete de pruebas automáticas.
4. El informe del historial toma la versión de la aplicación instalada en lugar de
   mostrar siempre la denominación antigua RC1.
5. Los BAT y el instalador reflejan la versión RC3; se conserva el AppId existente
   para permitir actualizar sobre RC1/RC2 sin cambiar el identificador de producto.
6. El canal de publicación estable sigue bloqueado para impedir distribuir una RC
   por error como 1.0 definitiva.

## Limitaciones de validación

No se ha podido compilar ni ejecutar el proyecto WPF ni Inno Setup en el entorno
Linux en que se prepararon los fuentes. PRUEBAS.bat y las pruebas manuales de
Windows siguen siendo obligatorias. Esta RC3 NO es la versión estable.
