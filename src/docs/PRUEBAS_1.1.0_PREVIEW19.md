# Pruebas de aceptación — Preview 19

1. Descomprime el ZIP completo en una carpeta nueva y ejecuta `PRUEBAS.bat`.
   Deben pasar el verificador, las pruebas funcionales, las 360 alternancias WPF,
   el proyecto PalClam y la compilación/autoprueba del servicio.
2. Abre **Security Health 360**. Sin ClamAV o firmas, debe mostrar **REVISAR**;
   el servicio sin latido no puede aparecer operativo. Las funciones optativas
   desactivadas deben mostrar **NO ACTIVADO**.
3. Activa **USB Watch** sin análisis automático. Conecta una memoria reconocida
   por Windows como *extraíble*. Comprueba que aparece y genera un evento de
   información en **Centro de notificaciones → USB Watch**. El programa no debe
   analizar por el mero hecho de conectarla.
4. Pulsa **Analizar unidad seleccionada** y comprueba que inicia el flujo habitual
   de ClamAV si hay motor y firmas. No debe ejecutar archivos ni modificar la unidad.
5. Activa el análisis automático USB y conecta **otra** unidad extraíble. Debería
   iniciar un análisis solo si el motor está disponible y sin tareas en marcha.
   Prueba de nuevo mientras otra tarea analiza: no debe iniciarse una segunda.
6. Desactiva USB Watch, cierra y reinicia PalClam: la preferencia persiste.
   Las unidades ya conectadas antes de habilitar la opción no deben autoanalizarse.
7. Activa **Download Guard**, con motor y firmas presentes. Comprueba en Guard que
   Descargas está vigilada (si no, detén e inicia Guard). Crea un archivo de texto
   inocuo en Descargas y verifica el registro de análisis reactivo sin duplicados.
8. Cambia entre los seis temas y tamaños de letra hasta 130 %. Prueba los estados
   mientras corre un análisis, con Guard detenido y con servicio no instalado.
9. Prueba `EMPEZAR_AQUI.bat`, instalación, inicio al entrar en Windows, reinicio y
   desinstalación. Mantener el servicio experimental sin activar en producción.

**Límites:** FileSystemWatcher no ofrece prevención real ni garantiza eventos
si Windows los pierde. La clasificación de unidades extraíbles omite algunos discos
USB reportados por Windows como unidades fijas. Un registro no acredita detección.
