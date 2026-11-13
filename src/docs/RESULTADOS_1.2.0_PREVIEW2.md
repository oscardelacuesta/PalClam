# PalClam 1.2.0 Preview 2 — resultados

12 de noviembre de 2026. Entorno: Linux; SDK .NET 10.0.404 preparado para esta entrega.

| Comprobación | Resultado |
| --- | --- |
| Localización .NET real | 45.590 comprobaciones superadas |
| Recursos | 1907 claves × seis idiomas = 11442 textos; sin claves vacías, duplicadas o formatos desajustados |
| Plantillas de presentación | Todas comprobadas con argumentos opacos en los seis idiomas |
| Detección de idioma | Auto, culturas regionales, fallback español y prioridad manual comprobados |
| Guardado y migración | Serialización, reinicio, perfil sin elección y fallo de escritura comprobados |
| WPF PalClam | Compilación cruzada Release: 0 errores, 0 advertencias |
| Pruebas WPF de temas/idiomas | Compilan: 0 errores, 0 advertencias. Ejecución visual pendiente |
| Servicio Windows | Compila: 0 errores, 0 advertencias. Autoprueba bloqueada por el guard de Windows |
| Comparación con Preview 1 | 66 archivos principales de motor, seguridad, servicio, actualización y temas idénticos byte a byte |
| Instalador | AppId preservado, seis idiomas configurados y referencias de fuentes presentes. Inno no ejecutado |
| Verificador de fuentes | 99 referencias literales resuelven; PowerShell 5.1 no ejecutado |
| Publicación | Bloqueada; acta Preview 2 íntegramente PENDIENTE |

## Evidencias

Los resultados de compilación y de localización están en `validacion-preview2/`. La comprobación WPF final se realizó secuencialmente, sin servidor de compilación, tras una interrupción del runtime SDK en una ejecución paralela del entorno Linux. No se modifican los BAT de Windows con esa configuración temporal de comprobación.

Comandos principales: `dotnet run --project PalClam.LocalizationTests/PalClam.LocalizationTests.csproj -c Release`; `dotnet build PalClam/PalClam.csproj -c Release -p:EnableWindowsTargeting=true`; compilaciones equivalentes de ThemeTests y servicio. Comprobación estática adicional: `python Tools/validate_localization.py`.

## SmokeTests y límites de plataforma

La batería general se ejecutó hasta `USB Watch: no analiza unidades previas al inicio`, en `PalClam.SmokeTests/Program.cs:207`. Utiliza raíces Windows F:\ y G:\, que Path interpreta de forma distinta en Linux. El mismo resultado se reprodujo con la base Preview 1. El código RemovableDriveMonitor se conserva byte a byte. **No se declara la batería completa superada**: repetirla en Windows.

El servicio devuelve código 1 antes de su autoprueba cuando OperatingSystem.IsWindows es falso. No se ha simulado el registro ni el arranque de un servicio Windows.

No ejecutado: WPF visible, 360 alternancias reales, DPI/contraste/tamaño de texto, análisis ClamAV real, cambios de idioma durante un análisis, motor/firmas, pruebas de correo/Outlook/IA con cuentas, instalador, actualización/desinstalación y aceptación Windows. No se incluye un EXE ni instalador validado.

## Cobertura e integridad

Se incorpora traducción de la interfaz propia de los módulos de Protección, Correo, IA, Herramientas, Ayuda, Ajustes y los restantes módulos, sus avisos y mensajes parametrizados. Se conservan los datos personales y técnicos. Las traducciones asistidas localmente incluyen revisión de terminología/advertencias; la revisión lingüística y visual completa sigue pendiente.

Base exacta: `PalClam_1.2.0_Preview1_MULTILENGUAJE_COMPLETO.zip`.

SHA-256 base: `8626f57796049355ebf8a55884615e3adfa69d16ee3f0b01496a380db931e7f8`.

El parche contiene únicamente archivos modificados/nuevos sobre esa base, con manifiesto de hashes. Al empaquetar se comprueba que aplicarlo reconstruye byte a byte el ZIP completo. La base 1.1.0, la aceptación anterior, la lógica del motor, firma de actualizaciones y privilegios permanecen conservados.

Para probar en Windows: extraer el completo en carpeta nueva y ejecutar `PRUEBAS.bat`. Después seguir `PRUEBAS_1.2.0_PREVIEW2.md`. No marcar CONFORME sin evidencias.

La etiqueta opcional del menú de Explorer se traduce al activar esa integración. Sus claves, propietario y comando se conservan; una entrada anterior se renueva al desactivar y volver a activar la opción. El adaptador de notificaciones y esa etiqueta son cambios de presentación, no de política antivirus.
