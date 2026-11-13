# Pruebas de aceptación — PalClam 1.1.0 Preview 21

## 1. Compilación e instalador

1. Descomprimir el ZIP completo en carpeta nueva, sin mezclar archivos de versiones previas.
2. Ejecutar `PRUEBAS.bat` en Windows x64 con SDK .NET 10: deben pasar verificación de fuentes, SmokeTests, 360 alternancias WPF, compilación PalClam y servicio.
3. Ejecutar `EMPEZAR_AQUI.bat` para compilar y empaquetar; verificar que el instalador generado se llama `PalClam_1.1.0_Preview21_Setup_win-x64.exe`.
4. Verificar actualización sobre Preview 20, conserva ajustes e inventario de cuarentena e instala/desinstala sin dejar inicios de sesión obsoletos.

## 2. Remediation Center PRO — pruebas manuales seguras

**Usar exclusivamente archivos de prueba inofensivos y un equipo de pruebas. No ejecutar malware real.**

1. Crear un archivo de texto inofensivo propio. Generar una detección controlada mediante laboratorio o usar el flujo existente validado, con permiso explícito, para aislarlo.
2. Abrir **Protección → Remediation Center PRO**, confirmar que aparece el archivo aislado con SHA-256 y estado anterior.
3. Pulsar **Reanalizar aislados**: debe usar ClamAV y firmas locales, sin restaurar, lanzar, borrar ni cambiar el veredicto original. Ante falta de firmas debe indicar que no hay resultado.
4. Comprobar los tres tipos de resultado: detección, sin detecciones (*sin promesa de seguridad*) y no concluyente si se fuerza un error controlado (por ejemplo, sin motor).
5. Seleccionar varios elementos inofensivos: reanalizar hasta 20; seleccionar más de 20 debe quedar bloqueado. Eliminar y restaurar deben exigir exactamente un seleccionado.
6. Marcar **posible falso positivo**: comprobar que solo cambia la marca y nunca libera automáticamente el objeto.
7. Exportar el informe TXT; comprobar que incluye fechas, SHA-256, origen y resultado y **no contiene bytes del archivo aislado**.
8. Comprobar que el centro de notificaciones ofrece enlace directo a Remediation Center.
9. Alterar una copia de cuarentena exclusivamente de laboratorio y verificar que el SHA-256 o tamaño bloquea el reanálisis.
10. Eliminar/restaurar un objeto de laboratorio tras confirmar; comprobar que ya no puede reanalizarse como aislado.

## 3. Regresiones

- Repetir `PRUEBAS.bat` con los seis temas y textos al 90 %, 100 % y 130 %.
- Verificar que Guard personal, USB Watch, Download Guard y el servicio opcional no se reinician al abrir Remediation Center ni al cambiar tema.
- Comprobar aviso UAC y funcionamiento de Machine Guard solamente en instalación compartida de prueba.
- Verificar que los registros de alertas y cuarentena anteriores siguen siendo legibles.

**No distribuir como estable si falla alguna prueba de compilación, integridad, restauración o servicio.**
