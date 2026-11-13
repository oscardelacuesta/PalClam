# Traspaso a ChatGPT Work — PalClam 1.2.0 multilenguaje

## Paquete de partida

**PalClam 1.1.0** código congelado para aceptación; base RC1 Cierre Visual FIX3. Se entregan fuentes C# / WPF .NET 10, proyecto de servicio Windows independiente, scripts BAT/PowerShell, Inno Setup, pruebas y guías. No se incluyen instaladores compilados ni ClamAV oficial y firmas reales: se preparan en Windows.

**Situación de calidad:** el usuario confirmó la ejecución correcta de pruebas automáticas de FIX3; las pruebas de instalación, seguridad y aceptación manual siguen pendientes en `docs/ACEPTACION_1.1.0_FINAL.ini`. El canal público permanece bloqueado.

## Objetivo de Work

Crear **PalClam 1.2.0 multilenguaje**, trabajando sobre una copia de las fuentes de 1.1.0. **No modificar el árbol congelado** y no retirar controles de seguridad.

### Idiomas iniciales

ES (predeterminado), EN, FR, DE, PT e IT. Más idiomas solo tras verificar cobertura y UI. El instalador puede usar sus propios mensajes de Inno Setup; la cultura de Windows no debe sustituir una elección explícita del usuario.

### Arquitectura solicitada

1. Introducir recursos `.resx` por cultura o equivalente fuertemente tipado para textos WPF, cuadros de diálogo, menú de bandeja, ayuda, avisos, correo, opciones, asistente IA e informes.
2. Usar claves estables, tratamiento de plurales y parámetros (`{0}`), y fallback **a español**, visible y comprobable. No traducir cada texto con llamadas a IA durante la ejecución.
3. Añadir en **Ajustes → Apariencia/Idioma** selector persistente; permitir aplicar cultura sin reiniciar el proceso si es viable sin alterar análisis, o justificar y señalar claramente cuándo hace falta reinicio.
4. Mantener intactas claves JSON, nombres de campos, `AppSettings`, rutas, hashes SHA, identificadores de estados, comandos ClamAV, firmas, URLs API, ids de temas y argumentos del instalador. Separar mensajes de máquina de visualizaciones traducidas.
5. Proteger variables, placeholders, atajos, Markdown de ayuda, privacidad, consentimiento remoto de IA, nombres de proveedores y exenciones de responsabilidad (Guard no bloquea preventivamente; no sustituye Defender).
6. Usar `PalClam/MainWindow*.cs`, `PalClam/Services/HelpContentCatalog.cs`, `SecurityAdviceCatalog.cs`, `MainWindow.Help.cs`, `MainWindow.OptionsTabs.cs`, `UI/*`, `SettingsStore.cs` y `PalClam.Service/*` como puntos de partida; no traducir cadenas técnicas de la capa de motor que deban ser estables.
7. Añadir pruebas de claves faltantes/duplicadas, fallback, formatos, cambio de idioma con análisis en curso, siete paletas, combinaciones de contraste, DPI 125%/150%, botones largos, textos RTL futuros y ventanas pequeñas.
8. Mantener `AppId` original de Inno Setup para actualizaciones y un verificador compatible con UTF-8 en Windows PowerShell 5.1. Incrementar la versión a 1.2.0 solo en la rama nueva.
9. Entregar **ZIP completo + parche** y registro de cambios; compilar y ejecutar pruebas Windows antes de afirmar estabilidad.

### Prioridad y método

Primera fase: infraestructura de localización y 15-30 cadenas de Ayuda/Ajustes y cabecera, con pruebas y selector persistente. Segunda fase: navegación y Smart Scan. Tercera fase: módulos especializados y mensajes de error. Cuarta fase: traducción completa, QA visual y aceptación.

**Reglas innegociables:** ClamAV y firmas no se alteran; secretos nunca en recursos ni en los registros; ningún archivo de usuario se manda automáticamente a IA; no activar publicación/desinstalación/acciones peligrosas desde recursos traducidos.
