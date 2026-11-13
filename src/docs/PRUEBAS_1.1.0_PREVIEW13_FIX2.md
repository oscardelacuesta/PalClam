# PalClam Preview 13 FIX2 — Pruebas de regresión WPF

1. Ejecuta `PRUEBAS.bat` en Windows con el SDK .NET 10.
2. El primer paso debe aceptar las referencias WPF sin ambigüedad; de lo contrario, `VerificarFuentes.ps1` parará antes de las 107 pruebas.
3. Confirma las 107 pruebas funcionales, las 360 alternancias WPF, y que **[4/4] Compilando código Windows/WPF** termina sin errores `CS0104`, `CS0176` ni `CS0120`.
4. Genera instalador con `EMPEZAR_AQUI.bat` y prueba abrir Inspector y Opciones, cambiando temas durante un análisis de archivos inofensivos.
5. Si falla, comparte desde la primera línea `[ERROR]` hasta la línea `Compilación error`.

FIX2 corrige nombres ambiguos del código; no cambia ClamAV ni la política de análisis.
