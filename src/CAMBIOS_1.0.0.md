# PalClam 1.0.0 — Consolidación de la versión estable

Base: **PalClam 1.0.0 RC8 FIX2**, compilada y probada por el autor en Windows según confirmación recibida.

- Promoción de la versión de ensamblado de `1.0.0-rc.8` a `1.0.0`.
- Etiquetas de ventana, Acerca de, historial, Guard, Mail, BAT e instalador normalizadas.
- Se conserva el AppId Inno Setup de las RC; el instalador final es `PalClam_1.0.0_Setup_win-x64.exe`.
- Verificación previa de versiones adaptada a estable; comprobación del paquete offline y SHA-256 mantienen sus controles.
- Preparación de manifiesto de actualización firmada **solo bajo aceptación local explícita**. No hay subida automática al servidor.
- Se conservan todas las funciones de RC8, las 47 pruebas y el historial de desarrollo.

**Pendiente para distribución pública:** ejecución final del instalador estable en Windows, lista de comprobación de funciones sensibles, revisión legal y, si corresponde, firma de código Authenticode. La firma RSA del manifiesto de actualización no equivale a Authenticode.
