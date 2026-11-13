# Pruebas Preview 13 FIX1

Ejecuta `PRUEBAS.bat` en Windows con el SDK .NET 10.

Esperado en el paso [2/4], después de las pruebas de exclusiones:

- `[OK] estimación respeta exclusiones (esperados: 2; obtenidos: 2; inaccesibles: 0; truncado: False)`
- `[OK] estimación: sin exclusiones cuenta los tres archivos de la prueba aislada`
- `[OK] estimación: un archivo expresamente excluido no se cuenta`
- `[OK] estimación: carpeta vecina de prefijo parecido no se excluye`

Si falla, facilita las últimas líneas de la consola, no solo el resumen. Después deben pasar las pruebas de cuarentena, IA y temas WPF, y la compilación completa.

Concluidas las cuatro etapas, abre PalClam, inspecciona un TXT inofensivo, escanéalo con ClamAV y prueba el cambio de tema durante el análisis. No distribuyas el programa hasta completar la verificación real.

No hace falta eliminar `ejemplo.txt` antes del test: la prueba ahora aísla los directorios para ser robusta al añadir nuevas funciones.
