# Validación manual — PalClam 1.1.0 Preview 3

1. Ejecutar `PRUEBAS.bat` y verificar que los smoke tests incluyen 3 casos nuevos de consejos.
2. Ejecutar `CREAR_INSTALADOR.bat` y comprobar la salida `PalClam_1.1.0_Preview3_Setup_win-x64.exe`.
3. Arrancar y entrar en Análisis o PalClam AI; pulsar escudo y texto «PalClam» en la cabecera. Debe abrir el Panel principal y NO interrumpir un análisis en curso.
4. Desplegar Herramientas. Debajo aparece el personaje, sin deformaciones, junto con un consejo y dos botones: «Otro consejo» y «Ver todos».
5. Pulsar «Otro consejo» varias veces: cambia el texto, no cambia ninguna preferencia ni se lanza análisis.
6. Pulsar «Ver todos»: abrir «Consejos y estado» con el catálogo, botones y estado local.
7. Comprobar que se informa correctamente de motor sin instalar, sin firmas, vigilancia opcional inactiva y último análisis incompleto; no presentar resultados como limpieza certificada.
8. Pulsar los accesos del Centro: abrir Motor y firmas, Análisis, Diagnóstico, Historial, Cuarentena, Guard y AI. No hay acciones destructivas automáticas.
9. F1 abre Consejos y Alt+Inicio vuelve al Panel principal (en cualquier página).
10. En ventana de 960x660 el personaje permanece accesible con el desplazamiento del lateral.
11. Confirmar conservación de las funciones Preview 2: cuarentena, restauración y eliminación bajo consentimiento; motor offline, firmas, Guard, Outlook y AI opt-in.
12. Ejecutar `REVISAR_ENTREGA.bat`. Revisar licencia de uso público del personaje antes de distribuir.

Esta lista contiene comprobaciones pendientes en Windows, no resultados ya superados.
