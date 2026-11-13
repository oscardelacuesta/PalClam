# Validación obligatoria — PalClam Preview 15

## 1. Compilación y regresión

1. En Windows x64 con SDK .NET 10, ejecutar `PRUEBAS.bat` desde el ZIP completo. Debe superar las pruebas funcionales, las 360 alternancias WPF, el build WPF, el build del servicio y el `--selftest` del servicio.
2. Ejecutar `EMPEZAR_AQUI.bat` para publicar ambos ejecutables y generar el instalador con Inno Setup. Comprobar que contiene `PalClam.Service.exe` y `Tools\MachineService.ps1`.
3. No distribuir si alguna etapa falla. La validación de scripts no reemplaza la prueba real del SCM.

## 2. Instalación con permisos limitados

1. Instalar PalClam **para todos los usuarios** en Program Files con UAC.
2. Abrir PalClam → Protección → Guard → `Servicio de Windows · Experimental`. Debe mostrarse inicialmente `Servicio no instalado`.
3. Pulsar `Instalar y activar (admin)`, aceptar UAC, comprobar el estado con `services.msc`: nombre `PalClamGuardSvc`, cuenta `NT AUTHORITY\LocalService`, inicio retrasado, estado `En ejecución`.
4. Repetir rechazando UAC: no debe instalarse ni modificar estado.
5. Comprobar que una instalación personal fuera de Program Files no puede registrar el servicio.

## 3. Vigilancia y alcance

1. Colocar un `prueba-inofensiva.txt` recién creado en `%ProgramData%\PalClam\GuardWatch`. Esperar estabilización y actualización de `estado.json`.
2. Verificar que `eventos.log` no presenta detecciones ni errores inesperados. **No introducir malware real para probar**; usar pruebas inofensivas.
3. Modificar varias veces el archivo y generar ráfagas de archivos pequeños; comprobar cola y respuesta, que no quedan archivos bloqueados y que los avisos de pérdida de eventos no se anuncian como resultados limpios.
4. Crear un archivo >100 MB en esa carpeta: debe marcarse como omitido. Crear un enlace de reanálisis: no debe analizarse como archivo regular.
5. Comprobar que archivos de `Descargas` personales **no** generan eventos del servicio. Guard 2.0 puede vigilarlos separadamente si está habilitado.

## 4. Seguridad, paradas y limpieza

1. Desde `services.msc`, detener `PalClamGuardSvc` durante un análisis y comprobar que cierra sus procesos hijo y se detiene, sin modificar archivos originales.
2. Desactivar o quitar temporalmente las firmas **solo en un entorno de prueba aislado**: el servicio no debe mostrar vigilancia sana; debe registrar error y detenerse.
3. Cerrar sesión: el servicio debe seguir activo y el WPF no es necesario.
4. Pulsar `Retirar servicio (admin)`, aceptar UAC, comprobar ausencia de `PalClamGuardSvc`. Carpeta vigilada y eventos deben conservarse.
5. Probar desinstalación compartida: el servicio propio debe retirarse antes de borrar su ejecutable.
6. Confirmar que Defender permanece habilitado y que no se afirma bloqueo preventivo ni cobertura completa.

## Resultados

- Compilación servicio: [ ]
- Servicio registrado correctamente: [ ]
- Permisos LocalService y eventos: [ ]
- Detección local/errores/archivos omitidos: [ ]
- Parada sin procesos colgados: [ ]
- Desinstalación y UAC cancelado: [ ]

**Estado en la entrega de fuentes:** pendiente de pruebas Windows reales.
