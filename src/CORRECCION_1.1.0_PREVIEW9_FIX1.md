# PalClam 1.1.0 Preview 9 FIX1 — Compilación y selector rápido de temas

## Origen del error

El SDK de Windows permite compilar WPF y Windows Forms para `NotifyIcon`. Ambos exponen
`TabControl`, por lo que `MainWindow.OptionsTabs.cs` provocaba `CS0104`.

**Corrección:** los dos usos del tipo ahora especifican
`System.Windows.Controls.TabControl` (de WPF).

## Mejora de la interfaz

- Nuevo botón de color con el tema actual en la cabecera principal.
- Desplegable con seis temas y muestra de color; marca el activo.
- Elección sincronizada con `Opciones > Apariencia`, con persistencia en `settings.json`.
- Actualización inmediata del monitor flotante si está abierto.
- Acceso directo a más ajustes de Apariencia desde el desplegable.
- El texto `PALENTINO.ES` se mantiene en el pie; se retira de la cabecera para evitar amontonamiento.

No se cambia el motor de ClamAV, pausa, cuarentena, correo ni protección.
La compilación final WPF y su visualización deben verificarse en Windows.
