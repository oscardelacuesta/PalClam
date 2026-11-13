# Pruebas de aceptación — PalClam 1.1.0 Preview 20

## Antes de instalar

1. Descomprimir el proyecto completo en una carpeta nueva. Ejecutar **PRUEBAS.bat**: debe completar cinco etapas, incluidas SmokeTests, 360 alternancias reales de tema WPF y compilación de `PalClam.Service.exe`.
2. Si compila, ejecutar **EMPEZAR_AQUI.bat** e inspeccionar que el instalador incluya el EXE del servicio, `MachineService.ps1`, FreshClam, sigtool y código fuente.
3. No distribuir por haber superado solo las pruebas de código: instalar en una máquina de pruebas.

## Signature Center PRO

4. Abrir **Motor y firmas**; comprobar que aparecen dos secciones distintas: **mi cuenta** y **servicio del equipo**. Una base ausente debe mostrar estado incompleto, no «operativo».
5. Ver las versiones cuando `sigtool.exe` está disponible. Si no lo está o un archivo es inaccesible, mostrar **no verificable**, nunca una versión inventada.
6. Ejecutar «Actualizar mi cuenta»; confirmar que utiliza su carpeta local y que el servicio conserva sus bases.
7. Con instalación personal en Descargas, pulsar «Actualizar equipo online»: debe informar de que se requiere instalación compartida y no elevar privilegios sin motivo.

## Servicio experimental en Windows (máquina de pruebas, administrador)

8. Instalar **para todos los usuarios** en `Program Files`; activar Machine Guard expresamente. Con el servicio y las firmas operativos, anotar las versiones de main/daily.
9. Ejecutar **Actualizar equipo online · UAC** y aceptar la autorización. Comprobar que FreshClam se ejecuta sobre una copia temporal y que `sigtool` informa de versiones de main y daily.
10. Verificar que el servicio se detiene solo durante la sustitución final y reinicia únicamente si estaba activo previamente; si estaba detenido, **debe permanecer detenido**.
11. Desconectar Internet, repetir la acción y confirmar que no desaparecen ni se sustituyen las bases compartidas anteriores por firmas incompletas.
12. Colocar en el entorno de pruebas una base compartida de versión superior al respaldo, intentar sincronización offline y comprobar que se rechaza el retroceso.
13. Con UAC cancelado, no debe modificarse el servicio ni sus bases. Una cuenta sin permisos administrativos no debe poder sobrescribir `ProgramData\PalClam\MachineGuard\Signatures`.
14. Examinar `estado.json`, los registros y un análisis real de muestra inofensiva. Un latido antiguo o una actualización fallida NO debe mostrarse como protección preventiva activa.

## Regresión

15. Verificar Guard personal, USB Watch, vigilancia de Descargas, centro de notificaciones, Inspector, seis temas, aurora, cuarentena y acceso a Modern Terminal AI.
16. Probar cierre de sesión, arranque con Windows para usuario único y todas las cuentas, actualización sobre instalación anterior y desinstalación.

**Criterio de aceptación:** no publicar hasta compilar, probar instalación real y verificar integridad de firmas/servicio, recuperación y permisos. La prueba estática del ZIP no reemplaza estas comprobaciones.
