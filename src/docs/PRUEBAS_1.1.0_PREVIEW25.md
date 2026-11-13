# PRUEBAS — PalClam 1.1.0 Preview 25

## A. Compilación y pruebas automáticas

1. Descomprimir el proyecto completo en carpeta nueva, sin mezclar con P24.
2. Ejecutar `PRUEBAS.bat`. Registrar salida íntegra de las cinco fases.
3. Ejecutar `CREAR_INSTALADOR.bat`, después `VALIDAR_PAQUETE.bat` y `REVISAR_ENTREGA.bat`.
4. Comprobar `1.1.0-preview.25` tanto en WPF como en PalClam.Service.
5. Comprobar Inno Setup, instalación individual/compartida y desinstalación limpia.

## B. Machine Guard en PC de PRUEBAS (nunca primera prueba en producción)

1. Instalar para todos los usuarios y activar el servicio voluntariamente con UAC.
2. Comprobar `Running` con `Get-Service PalClamGuardSvc` y el latido reciente
   en `%ProgramData%\PalClam\MachineGuard\estado.json`.
3. Detener desde WPF; verificar `Stopped`. Sin iniciar el servicio, sincronizar
   las firmas de instalación: debe seguir detenido.
4. Iniciar y reiniciar desde WPF; verificar Running sostenido, no solo la respuesta
   inicial de `sc.exe`.
5. En un equipo aislado y con copia de seguridad, simular **únicamente** la retirada
   administrativa temporal del ejecutable `clamscan.exe` o de las firmas compartidas
   (con servicio detenido antes de manipular). Reiniciar el servicio: debe informar
   no operativo, nunca verde. Restaurar y comprobar registros sin borrar errores.
6. Llenar la carpeta común con pruebas inocuas, controlar estados `Reconciliando`,
   `Degradado`, límites y eventos perdidos; confirmar ausencia de cuarentena automática.
7. Renovación online de firmas con autorización UAC, conexión disponible y restauración
   del servicio; si falla, registrar explícitamente su estado en vez de afirmar éxito.

## C. Regresiones

- Guard personal y Download Guard, USB Watch (con dispositivo de prueba),
  Smart Scan PRO (pausa/cancelación), Remediation Center (SHA-256 y restauración
  manual), alertas e historial, Signature Center y AI Security Analyst sin
  transferencia de datos sin consentimiento.
- Probar los seis temas, 125/150% de escala, ventana reducida y bandeja.
- Revisar compatibilidad con Windows 10/11 x64, instalación no-admin y uso multiusuario.

## Criterio de aprobación

**No etiquetar RC1** hasta que `PRUEBAS.bat` pase, Inno Setup genere e instale
correctamente, y se hayan validado servicio/firmas/cuarentena/privacidad en Windows.
