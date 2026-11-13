# PalClam 1.1.0 Preview 5 — Temas, accesibilidad y controles de análisis

- Seis temas **claros** completos: Morado clásico, Azul océano, Verde esmeralda, Cobre elegante, Crema y ciruela, Grafito claro. No se anuncia un tema oscuro incompleto.
- Tema persistido por usuario; cambios inmediatos en fondos, botones, bordes, cabecera, barra lateral y tarjetas (pinceles compartidos mutables).
- Escala de texto de 90 %, 100 %, 110 %, 120 % y 130 %, guardada y aplicada a páginas preparadas. Valores inválidos se restablecen a 100 %.
- Los colores semánticos rojo/ámbar/verde no cambian de significado. Pruebas automáticas de contraste y consistencia.
- Detener más visible también junto a la selección de análisis; se conserva la barra inferior fija. Pausa aparece expresamente **no disponible**: clamscan no puede pausarse con fiabilidad.
- Ventanas secundarias de historial y rendimiento también heredan los colores y escala guardados.
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

## Ayuda, información y fecha
- **Herramientas → Ayuda · Funciones y ventajas** con cinco ventajas, once capacidades comprobables, cinco límites y accesos a sus páginas.
- Información explícita de gratuidad (GPLv2), compatibilidad complementaria con Defender, uso offline y remediación solo con confirmación.
- Mayús+F1 abre Ayuda, F1 mantiene Consejos. Nuevo botón desde «Acerca de».
- Etiquetas técnicas «.NET 10» eliminadas de cabecera, pie y «Acerca de» (permanece como tecnología interna).
- Fecha local de Windows en el pie, actualizada por el temporizador de la interfaz.
- Nuevas pruebas de catálogo y correspondencia de enlaces de ayuda.
- Documentación incorporada: `docs/AYUDA_FUNCIONES_VENTAJAS.md`.
