# PalClam 1.2.1 — resultados de preparación

Fecha: 12 de noviembre de 2026. Base: PalClam_1.2.0_COMPLETO.zip, SHA256 `1ebe88254c895c47d806d040c591f5f9b0a2b6f118c2a1edb9511d9a7222608c`.

## Cambios entregados

La entrada habitual EMPEZAR_AQUI.bat genera, en Windows, el instalador automático, los instaladores x64/x86 y los dos ZIP portables. Cada edición publica su aplicación y servicio para el RID correspondiente y prepara el motor ClamAV de su misma arquitectura. Se verifica la cabecera PE de los ejecutables y el SHA256 del paquete oficial de cada motor. Un fallo de una fase devuelve error y detiene la entrega.

La portable es autocontenida y guarda los datos propios en Data junto al programa. Se inhiben registro de Explorer, inicio automático, tareas y servicio. Se evita aplicar un instalador sobre ella. Las claves IA conservan DPAPI y siguen vinculadas a Windows; las rutas personales pueden necesitar ajuste al cambiar de equipo. El instalador conserva AppId y la ruta de datos de la instalación normal. La web del producto pasa a palclam.com, conservando los créditos de palentino.es y el canal firmado anterior hasta que el nuevo dominio esté preparado.

El inventario legal cubre las once bibliotecas NuGet resueltas y las licencias originales de los runtimes publicados. Se incluyen originales de Inno Setup, OpenMcdf MPL-2.0 y la alternativa GPL de UTF.Unknown, con sus fuentes exactas. El instalador y las portables reciben Licenses, Source, los avisos y las atribuciones. La comprobación rechaza bibliotecas desconocidas, avisos alterados y fuentes requeridas ausentes. Se conservan todos los archivos originales del paquete ClamAV y se prepara su fuente oficial fijada al generar la distribución.

## Comprobaciones realizadas

Entorno Linux, SDK .NET 10.0.404, PowerShell 7.6.5. Los logs están en `docs/validacion-1.2.1`. No se ha ejecutado Windows mediante emulación.

| Comprobación | Resultado observado |
|---|---|
| Localización | 49.358 comprobaciones superadas; 2.064 claves × 6 idiomas = 12.384 textos. |
| Funciones correo y monitor | 55 comprobaciones superadas, incluidos eventos reales de archivos sintéticos; sin malware. |
| Arquitectura y almacenamiento | 22 comprobaciones superadas; PE, selección de motor, rutas, marcador portable y procesos separados. |
| Empaquetado y licencias | 35 comprobaciones superadas en carpetas temporales con fixtures sintéticos, eliminados al terminar. Se comprueban ambas portables, fuentes, avisos, privacidad y rechazo de arquitectura incorrecta. No son motores o firmas reales. |
| Publicación de aplicación | `win-x64` y `win-x86` autocontenidas: éxito, arquitectura PE correcta. |
| Publicación de servicio | `win-x64` y `win-x86` autocontenidas: éxito, arquitectura PE correcta. |
| Proyecto ThemeTests WPF | Compilación cruzada correcta, 0 errores y 0 advertencias. Ejecución de la interfaz pendiente. |
| PowerShell | Análisis sintáctico de los scripts correcto. Verificador completo de fuentes superado. Comprobada propagación de códigos de salida. |
| Inventario legal real | Coinciden las once bibliotecas resueltas; hashes de textos y archivos fuente comprobados. Licencias de los runtimes x64/x86 recuperadas de los paquetes del SDK utilizados. |
| ZIP de fuentes | Creado y comprobado: código y scripts necesarios, recursos, clave pública y avisos; sin bin/obj/cache, claves privadas ni datos de usuario. |
| Suite SmokeTests completa | No completada en Linux: detiene en la comprobación USB de rutas Windows F:/G:, Program.cs:207. Es la misma limitación de plataforma de la base; no se ha cambiado el test para ocultarla. PRUEBAS.bat conserva su ejecución en Windows. |

Las pruebas de empaquetado usan datos expresamente identificados como TEST ONLY, en una carpeta temporal independiente. Ningún motor, firma o runtime simulado se añade a los entregables. Los ejecutables de publicación cruzada solo se utilizan aquí para comprobar compilación y PE; el ZIP de esta entrega contiene fuentes, documentación y las fuentes de bibliotecas, no los instaladores finales.

## Pendientes de ejecución en Windows

Ejecutar EMPEZAR_AQUI.bat con SDK .NET 10 e Inno Setup 6.3 o posterior. Verificar las cinco salidas, instalación limpia y actualización, desinstalación, elección automática de arquitectura, convivencia con una portable, rutas con espacios y carpeta Data sin permisos. Probar la portable con su runtime y motor reales, cambios de ubicación/usuario, DPAPI, interfaz/temas/DPI, Outlook clásico/Graph y PDH. La ejecución de ClamAV/FreshClam/SigTool y la firma/antigüedad de bases reales se comprueban al construir en Windows. No se han ejecutado esas herramientas nativas aquí.

ISCC no se ha ejecutado en este entorno; la sintaxis y las ramas x64/x86/dual del archivo Inno se han revisado. No se afirma haber probado instaladores ni portables nativos. Tampoco se afirma certificación antivirus, bloqueo preventivo o recuperación frente a ransomware. El monitor conserva su naturaleza reactiva.

Todas las casillas de `ACEPTACION_1.2.1.ini`, incluida VERIFICACION_LICENCIAS sobre los archivos finales, se mantienen PENDIENTE. La versión se identifica como 1.2.1 sin RC/Preview; esa identificación no fabrica aceptación ni publica actualizaciones. El flujo de publicación automática permanece bloqueado y las claves privadas de firma quedan fuera de los paquetes.

## Contenido de la entrega

- Completo: todas las fuentes 1.2.1, sin necesidad de parches anteriores.
- Parche: archivos añadidos o cambiados exclusivamente respecto al completo 1.2.0; no borra archivos.
- Manifiesto: hashes de las fuentes, base exacta, cambios del parche y resultados observados. Los manifiestos y actas anteriores se conservan como histórico.

Al redistribuir las salidas generadas, conservar Licenses, Source y los textos de copyright/licencia. No reducir el ZIP portable a un EXE suelto: necesita su runtime, motor, firmas, avisos y fuentes.
