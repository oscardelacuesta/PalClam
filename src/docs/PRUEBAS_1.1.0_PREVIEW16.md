# Pruebas de aceptación — PalClam 1.1.0 Preview 16

**Ejecutar solo en un equipo de pruebas Windows 10/11 x64 con .NET 10 SDK.**
No instalar el servicio experimental en producción todavía.

## Construcción

1. Descomprimir el proyecto **completo** en una carpeta nueva, sin mezclarlo
   con `bin`, `obj` o la publicación de Preview 15.
2. Ejecutar `PRUEBAS.bat`: validación de fuentes, SmokeTests, 360 cambios de
   tema WPF, compilación del escritorio y autoprueba del servicio.
3. Ejecutar `EMPEZAR_AQUI.bat` para crear y validar el instalador Inno Setup.
   La compilación no demuestra que el servicio esté listo para producción.

## Servicio, seguridad y estado

1. Instalar para **todos los usuarios** en `Program Files`. La instalación
   no debe iniciar ni registrar el servicio sin consentimiento expreso.
2. En Guard, instalar el servicio con UAC. Cancelar una primera vez el aviso:
   debe respetarse la cancelación y no cambiarse el estado anterior.
3. Repetir y aprobar: comprobar `services.msc`, tipo de cuenta `LocalService`,
   servicio `PalClamGuardSvc`, arranque automático retrasado y recuperación SCM.
4. Verificar que las firmas compartidas están en
   `%ProgramData%\PalClam\MachineGuard\Signatures`. Una cuenta estándar no
   debe poder escribir allí. La cuenta de servicio solo debe leerlas.
5. Confirmar que el estado se refresca aproximadamente cada 10 segundos al
   abrir la sección; ocultar o cambiar de sección no debe crear sondeos nuevos.
6. En GuardWatch, crear varios TXT inofensivos y observar contadores.
7. Detener el servicio y comprobar que, pasados unos 35 segundos, el latido
   antiguo no aparece como servicio operativo. Reiniciar y observar
   `Reconciliando` y el contador de revisiones.
8. Añadir un archivo mayor de 100 MB en entorno de pruebas: debe registrarse
   como omitido, no como analizado, y mostrar advertencia.
9. Provocar una ráfaga controlada de archivos: comprobar cola y pérdidas.
   Si se pierden eventos, el estado debe ser degradado, no verde.
10. Sincronizar firmas desde Opciones con UAC. Se debe parar y reiniciar el
    servicio. Si las firmas compartidas son más recientes, no sobreescribirlas
    con el paquete antiguo; registrar fallo sin borrar las firmas previas.
11. Ejecutar la prueba de compatibilidad con dos usuarios de Windows: la
    configuración y Guard personal siguen separados, el servicio es común.
12. Retirar el servicio y comprobar que queda eliminado. GuardWatch y los
    registros **no deben borrarse**. Comprobar el desinstalador con la cuenta
    administrativa apropiada.

## Regresión obligatoria

- Cambiar seis temas durante un análisis real; aurora sin bloqueos ni cierre.
- Verificar el texto «Repetir análisis» a escala 90, 100, 120 y 130 %.
- Inspector, cuarentena, programación, correo e IA no deben cambiar su
  funcionamiento por la nueva revisión.
- En **Opciones → Integraciones**, comprobar que el enlace sutil a Modern Terminal AI abre
  `https://modernterminal-ai.com/` en el navegador solo tras pulsarlo; cambiar
  temas y revisar que no inicia análisis ni envía información del archivo.
- No ejecutar muestras reales de malware en estas pruebas; son suficientes
  archivos inofensivos para validar eventos y estados.

## Resultado

Esta es una versión **experimental**. Los resultados `OK` en SmokeTests no
sustituyen los ensayos Windows del servicio, ACL, UAC y SCM.
