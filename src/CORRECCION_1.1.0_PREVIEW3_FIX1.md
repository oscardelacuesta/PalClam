# PalClam 1.1.0 Preview 3 — FIX1: validación de versión del instalador

## Síntoma

`[ERROR PREVIO] El nombre visual del instalador no corresponde a 1.1.0 Preview 3.`

## Causa

`Tools/VerificarFuentes.ps1` y `Tools/ValidarPaquete.ps1` todavía exigían los nombres de Preview 2, pese a que `Installer/PalClam.iss` declara correctamente Preview 3.

## Cambios

- Ambas comprobaciones esperan ahora `AppVerName={#MyAppName} 1.1.0 Preview 3`.
- Ambas comprobaciones esperan `OutputBaseFilename=PalClam_1.1.0_Preview3_Setup_win-x64`.
- Actualizados los mensajes informativos y el texto del archivo inofensivo de prueba.
- No se elimina ninguna validación, ni se modifica ClamAV, C#, los BAT, las firmas o las claves.

## Aplicación del parche

Extraer el ZIP del parche en la **raíz de los fuentes Preview 3** y permitir sobrescribir `Tools/VerificarFuentes.ps1` y `Tools/ValidarPaquete.ps1`. Después ejecutar `EMPEZAR_AQUI.bat`.

## Alcance de las pruebas

Verificada estáticamente la coherencia de las expresiones con Inno Setup y la integridad de los ZIP. No se ha ejecutado el script PowerShell ni compilado en Windows desde el entorno de creación.
