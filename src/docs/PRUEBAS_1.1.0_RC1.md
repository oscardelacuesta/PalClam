# PalClam 1.1.0 RC1 — Plan de aceptación para Windows

**RC1 pendiente de validación.** No usar la siguiente lista como prueba de que
se haya ejecutado. Guardar registro de consola, capturas y configuración del equipo.

## 1. Construcción (Windows 10/11 x64 + SDK .NET 10 + Inno Setup)

1. Extraer el ZIP completo en una carpeta nueva, sin mezclar versiones.
2. Ejecutar `PRUEBAS.bat /nopause` y comprobar las cinco fases, incluidos los
   SmokeTests, los 360 cambios de tema y la autoprueba del servicio.
3. Ejecutar `CREAR_INSTALADOR.bat` y verificar que construya
   `dist\installer\PalClam_1.1.0_RC1_Setup_win-x64.exe`.
4. Ejecutar `VALIDAR_PAQUETE.bat`; archivar su informe SHA-256.
5. Comprobar en WPF, About, informes y archivos .csproj `1.1.0 RC1` o `1.1.0-rc.1`.
6. Confirmar que `PUBLICAR_ACTUALIZACION.bat` devuelve error, sin publicar.

## 2. Instalación y actualizaciones

1. Instalar en VM limpia como usuario estándar (instalación individual).
2. Instalar como administrador para todos los usuarios; revisar los permisos.
3. Actualizar Preview 25 a RC1 y verificar que conservan preferencias, informes y
   cuarentena, sin dar por hecho que una actualización de versión los migra.
4. Desinstalar y comprobar que no queda un servicio apuntando a un EXE inexistente;
   se conservan los datos del usuario indicados en la documentación.
5. Comprobar el motor ClamAV, las firmas offline y una actualización en red.
6. Simular fallo de descarga o falta de firmas con copia de seguridad en VM:
   verificar que no se informa falsamente de una actualización correcta.

## 3. Servicio, escaneos, cuarentena, IA

1. Activar voluntariamente Machine Guard con UAC, verificar el estado y el latido
   reciente. Detener, iniciar y reiniciar; comprobar la recuperación y registros.
2. Renovar firmas con servicio detenido y comprobar que permanezca detenido.
3. Ejecutar el usuario estándar y comprobar que no puede modificar el servicio,
   las firmas compartidas ni las ACL sin elevación.
4. Guard 2.0, Download Guard, memoria USB de prueba y pérdidas de eventos:
   no presentar la vigilancia reactiva como bloqueo preventivo.
5. Smart Scan: rápido/completo/personalizado, pausa, cancelación, reintentar y
   progreso honesto en rutas inaccesibles y archivos comprimidos.
6. Cuarentena: reanálisis de un archivo de prueba inofensivo, bloqueo por SHA-256
   alterado y confirmación al restaurar o borrar. No usar malware real.
7. Security Dashboard / Alertas / Health 360: comprobar historial con eventos,
   vacío, corrupto y latido antiguo. Verificar que no haya resultados inventados.
8. IA con proveedor local y remoto: antes de enviar cualquier dato, exigir
   confirmación y revisar el texto, sin envío automático de rutas/archivos.
9. Seis temas, escalado 125/150 %, ventana reducida, bandeja, CPU y memoria.

## 4. Puerta de aceptación

Completar `docs\ACEPTACION_1.1.0_RC1.ini` pasando a CONFORME únicamente
las fases realmente comprobadas con evidencias. Ejecutar `REVISAR_ENTREGA.bat`:
si alguna queda PENDIENTE debe devolver código 1 y bloquear la distribución.
La aprobación documental NO equivale a una auditoría de seguridad independiente,
ni habilita el canal de publicación automática.
