# PalClam 1.1.0 — aceptación final en Windows

**Objetivo:** permitir cerrar la versión sin declarar que una compilación equivale a un antivirus validado.

## Etapas automáticas

Ejecutar en una carpeta nueva: `PRUEBAS.bat` (5 fases); `CREAR_INSTALADOR.bat`; `VALIDAR_PAQUETE.bat`. Comprobar compilación WPF .NET 10 x64, siete temas y 360 alternancias, smoke tests, compilación/autoprueba del servicio, hashes del instalador y firmas del motor ClamAV. El ZIP de código no incluye el EXE compilado.

## Matriz de ensayo manual (guardar evidencias)

1. **Instalación limpia** en Windows 10/11 x64; acceso directo, antivirus instalado, primera ejecución, opciones guardadas y datos del usuario preservados.
2. **Actualización** sobre RC1 FIX3 y, si procede, sobre 1.0.0; AppId idéntico, configuración/cuarentena no borradas ni sobrescritas; conservar copia de seguridad antes de probar.
3. **Desinstalación**: servicio opcional, tareas programadas, entradas de arranque y Explorer limpios; ningún dato ajeno eliminado. Confirmar política sobre datos locales.
4. **Motor y firmas**: ClamAV y sigtool, base daily/main/bytecode reconocida; FreshClam offline/online/fallo con diagnóstico honesto; sin instalar versiones incompatibles ni llamar vigente a una base ausente.
5. **Análisis**: rápido, completo y personalizado; exclusiones, objetivos duplicados, unidades, lotes, progreso/ETA, pausa/reanudación/detención, prueba de archivo grande y archivo inaccesible; detección con muestra **EICAR oficial no ejecutable** en entorno de pruebas controlado; no usar malware real.
6. **Cuarentena**: confirmación, integridad de ficheros, reanálisis aislado, restauración voluntaria, intentos fallidos, carpeta inaccesible, historial y falsos positivos sin ejecución de muestras.
7. **Guard** reactivo y **Machine Guard** opcional: iniciar/detener/reiniciar, servicio sin privilegios desde WPF, latido antiguo, motor sin firmas, rutas protegidas, inicio sesión, USB Watch y Download Guard sin duplicados.
8. **IA y correo**: proveedor local/externo, consentimiento antes de enviar datos, no enviar rutas por defecto, respuesta de error, sin acciones destructivas; EML/Outlook y avisos etiquetados como heurísticos, no como detección ClamAV.
9. **Diseño**: siete temas y 360 alternancias, escalado al 100/125/150%, combos y botones oscuros, scroll, aurora Suave/Equilibrada/Dinámica/Desactivada, Ayuda con accesos válidos, altos contrastes.
10. **Rendimiento y fiabilidad**: análisis de muchas rutas, consumo de memoria, cancelación, fallos de red, registros extensos, segundo arranque, instalación sin conexión, reinicio de Windows y recuperación del servicio.
11. **Licencias y publicación**: licencia GPLv2, avisos terceros, fuente correspondiente de ClamAV 1.5.4, checksums oficiales/firmas aplicables, hash SHA256 del instalador, firma de código si procede, aprobación expresa del autor.

### Bloqueo final

Marcar cada punto `CONFORME` **solo tras ensayo verificable** en `docs/ACEPTACION_1.1.0_FINAL.ini`. Ejecutar `REVISAR_ENTREGA.bat`; si falla o permanece pendiente, **no distribuir**. `PUBLICAR_ACTUALIZACION.bat` permanece bloqueado deliberadamente: habilitar un canal estable exige un procedimiento de publicación independiente y autorizado.
