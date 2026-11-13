# PalClam 1.0.0 RC3 — Guía de aceptación antes de 1.0 estable

## 1. Verificación local (sin Visual Studio)

1. Descomprime en una carpeta NUEVA y ejecuta `PRUEBAS.bat`.
2. Comprueba que indica 20 pruebas correctas y que `dotnet build` termina con código 0.
3. Ejecuta `CREAR_INSTALADOR.bat`. Comprueba la existencia del EXE RC3 en `dist\installer`.
4. Haz una copia de seguridad de las opciones y datos locales si vas a probar
   aislamiento/restauración de archivos.

## 2. Instalación/actualización

- Con PalClam cerrado incluso desde la bandeja, instala RC3 sobre una RC anterior.
- Revisa accesos directos, icono, nueva versión RC3, consola, menú y desinstalador.
- Desconecta Internet y confirma que motor/firmas offline permiten escanear un
  archivo de prueba. **Desactualizado** NO debe mostrarse como **actualizado**.
- No pruebes el instalador en equipos de producción hasta validar RC3.

## 3. Funciones críticas

- Analiza una carpeta con archivos de prueba inofensivos. Verifica el progreso
  y el resultado. Repite y cancela: el estado debe ser **Cancelado**, nunca limpio.
- Para la detección usa SOLO el archivo de prueba estándar EICAR, no malware real.
  Aísla y restaura únicamente el archivo creado para la prueba; confirma que
  no se sobrescribe otro archivo si el destino ya existe.
- Inicia Guard en carpeta temporal y prueba crear/modificar archivos. Prueba
  pausa, reanudación, icono residente y salida completa.
- Prueba análisis programado con sesión iniciada y comprueba el registro.
- Analiza un `.eml` inocuo con adjunto de prueba; comprueba temporales y errores.
- Outlook en línea requiere autorización real del usuario, permiso Mail.Read y
  cuenta de prueba; no se debe dar por validado solo por compilar.

## 4. Actualizaciones

- Sin servidor de publicaciones, una consulta no debe instalar nada.
- `PUBLICAR_ACTUALIZACION.bat` debe rechazar RC3; NO publiques una candidata
  en el canal estable. El actualizador firmado se debe probar en un entorno
  de pruebas aislado antes de usarlo con usuarios reales.
- No habilitar actualizaciones silenciosas hasta verificar el ciclo completo.

## 5. Criterio para la versión 1.0 estable

Sin errores de compilación, instalación y desinstalación verificadas, sin pérdidas
ni bloqueos en cuarentena, análisis no concluyentes distinguibles de limpios,
pruebas de Guard/Outlook/actualizador completadas y avisos legales revisados.
La compilación por sí sola no demuestra calidad antivirus ni detección exhaustiva.
