# PalClam 1.1.0 Preview 9 FIX2 — colision WPF/Windows Forms

## Incidencia corregida

En Preview 9 FIX1 las 87 pruebas logicas pasaban, pero `dotnet build`
mostraba **10 errores CS0104** en `PalClam/MainWindow.OptionsTabs.cs`:
`Control` es ambiguo entre `System.Windows.Controls.Control` y
`System.Windows.Forms.Control`.

La correccion usa el alias **`WpfControl = System.Windows.Controls.Control`**
y sustituye los diez accesos a propiedades de dependencia en el estilo
de las pestanas. La clase `TabControl` continua totalmente cualificada
como WPF. Se ha reforzado el validador `Tools/VerificarFuentes.ps1`
para bloquear esta ambiguedad antes de la compilacion.

## Aplicacion y verificacion en Windows

1. Sobre una copia de los fuentes **Preview 9 FIX1**, extrae
   `PalClam_Preview9_FIX2_Parche_CS0104_Control.zip` sobrescribiendo.
2. Ejecuta `PRUEBAS.bat` y despues `EMPEZAR_AQUI.bat`.
3. Comprueba en PalClam que **Opciones > Apariencia** funciona,
   el boton **Tema de color** de la cabecera cambia la paleta
   y la barra flotante adopta el color seleccionado.

No se han tocado el motor, los procesos ClamAV, las firmas,
la pausa por lotes, Guard, correo, IA ni las reglas de cuarentena.
La version de instalacion **permanece 1.1.0 Preview 9**,
con el mismo AppId para preservar las actualizaciones.

**Validacion disponible aqui:** verificacion estatica y consistencia
entre el ZIP completo y el parche. **Pendiente:** compilacion WPF
y pruebas reales de ejecucion en Windows.
