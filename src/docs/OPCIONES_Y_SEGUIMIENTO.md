# PalClam 0.8.0 — Opciones y seguimiento

## Uso

El menú «Opciones» contiene ajustes de análisis, actualizaciones, presentación, exclusiones y acceso a los datos locales.
Se guardan como JSON en `%LOCALAPPDATA%\PalClam\settings.json` y se cargan al iniciar.

- **Archivos comprimidos:** permite el análisis interno de formatos soportados mediante `--scan-archive=yes|no`. Activado por defecto.
- **PUA:** pasa `--detect-pua=yes` a `clamscan`. Desactivado por defecto: puede producir falsos positivos.
- **Mostrar archivos normales:** filtra la tabla de resultados; los contadores y el informe detallado no se alteran.
- **Consola técnica visible:** la sección de resultados abre la consola plegable.
- **Actualizaciones:** búsqueda de versiones de PalClam, instalación automática optativa, actualización del motor y FreshClam. Frecuencia de 6, 12 o 24 horas para las comprobaciones periódicas de motor y firmas (si la aplicación permanece abierta). El primer arranque puede efectuar comprobaciones por separado.
- **Exclusiones:** añadir una ruta absoluta de fichero o directorio por línea. No se admiten comodines ni raíces de unidad. Pulsa «Guardar exclusiones» y verifica su comportamiento con un análisis de prueba.

## Límites del progreso

ClamAV puede procesar contenido dentro de contenedores, omitir archivos inaccesibles o manejar límites internos, lo que no corresponde uno a uno con la enumeración previa de archivos visibles. La barra, cuando se puede mostrar, compara archivos informados con una estimación local y se limita al 99% durante el análisis. En otros casos se presenta indeterminada. El elemento mostrado es el **último archivo del que ClamAV terminó de informar**, no siempre el archivo activo en ese instante. El resumen del motor tiene prioridad sobre el recuento de líneas individuales.

En caso de cancelación o errores, PalClam no ofrece un resultado «limpio» ni marca el 100% como éxito. La tabla retiene hasta 350 resultados recientes, priorizando detecciones e incidencias; el informe TXT guarda el detalle completo si la escritura es posible.

## Seguridad

PalClam es un escáner bajo demanda y no ofrece protección residente. No borra ni mueve archivos, no activa la cuarentena en esta versión y no desactiva Microsoft Defender. Las exclusiones reducen la cobertura y los cambios no afectan a un análisis ya iniciado.

## Comprobaciones recomendadas en Windows

1. Ejecuta `COMPILAR.bat` con SDK .NET 10; confirma que aparece `PalClam.exe`.
2. Ejecuta `CREAR_INSTALADOR.bat` con Inno Setup y conexión para preparar el motor/firmas offline.
3. Revisa las seis páginas, cambia preferencias y reinicia para comprobar persistencia.
4. Analiza una carpeta con archivos inocuos y comprueba el seguimiento, filtros y TXT.
5. Interrumpe un escaneo grande: la pantalla debe indicar que el análisis está incompleto.
6. Prueba exclusiones en una carpeta de ensayo. Evita excluir rutas críticas; si no funcionan con tu motor, quítalas.
7. Comprueba motor/firmas instalados y actualizaciones usando una conexión controlada.

**Esta entrega ha pasado comprobaciones estáticas, pero no se ha compilado ni ejecutado en Windows en el entorno de creación.**
