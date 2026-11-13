# PalClam 1.1.0 RC1 FIX1 — Verificación de Machine Guard

## Incidencia corregida

En `Tools/VerificarFuentes.ps1`, la condición que comprobaba la salud de
Machine Guard aún exigía una expresión del código de Preview 24:
`Report(recovering ? "Reconciliando" ...)`.

Desde Preview 25, el servicio prioriza un fallo del motor o de las firmas:
`Report(coreIssue is not null ? "Error" : recovering ? ...)`.
La condición antigua provocaba un falso bloqueo en `[1/5]`, sin llegar a
compilar la aplicación.

## Cambios

- La comprobación de motor y firmas ahora exige `MachineGuardPolicy.CoreIssue`,
  `ServicePaths.SignaturesPresent` y `File.Exists(ServicePaths.ScannerPath)`.
- La comprobación del estado exige los contadores de degradación, omitidos y
  errores, y los estados `Error`, `Reconciliando`, `Degradado` y `Vigilando`.
- La interfaz WPF debe seguir utilizando `snap.IsFresh`, informar con el texto
  «No se declara vigilancia activa» y comprobar `snap.IsHealthy`.
- Los mensajes de error del verificador separan las tres garantías y ayudan a
  identificar futuras regresiones.

No se han alterado las funciones del antivirus, el servicio ni los privilegios.

## Prueba en Windows

1. Extraer RC1 FIX1 completo en una carpeta nueva, o aplicar el parche sobre
   **RC1 original**, no sobre Preview 25.
2. Ejecutar `PRUEBAS.bat` o `CREAR_INSTALADOR.bat`.
3. Confirmar que la fase `[1/5]` pasa; registrar cualquier error posterior de
   pruebas, compilación de WPF/servicio o Inno Setup.
4. No publicar RC1 hasta completar las comprobaciones de aceptación.

**Limitación:** el cambio se ha verificado por inspección de fuentes y pruebas
estáticas de patrones positivos/negativos en este entorno. No se ha ejecutado
PowerShell ni compilado .NET 10/WPF aquí. `PRUEBAS.bat` sigue siendo obligatorio.
