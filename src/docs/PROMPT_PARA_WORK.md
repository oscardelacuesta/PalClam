# Prompt de traspaso a ChatGPT Work

Voy a adjuntar **PalClam 1.1.0 — paquete completo de código congelado**. Trabaja sobre **una copia** y crea una rama/versión **1.2.0 multilenguaje**. Primero lee `docs/TRASPASO_A_WORK_MULTILENGUAJE.md`, `docs/PRUEBAS_1.1.0_FINAL.md` y `docs/ACEPTACION_1.1.0_FINAL.ini`.

**Objetivo:** ES/EN/FR/DE/PT/IT con `.resx` (o equivalente mantenible), selector de idioma persistente en Ajustes, fallback español, localización completa de cabecera, menús, ayuda, diagnósticos UI, correos, avisos e IA. No traduzcas identificadores técnicos, comandos, nombres de archivos ni claves de configuración.

**Restricciones:** conservar .NET 10 WPF, ClamAV, interfaz actual con 7 temas incluido Oscuro violeta, aurora configurable, barras personalizadas, Ayuda junto a Ajustes, Machine Guard y aislamiento de privilegios, AppId Inno Setup, privacidad, firma de actualizaciones y bloqueo de publicación. No introducir regresiones ni atribuir a Guard bloqueo preventivo. No alterar fuentes congeladas de 1.1.0.

Empieza por diseñar la arquitectura de recursos y una primera implementación con pruebas que demuestren cambio de idioma y fallback; sigue por módulos priorizados, no un reemplazo masivo sin tests. Prepara proyecto completo, parche y documentación de pruebas. Si no hay entorno Windows, informa explícitamente de lo no ejecutado.
