# PalClam 1.1.0 RC1 · cierre visual FIX2 · aurora configurable

## Mejoras
- Aurora más visible por defecto: **Equilibrada** (1,65× los ciclos anteriores).
- Opciones → Apariencia → **Velocidad de la aurora**: Suave (1×), Equilibrada (1,65×), Dinámica (2,3×).
- Guardado persistente en `settings.json`. Preferencias antiguas sin este campo usan Equilibrada.
- Aplicación inmediata sin afectar al proceso ClamAV; minimización y alto contraste suspenden la animación.
- Nuevo estilo WPF de `ComboBox` y menú desplegable, incluido el modo editable del modelo IA, que utiliza colores dinámicos y evita el blanco nativo.
- Botón claro del panel y lista de resultados siguen la paleta; mejora de contraste de textos del hero oscuro.
- Ayuda actualizada con cuatro primeros pasos y explicación de apariencia y aurora.

## Verificación en Windows (pendiente)
1. Ejecutar `PRUEBAS.bat` desde una carpeta nueva, comprobar fases 1 a 5.
2. Cambiar a Oscuro violeta, revisar los ComboBox de Opciones, Smart Scan, filtros y modelo IA editable; comprobar teclas y rueda.
3. Seleccionar Suave / Equilibrada / Dinámica y comprobar el movimiento de la aurora sin parpadeo; minimizar y restaurar.
4. Reiniciar PalClam y comprobar persistencia; desactivar y reactivar la aurora.
5. Revisar botón principal del Dashboard y legibilidad del texto de cabecera.
6. Abrir Ayuda y verificar navegación de los enlaces.
7. Lanzar un análisis local mientras se cambia el aspecto para comprobar que sigue en curso.

Nota: el paquete contiene fuentes y scripts, no un ejecutable compilado. No distribuir como estable sin probar en Windows.
