# PalClam 1.1.0 Preview 14 FIX1

Corrección visual y de interacción sobre **Preview 14**.

## Incidencias corregidas

1. **Tarjeta del estado del motor**
   - El texto "Repetir análisis" podía quedar recortado en el panel principal.
   - Se aumenta el ancho reservado a la tarjeta derecha y su ajuste en diseño amplio.

2. **Aurora de cabecera**
   - La aurora previa resultaba demasiado estática y difusa.
   - Se sustituye por una composición de **cintas vectoriales animadas** con desplazamiento real, opacidad variable y color dependiente del tema.

3. **Hover no deseado en la marca PalClam**
   - El bloque del escudo y del título estaba implementado como botón, mostrando un efecto visual al pasar el ratón.
   - La interacción se mantiene, pero ahora se presenta con un contenedor neutro sin estilo de botón.

## Archivos modificados

- `PalClam/MainWindow.cs`
- `PalClam/MainWindow.Dashboard.cs`
- `PalClam/MainWindow.HeaderAurora.cs`

## Validación pendiente

- `PRUEBAS.bat` en Windows
- comprobación visual del panel principal
- verificación de la aurora en los seis temas
- comprobación de que la cabecera ya no muestra hover de botón
