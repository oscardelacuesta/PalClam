# Aceptación manual: PalClam 1.1.0 Preview 2

1. Ejecutar `PRUEBAS.bat` y confirmar que terminan todos los casos sin errores.
2. Ejecutar `CREAR_INSTALADOR.bat` y luego `REVISAR_ENTREGA.bat`.
3. Crear un archivo inofensivo en una carpeta de pruebas; en el análisis con EICAR (archivo de prueba estandarizado, nunca malware real) comprobar la detección.
4. Al aislar: elegir NO => archivo intacto; elegir SÍ => archivo movido al perfil y registrado.
5. En cuarentena, pulsar eliminar y elegir NO => el archivo debe permanecer aislado y restaurable.
6. Pulsar eliminar y elegir SÍ => registro «Eliminado» y archivo aislado ya no presente; la ruta original sigue sin existir.
7. Intentar restaurar un elemento ya eliminado => operación rechazada.
8. Desde la tabla de detecciones, usar «Aislar y eliminar…»; elegir NO en la primera confirmación => sin traslado ni borrado.
9. Repetir el paso 8, aceptar primero y rechazar segundo => archivo solo aislado y restaurable.
10. Repetir el paso 8 con doble SÍ => archivo aislado eliminado con registro.
11. Verificar que ZIP internos, enlaces y elementos bloqueados no provoquen borrado arbitrario; que los fallos se expliquen.
12. Verificar análisis, Guard, Mail, Outlook, IA opcional y funcionamiento offline sin regresiones.
13. Confirmar que no hay servicio anti-kill instalado y que la aplicación no afirma ser protección PPL.
14. No publicar Preview 2 como estable antes de completar estas pruebas.
