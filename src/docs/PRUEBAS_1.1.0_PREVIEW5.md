# PalClam 1.1.0 Preview 5 — Temas, accesibilidad y controles de análisis

- Seis temas **claros** completos: Morado clásico, Azul océano, Verde esmeralda, Cobre elegante, Crema y ciruela, Grafito claro. No se anuncia un tema oscuro incompleto.
- Tema persistido por usuario; cambios inmediatos en fondos, botones, bordes, cabecera, barra lateral y tarjetas (pinceles compartidos mutables).
- Escala de texto de 90 %, 100 %, 110 %, 120 % y 130 %, guardada y aplicada a páginas preparadas. Valores inválidos se restablecen a 100 %.
- Los colores semánticos rojo/ámbar/verde no cambian de significado. Pruebas automáticas de contraste y consistencia.
- Detener más visible también junto a la selección de análisis; se conserva la barra inferior fija. Pausa aparece expresamente **no disponible**: clamscan no puede pausarse con fiabilidad.
- Mantiene Self-Defense observacional, Guard, IA multi-proveedor, remediación confirmada, cuarentena, correo y análisis offline.
- No se cambia el AppId de Inno ni se publica la versión Preview automáticamente.

## Pruebas requeridas en Windows
1. Abrir Opciones y elegir cada uno de los seis temas, comprobando Inicio, Análisis, Herramientas, Guard, Mail y AI.
2. Reiniciar PalClam y comprobar que se mantiene el tema seleccionado.
3. Alternar repetidamente 90 -> 130 -> 100 % y comprobar que el texto recupera el tamaño original sin acumulación.
4. Reiniciar y comprobar persistencia del tamaño. Verificar ventana de 960x660 y resolución 1600x900.
5. Durante un análisis, revisar Detener arriba y abajo; comprobar que ambos reflejan Deteniendo y desaparecen al finalizar.
6. Verificar que Pausar permanece deshabilitado con su explicación. No debe aparentar que funciona.
7. Ejecutar PRUEBAS.bat, CREAR_INSTALADOR.bat y VALIDAR_PAQUETE.bat. No distribuir sin estas comprobaciones.

## Conformidad manual
- [ ] Temas verificados tras cambiar y reiniciar
- [ ] Texto 90/100/110/120/130 y restauración
- [ ] Detener arriba/abajo y estado correcto
- [ ] Reintentar tras cancelación
- [ ] Módulos anteriores intactos

## Ayuda, características y fecha
8. Abrir **Herramientas → Ayuda · Funciones y ventajas** y comprobar las cinco ventajas, todas las características y las limitaciones sin falsos reclamos de protección preventiva.
9. Pulsar en varios botones «Ver función» y comprobar que conducen a Análisis, Cuarentena, Guard, IA, Correo, Outlook y Self-Defense sin ejecutar acciones.
10. Comprobar F1 (consejos) y Mayús+F1 (ayuda). Comprobar desde «Acerca de» el nuevo enlace a la ayuda.
11. Cambiar los seis temas en Ayuda y volver a Inicio: las tarjetas, bordes y botones deben usar el tema elegido.
12. Verificar la ayuda a tamaños 90 %, 100 % y 130 %, especialmente a resolución 960×660 y 1600×900.
13. Comprobar que no aparece «.NET 10» en la cabecera, pie ni «Acerca de», y que el pie muestra la fecha local de Windows.
14. Reiniciar la app, conservar tema/escala y verificar que la fecha del pie sigue siendo actual.

La prueba visual WPF y de instalación solo puede cerrarse al ejecutar el instalador en Windows.
