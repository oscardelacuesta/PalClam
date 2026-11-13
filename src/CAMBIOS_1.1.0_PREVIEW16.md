# PalClam 1.1.0 Preview 16 — Machine Guard: estado, firmas y recuperación

Base: Preview 15. Mantiene el instalador Inno Setup, Guard personal, el servicio
experimental independiente, el Inspector, la IA, los seis temas y la aurora.

## Cambios realizados

1. **Estado dinámico en WPF**: consulta de `estado.json` cada 10 segundos
   cuando se muestra la sección, y comprobación de latido reciente. No existe
   canal de comandos privilegiados desde la interfaz, solo lectura. El JSON
   se lee de forma tolerante a escrituras simultáneas y al esquema anterior.
2. **Indicadores conservadores**: análisis, detecciones, errores, omitidos,
   eventos perdidos, pendientes y revisiones de recuperación. Un latido viejo,
   una revisión en curso o un error NO se consideran vigilancia saludable.
3. **Firmas comunes**: `ProgramData\PalClam\MachineGuard\Signatures`, en vez
   de cargar directamente bases de la carpeta de instalación. La instalación
   y renovación requieren autorización de administrador. Se validan con
   `sigtool --info` y SHA-256, y se cambian con el servicio detenido.
4. **Evitar retrocesos**: si una base compartida tiene fecha más reciente que
   su equivalente del instalador, se rechaza la copia. Esto NO significa
   comprobar la versión oficial más reciente ni actualizar desde Internet.
5. **Recuperación limitada**: tras arrancar el servicio o perder eventos se
   enumera la carpeta observada, omitiendo reparse points/junctions. Se limita
   a 2.000 archivos en cola y NO garantiza que se hayan analizado todos.
6. **SCM**: configuración de reinicio ante fallos, con demora; se conservan
   los estados de error y avisos de pérdida de eventos.
7. **Paquete corregido**: retirada una referencia en Inno Setup a un documento
   de Preview 15 FIX2 que no existía en los fuentes facilitados.
8. **Pruebas nuevas**: JSON corrupto, servicio degradado, latido antiguo y
   métricas de estado; compilación independiente del servicio.

## Límites expresos

- Guard sigue siendo reactivo. No bloquea ejecuciones ni sustituye Defender.
- La carpeta vigilada sigue siendo únicamente `ProgramData\PalClam\GuardWatch`.
- La renovación de firmas toma bases del **instalador local**; no lanza FreshClam
  ni actualiza Internet de forma automática. Un instalador antiguo no se
  convierte por ello en firmas nuevas.
- La revisión de recuperación es de mejor esfuerzo; si falla, se satura,
  encuentra enlaces, archivos inestables o supera el límite, se indica que
  hace falta un análisis completo manual.
- La instalación y el reinicio del servicio requieren privilegios de
  administrador y una instalación compartida en Program Files.
- No se ha compilado ni ejecutado WPF/SCM en este entorno Linux. No distribuir
  sin superar `PRUEBAS.bat` y la batería manual indicada en docs.
