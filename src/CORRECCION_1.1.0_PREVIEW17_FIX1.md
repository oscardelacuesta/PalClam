# PalClam 1.1.0 Preview 17 FIX1 — Verificación del servicio

## Incidencia

`PRUEBAS.bat` se detenía en la fase `[1/5]` con el mensaje:

`[ERROR PREVIO] Preview 17: servicio incompleto, no separado de WPF o sin instalador.`

La causa era una comprobación estática obsoleta en `Tools/VerificarFuentes.ps1`. El script buscaba la frase «No se declara vigilancia activa» en `PalClam.Service/MachineGuard.cs`, pero esa frase se utiliza en `PalClam/MainWindow.MachineService.cs` al mostrar un servicio sin latido reciente. El servicio informa su estado mediante «Vigilando», «Degradado» y «Reconciliando».

## Cambios

- Se elimina solamente esa dependencia de texto en el archivo equivocado.
- Se conserva la exigencia del proyecto Windows .NET 10, canal limitado, `FileSystemWatcher`, interfaz de administración e inclusión del servicio en Inno Setup.
- Se comprueba de forma independiente que Machine Guard requiere firmas compartidas, evalúa los archivos omitidos y el indicador de degradación, y distingue sus estados de salud.
- Se exige que WPF advierta expresamente cuando no existe un latido reciente y consulte `snap.IsHealthy(...)`.
- Se separan los mensajes del verificador para identificar el bloque que falla.

## Alcance

No se modifica el ejecutable del servicio, la aplicación WPF, el motor ClamAV, Guard, las notificaciones, la configuración ni el instalador. La versión continúa siendo `1.1.0-preview.17`.

## Comprobaciones

- Comparación byte a byte de todos los archivos previos con la base Preview 17, excepto documentación y el verificador actualizado.
- Verificación estática de los **24 patrones** de servicio, WPF, instalador, controles de administración y pruebas: todos presentes.
- Integridad y reconstrucción del parche contra el ZIP anterior.

**Pendiente:** ejecutar `PRUEBAS.bat` en Windows, compilar WPF/servicio, hacer el test real y generar el instalador. La inspección estática no sustituye una ejecución real de PowerShell ni una compilación Windows.
