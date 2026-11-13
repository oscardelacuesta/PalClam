# Pruebas de aceptación — Preview 2

## Automáticas

En Windows con SDK .NET 10 x64: `PRUEBAS.bat`. Mantiene seis fases: fuentes/recursos, localización, SmokeTests, temas WPF, compilación WPF y compilación/autoprueba de servicio.

`PalClam.LocalizationTests` ejecuta recursos reales, las seis culturas, todas las plantillas de mensajes, campos opacos, fallback, formatos, guardado/reinicio de preferencia, fallo de escritura y prioridad manual sobre Windows. `Tools/validate_localization.py` es una comprobación estática adicional.

`PalClam.ThemeTests` prepara 360 alternancias con siete temas y seis idiomas, textos de página oculta, tooltips, progreso simulado y conservación de ItemsSource/SelectedItem. También conserva un nombre de archivo que coincide con una etiqueta traducible. **Compilar este proyecto no equivale a ejecutar sus ensayos WPF.**

## Matriz Windows

| Comprobación | Resultado esperado |
| --- | --- |
| Perfil nuevo en Windows ES/EN/FR/DE/PT/IT | Idioma detectado sin elegirlo manualmente |
| Perfil nuevo con idioma no soportado | Español |
| Windows inglés y preferencia francesa guardada | Francés, incluso al reiniciar |
| Volver a Automático | Guarda auto y usa el idioma de Windows |
| Migración desde Preview 1 con es explícito | Conserva español hasta seleccionar Automático u otro idioma |
| Idioma regional | Se normaliza al idioma disponible |
| Navegación completa | Protección, Correo, IA, Herramientas y todos sus módulos muestran traducciones |
| Páginas ocultas, subventanas y bandeja | Al abrirlas utilizan el idioma seleccionado |
| Cambiar idioma con análisis activo | No reinicia motor, no cancela ni modifica pausa, resultados o exclusiones |
| Prioridad, modo y tema | Etiqueta traducida; valor interno e identificador conservados |
| Outlook/correo/Inspector | Asunto, archivo, ruta, adjunto, certificado y hash preservados |
| IA | Prompts guiados visibles, consentimiento por consulta, ninguna llamada por cambiar idioma |
| Diálogos y filtros | Botones predeterminados de seguridad y patrones de archivo intactos |
| Privacidad | Ningún archivo o dato se transmite para traducir la interfaz |

Revisar los seis idiomas con siete temas, texto hasta 130 %, DPI 100/125/150 % y ventana mínima. Revisar saltos de línea, botones largos, menús, foco, tooltips, enlaces y alto contraste. Completar revisión lingüística, especialmente textos automáticos extensos.

Repetir las pruebas de firmas, cuarentena, servicio, Guard, rendimiento e instalación de la base 1.1.0. La localización no acredita la eficacia del motor ni un bloqueo preventivo.

## Construcción y acta

Después: `CREAR_INSTALADOR.bat`, `VALIDAR_PAQUETE.bat`, `REVISAR_ENTREGA.bat`. El nombre de instalador es `PalClam_1.2.0_Preview2_Setup_win-x64.exe`. Comprobar los seis idiomas tras publicación autocontenida e instalación/actualización con AppId preservado.

Registrar evidencia en `ACEPTACION_1.2.0_PREVIEW2.ini`. Se entrega toda PENDIENTE: los resultados de desarrollo en Linux no sustituyen aceptación Windows. La publicación estable continúa bloqueada.
