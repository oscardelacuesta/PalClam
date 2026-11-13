# PalClam 1.1.0 Preview 18 — Centro de notificaciones

- Nueva sección **Centro de notificaciones** en Protección, acceso rápido desde el panel principal y desde la bandeja.
- Historial conjunto: incidencias de Guard personal (`%LocalAppData%\PalClam\Guard\incidencias*.jsonl`) y registros del servicio experimental (`%ProgramData%\PalClam\MachineGuard\eventos*.log`).
- Filtros por gravedad, origen, texto libre y eventos sin revisar; contadores de detecciones, advertencias y errores.
- Marcado de revisión por usuario en archivo local independiente. **No elimina ni altera las auditorías**.
- Exportación voluntaria de filas filtradas a TXT; la información puede contener rutas privadas.
- Lectura acotada (hasta 1,2 MB por cada uno de cuatro ficheros, máximo 400 eventos visibles), con avisos ante líneas corruptas, fuentes inaccesibles o truncamiento.
- Actualización al entrar en la sección, manual y cada ~10 segundos mientras se visualiza.
- Sin comunicación privilegiada nueva, sin nuevas acciones de cuarentena o bloqueo y sin modificar el servicio de Windows.
- Pruebas SmokeTests para lectura combinada, filtros, persistencia del marcador y registros inválidos.
- Pruebas WPF y compilaciones Windows siguen pendientes en el equipo del desarrollador.

Ver `docs/PRUEBAS_1.1.0_PREVIEW18.md`.
