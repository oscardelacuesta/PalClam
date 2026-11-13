# Resultados — PalClam 1.2.0 Preview 1

Fecha: 12 de noviembre de 2026. Entorno de ejecución: Linux, sin SDK .NET, WPF ni PowerShell.

## Ejecutado

| Comprobación | Resultado |
| --- | --- |
| XML `.resx`, claves duplicadas/vacías y cobertura | 37 claves × 6 idiomas = 222 textos; conforme a comprobación estática |
| Parámetros de formato | Coinciden en los seis recursos |
| Claves usadas en código | Las 37 están enlazadas y no hay claves desconocidas en el ámbito migrado |
| Enlaces Compile de proyectos | Destinos presentes |
| Versión / idioma neutro / AppId / bloqueo | 1.2.0, español, AppId original, publicación bloqueada |
| Pruebas negativas del validador | Detectadas clave ausente, clave duplicada, parámetro perdido y AppId alterado |
| Referencias literales de VerificarFuentes | 99 rutas de fuentes/documentación presentes (no ejecuta el script PowerShell) |
| Rutas Source directas de Inno Setup | Presentes; binarios, motor y firmas se preparan en Windows |
| Comparación con base | 65 archivos de servicios/motor/actualizador/clave pública comparados byte a byte: intactos |
| Acta 1.1.0 | Archivo original intacto |
| Acta 1.2.0 Preview 1 | Todos los ensayos PENDIENTE |

Comando portable ejecutado: `python Tools/validate_localization.py`. Las pruebas negativas se realizaron en copias temporales sin modificar el paquete entregado.

## No ejecutado

- Compilación C# WPF y servicio, restauración de paquetes NuGet y publicación autocontenida.
- `PalClam.LocalizationTests`: ResourceManager y satélites .NET, fallback en ejecución, serialización y fallo de persistencia.
- `PalClam.ThemeTests`: Binding WPF y 360 alternancias con progreso simulado.
- SmokeTests heredados y autoprueba del servicio Windows.
- BAT y PowerShell, generación de instalador Inno Setup, instalación/actualización/desinstalación.
- Cambio de idioma con ClamAV real activo, firmas, cuarentena, Guard, correo, IA, DPI, contraste y evaluación visual.

Estos puntos no se declaran superados. Los ensayos automáticos añadidos son código de pruebas preparado para ejecutar en Windows. No existe un EXE compilado en esta entrega.

## Integridad y alcance

ZIP base congelado: `PalClam_1.1.0_CIERRE_CODIGO_COMPLETO(1).zip`.

SHA-256 del ZIP base: `ae7271350b30ce43b7ea6be4e623ab323a9d449633d0e9768779aeea52260dae`.

Se ha trabajado en una copia 1.2.0. La lógica del servicio, privilegios, firmas, actualización autenticada, motor y cuarentena se conserva. Solo su proyecto cambia de versión. Los catálogos y controles de los siete temas/aurora se conservan. Primera fase de traducción: el resto de las cadenas sigue en español y se informa en la UI.

El paquete incluye fuentes completas, parche de archivos modificados/nuevos, manifiesto de hashes y plan de aceptación. La reconstrucción del árbol completo aplicando el parche se comprueba al empaquetar; el resultado queda registrado en la entrega.
