# Plan de pruebas — PalClam 1.2.0 Preview 1

## Resultado actual

Comprobaciones estáticas ejecutadas en Linux; pruebas C#, WPF, PowerShell, instalador y ClamAV **no ejecutadas**. El acta permanece PENDIENTE. Consultar `RESULTADOS_1.2.0_PREVIEW1.md`.

## Automáticas en Windows

En una carpeta nueva y con SDK .NET 10 x64, ejecutar `PRUEBAS.bat`. Deben completarse sus seis fases:

1. `VerificarFuentes.ps1` y `VerificarLocalizacion.ps1`: versiones, AppId, controles heredados, claves, duplicados, formatos y cobertura.
2. `PalClam.LocalizationTests`: ResourceManager real con seis culturas; clave y satélite ausentes con fallback español; marcador desconocido; parámetros y formas One/Other; serialización de AppSettings en carpeta temporal; reinicio de estado; fallo de escritura; compatibilidad con settings sin UiLanguage; culturas del proceso intactas.
3. SmokeTests heredados de análisis, seguridad y progreso, con la dependencia nueva de TextCatalog enlazada.
4. ThemeTests WPF: 360 alternancias de siete temas y seis idiomas; texto, tooltip parametrizado y control fuera del árbol visible; conservación de controles y progreso **simulado**. No equivale a ClamAV real.
5. Compilación WPF autocontenida win-x64.
6. Compilación y autoprueba del servicio Windows independiente.

Prueba portable adicional: `python Tools/validate_localization.py`. Es estática; no sustituye ninguna fase C# o WPF.

## Manuales de idioma

| Ensayo | Resultado esperado |
| --- | --- |
| Perfil nuevo | Español, sin depender de la cultura de Windows |
| Cambiar ES → EN → FR → DE → PT → IT → ES | Cabecera, títulos de pestaña, entrada de Ajustes e introducción de Ayuda cambian al instante |
| Abrir Ayuda después de cambiar idioma | Textos enlazados de la página ya creada muestran la selección nueva |
| Reiniciar normalmente y desde bandeja/arranque oculto | Se conserva el idioma seleccionado |
| Settings de 1.1.0 sin UiLanguage | Se usa español y se conservan rutas, exclusiones, tema y preferencias previas |
| UiLanguage inválido, null o cultura regional | Valores desconocidos → español; en-US/de-DE/pt-BR → idioma soportado correspondiente |
| Fallo de escritura de settings en entorno de prueba | La UI conserva idioma anterior; aviso y diagnóstico local, sin mensaje falso de guardado |
| Texto parametrizado Ver función | Nombre de función íntegro, sin perder símbolos o rutas de prueba |
| Traducción ausente en copia de prueba recompilada | Se muestra español, sin adoptar el idioma de Windows |

No manipular el perfil de trabajo para simular errores: usar cuenta o máquina de pruebas y copia de seguridad. Los fixtures automáticos permiten probar fallback sin editar los recursos del producto.

## Análisis activo y seguridad

Con ClamAV oficial y firmas reconocidas, analizar un conjunto de archivos inocuos suficientemente largo. Cambiar de idioma desde Ajustes y observar cabecera, progreso, historial, PID del motor y resultados. Repetir con pausa solicitada, pausa efectiva, reanudación y detención. Cambiar idioma no debe reiniciar el análisis, cancelar el proceso, vaciar resultados, alterar rutas/exclusiones ni solicitar privilegios.

Repetir junto a Guard y, si está instalado, Machine Guard. No debe cambiar su configuración, estado de pausa ni permisos. Probar correos e IA de forma voluntaria; no debe producirse ninguna consulta remota por cambiar idioma. Mantener las pruebas de cuarentena, firma de actualizaciones y privacidad de `PRUEBAS_1.1.0_FINAL.md`.

## Diseño y distribución

Comprobar los seis idiomas con siete temas, incluido Oscuro violeta, a 100/125/150 % de DPI y tamaño de texto hasta 130 %. Verificar ventana mínima, botones largos, pestañas, cabecera sin solapamientos, contraste, foco por teclado, lector de pantalla, barras y aurora. La prueba automática con progreso simulado no acredita estos ensayos visuales. RTL queda reservado para una fase futura.

Después: `CREAR_INSTALADOR.bat`, `VALIDAR_PAQUETE.bat` y `REVISAR_ENTREGA.bat`. El ejecutable esperado es `PalClam_1.2.0_Preview1_Setup_win-x64.exe`. Comprobar instalación limpia y actualización desde 1.1.0 con AppId conservado, datos/cuarentena intactos y recursos de los seis idiomas disponibles tras publicar/instalar.

Marcar CONFORME en `ACEPTACION_1.2.0_PREVIEW1.ini` únicamente tras ensayos con evidencias. `PUBLICAR_ACTUALIZACION.bat` continúa bloqueado.
