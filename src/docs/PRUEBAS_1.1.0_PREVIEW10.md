# Pruebas de aceptación — PalClam 1.1.0 Preview 10

**Plataforma:** Windows 10/11 x64 con SDK .NET 10. La compilación WPF, la ejecución del escáner ClamAV y el instalador no están probados en Linux.

## 1. Pruebas previas obligatorias

1. Descomprime el ZIP completo fuera de `Archivos de programa`, por ejemplo en una carpeta de desarrollo.
2. Ejecuta `PRUEBAS.bat`. Debe completar cuatro etapas: validación estática del código, SmokeTests, `PalClam.ThemeTests` (360 alternancias con controles WPF) y compilación de PalClam para win-x64.
3. Si cualquier paso falla, **no instales ni distribuyas**. Conserva las últimas líneas del error y el archivo señalado; para fallos de compilación incluye el código CSxxxx.

## 2. Reproducción del fallo original (OBLIGATORIA)

1. Arranca PalClam y selecciona una carpeta con muchos archivos inofensivos (no la carpeta de Windows ni un recurso remoto). Comprueba que hay motor y firmas ClamAV válidos.
2. Inicia un análisis largo con modo «Equilibrado»; activa la barra flotante opcional. Espera hasta que el contador de archivos crezca.
3. Con el análisis **en curso**, alterna desde la cabecera todos los temas: morado, océano, esmeralda, cobre, crema y grafito. Haz cinco ciclos completos.
4. Entra en Opciones → Apariencia mientras escanea; alterna temas por el desplegable y los botones de muestra. Repite cinco ciclos.
5. Cambia la selección entre las seis pestañas. Comprueba iconos, texto, contraste, indicador inferior y foco de teclado; ningún panel debe desaparecer ni perder sus valores.
6. Mientras el escaneo continúa, alterna una vez la escala de texto de 100 % a 130 % y devuélvela a 100 %. Verifica que no se acumulan incrementos de tamaño.
7. Pausa entre lotes, cambia de tema, reanuda y finaliza o detén. El resumen no debe afirmar que los archivos no analizados están limpios.
8. Cambia de tema con la barra flotante visible y oculta; revisa colores, avisos, acciones Pausar/Detener y conteos. No debe abrirse otra instancia del motor.
9. Cierra y reabre PalClam. Confirma que se conservan el último tema y el tamaño del texto.

**Criterio de éxito:** no hay cierres inesperados ni diálogo de errores, ClamAV sigue procesando, los resultados y estados siguen siendo coherentes y los cambios visuales se reflejan en todas las vistas. Si hay cierre, consultar `%LOCALAPPDATA%\PalClam\Diagnostics\fallos.log`.

## 3. Validación adicional

- Probar con el escaneo PAUSADO, con progreso indeterminado y con muchos resultados en pantalla.
- Probar temas en una pantalla HiDPI y con ventana al mínimo permitido.
- Probar teclado Tab / Shift+Tab al cambiar de pestaña.
- Comprobar que no se bloquean los controles del Guard, las firmas, la cuarentena o la IA.
- Ejecutar `EMPEZAR_AQUI.bat` solo después de superar las cuatro pruebas; crear instalador y ejecutar `REVISAR_ENTREGA.bat` en un entorno de pruebas.

## 4. Limitación de la prueba automática

`PalClam.ThemeTests` utiliza la misma clase de estilos que la interfaz, pero no ejecuta un análisis ClamAV. Su resultado exitoso no certifica ausencia de errores en todos los controladores de eventos de la aplicación. La validación real del informe debe completarse en Windows.
