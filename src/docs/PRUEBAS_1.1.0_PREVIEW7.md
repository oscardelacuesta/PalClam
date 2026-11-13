# Pruebas de aceptación · PalClam 1.1.0 Preview 7

**Previo:** no distribuir esta Preview sin completar estas pruebas en un equipo Windows. Conservar Defender activado y disponer de motor ClamAV y firmas válidas. Usar archivos de prueba inocuos.

## Paso 1 · Compilar e instalar

1. Ejecutar `PRUEBAS.bat`, comprobar los resultados (total esperado: 81).
2. Ejecutar `CREAR_INSTALADOR.bat`, revisar ausencia de errores de C# y de Inno Setup.
3. Ejecutar `REVISAR_ENTREGA.bat` y leer el informe de SHA-256.
4. Actualizar sobre Preview 6 y comprobar que se mantienen las opciones, temas y datos del historial.

## Paso 2 · Firmas

- Entrar en Motor y firmas: con bases `main` y `daily`, mostrar la disponibilidad local y un aviso prudente sobre su vigencia.
- Probar un directorio sin bases completas en perfil limpio: **no debe afirmar que está listo**.
- Simular una incidencia de red de FreshClam: mantener firmas previas y mostrar último error, sin afirmar que están actualizadas.
- Probar comprobación manual con conexión: si FreshClam termina correctamente, mostrar última verificación y eliminar error anterior.
- Verificar que al reabrir sin Internet no reintenta automáticamente cada segundo.

## Paso 3 · Explorador

- Activar integración desde Opciones, verificar estado. En Windows 11 mirar también «Mostrar más opciones».
- Hacer clic derecho sobre un archivo, una carpeta y una unidad y elegir «Analizar con PalClam».
- Confirmar que se abre la pantalla Análisis con el objetivo correcto sin bloquearse por búsqueda de actualizaciones.
- Probar ruta con espacios, fichero que ya no existe y ruta excluida; debe avisar y no analizar rutas incorrectas.
- Desactivar la opción y confirmar que el registro se retira para el usuario actual.

## Paso 4 · Mail

- Analizar un EML/MSG/OFT inocuo de prueba con un adjunto de prueba inocuo.
- Verificar filtro Todos, Detecciones, Requieren atención, No analizados / errores y Avisos de enlaces.
- Probar correo con enlaces de IP: debe etiquetarlos como **aviso heurístico**, nunca como detección confirmada.
- Simular adjunto fuera del límite: debe quedar Omitido y no contarse como limpio.
- Exportar TXT y revisar cuatro grupos de resultados, privacidad de las rutas y la fecha.
- Confirmar que no se mueve, aísla ni borra nada automáticamente.

## Paso 5 · Regresión

- Verificar análisis rápido y personalizado, cuarentena manual, restauración, protección de borrado con confirmación, Guard, Outlook opcional, IA opcional, Self-Defense Monitor, los seis temas y el ajuste de tamaño.
- Comprobar que Detener permanece visible, que no hay botón «Pausar» simulado y que funciona la navegación al pulsar el logo.
- Mantener Microsoft Defender activo; no afirmar protección preventiva real.
