# PalClam 1.1.0 Preview 3 — Navegación, personaje y consejos

## Novedades

- **Escudo y marca superior pulsables:** vuelven al Panel principal desde cualquier pestaña, sin afectar al análisis activo.
- **Personaje original del autor:** imagen suministrada por el usuario, integrada de forma local como recurso en el lateral, justo debajo de Herramientas.
- **Consejos que cambian por decisión del usuario:** botón «Otro consejo», sin temporizadores invasivos ni llamadas a Internet.
- **Centro de recomendaciones:** nueva pantalla con consejos explicativos y botones a secciones ya existentes; muestra el estado real del motor, presencia y fecha de firmas, estado de Guard y último análisis.
- **Accesibilidad:** F1 abre consejos y Alt+Inicio vuelve al panel principal.
- **Seguridad:** no se activan acciones de limpieza, cambios de configuraciones ni IA desde el personaje. No se modifica ClamAV, cuarentena, Guard o los proveedores de IA.
- **Pruebas:** tres comprobaciones nuevas del catálogo, enlaces internos y advertencias de seguridad.

## Precauciones

El GIF del usuario **contiene un fotograma**, de modo que se presenta como imagen estática, sin añadir animaciones sintéticas. La autoría y los derechos de reproducción de la imagen deben confirmarse antes de distribuir públicamente la aplicación. El centro no certifica la limpieza del sistema. PalClam sigue sin protección anti-manipulación del proceso mediante servicio Windows.

## Instalador y versiones

1.1.0-preview.3; instalación con el mismo AppId de Inno Setup. Proyecto WPF .NET 10 x64, BAT habituales, validación offline y canal de publicación estable bloqueado. La compilación Windows debe probarse con `PRUEBAS.bat` y `CREAR_INSTALADOR.bat`.
