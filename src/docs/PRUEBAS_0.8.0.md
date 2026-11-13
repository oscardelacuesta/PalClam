# Pruebas funcionales de PalClam 0.8.0 (Windows 10/11 x64)

> Antes de distribuir o confiar archivos sensibles, prueba la compilación en una máquina de laboratorio. Esta versión no sustituye Defender.

1. **Compilación**: ejecutar `COMPILAR.bat`; comprobar que produce `PalClam\dist\win-x64\PalClam.exe`. Si falla, conservar el log íntegro.
2. **Instalador offline**: ejecutar `CREAR_INSTALADOR.bat` conectado a Internet en el PC de desarrollo; comprobar que el EXE generado contiene ClamAV, firmas de respaldo y fuentes GPL, y probar en un segundo equipo desconectado.
3. **Arranque**: comprobar que PalClam Guard está desactivado por defecto y que siguen funcionando páginas de Cuarentena, Programador, Opciones, Diagnóstico, Firmas y Actualizaciones.
4. **Activación manual de Guard**: elegir una carpeta temporal de pruebas vacía, pulsar «Guardar preferencias» e «Iniciar vigilancia». Comprobar que aparece el estado activo.
5. **Archivo nuevo**: copiar un `.txt` inofensivo en la carpeta vigilada; tras su estabilización debería constar como «Sin detecciones». Repetir modificándolo y renombrándolo.
6. **Archivo grande**: fijar 50 MB, copiar un archivo mayor y verificar «No analizado» (no «Limpio»). No iniciar un archivo ejecutable desde una carpeta bajo vigilancia como prueba.
7. **Detecciones**: usar un archivo de prueba EICAR obtenido del proyecto oficial EICAR, en un laboratorio y con conocimiento de que Defender podría bloquearlo antes de que llegue a Guard. Nunca utilizar malware real. Verificar aviso y registro y que **no** se mueve solo a cuarentena.
8. **Cuarentena**: seleccionar la detección, detener Guard, aislar solo si el archivo no ha cambiado y tras la confirmación. Probar restauración y rechazo de sobrescritura.
9. **Avisos y privacidad**: desactivar el aviso de Guard, probar la configuración; verificar que las incidencias se almacenan únicamente en `%LOCALAPPDATA%\PalClam\Guard\incidencias.jsonl`.
10. **Cierre**: cerrar PalClam; confirmar que no queda vigilancia ni ejecutables de `clamscan` lanzados por Guard. El arranque automático solo debe funcionar tras marcar su opción.
11. **Carga y saturación**: crear muchos archivos inofensivos durante pocos segundos. Si se pierden eventos, la interfaz debe advertir «vigilancia incompleta»; realizar un análisis completo.
12. **Actualización**: comprobar motor y firmas, operación manual y actualización por instalador sin desactivar Microsoft Defender ni perder preferencias.

**Límites conocidos:** ClamAV no ofrece `on-access` de Windows. `FileSystemWatcher` puede perder eventos en situaciones de alta actividad. ClamScan no expone un porcentaje preciso dentro de archivos grandes. `clamscan` carga firmas cada vez (mayor consumo).
