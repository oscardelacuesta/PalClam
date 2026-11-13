# Pruebas RC1 — Cierre visual previo a estable

Estas pruebas son adicionales a las de RC1 FIX2. No equivalen a una aprobación automática de versión estable.

1. Ejecutar `PRUEBAS.bat`: el verificador debe superar fase [1/5] con PowerShell 5.1 y compilar WPF/Service/Tests con .NET 10.
2. Ejecutar `PalClam.ThemeTests`: comprobar que carga la plantilla XAML, alterna los 7 temas y no se congela un pincel.
3. Cabecera: verificar que **Tema · Ayuda · Ajustes** aparece sin solaparse a 100%, 125% y 150% y con ventana reducida al mínimo.
4. Ayuda: botón abre la página actual; el enlace de Herramientas y Mayús+F1 siguen disponibles. Comprobar retorno por navegación.
5. Barras: verificar arrastre, clic en pista, rueda, teclado, barras horizontales y verticales, en ListView/TextBox/ScrollViewer (los controles con scroll propio mantienen funcionalidad).
6. Tema Oscuro violeta: cambio en caliente desde cabecera y Opciones, guardado tras reiniciar, contraste de errores, alertas, botones, estados, inputs y enlaces. No iniciar/parar escaneo al cambiar tema.
7. Contraste alto de Windows: los controles nativos deben seguir utilizables; revisar los cambios en caliente.
8. Servicio y cuarentena: repetir pruebas RC1 de arranque, latido, firmas, detección, aislamiento reversible y desinstalación.
9. Instalar desde cero y actualizar desde RC1 FIX2; comprobar que el AppId no cambia y que no desaparecen ajustes/cuarentena.
10. Hasta completar todas las anteriores, no marcar `docs/ACEPTACION_1.1.0_RC1.ini` como aprobada.
