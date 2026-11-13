# Pruebas de aceptación — PalClam 1.1.0 Preview 9

1. Ejecuta `PRUEBAS.bat`. Deben pasar los casos previos y el verificador de fuentes.
2. Ejecuta `EMPEZAR_AQUI.bat`, instala sobre Preview 8 y comprueba que mantiene los datos y las firmas.
3. Verifica que **Opciones y personalización** está siempre visible **fuera de Herramientas**; abre desde barra lateral, cabecera y `Ctrl+,`.
4. Cambia entre las seis pestañas, modifica ajustes y comprueba que persisten después de reiniciar.
5. Cambia cada uno de los seis temas y el texto al 90%, 100%, 110%, 120%, 130%; verifica que la barra flotante, pestañas, diálogos y controles mantienen contraste y no se cortan.
6. En Opciones > Análisis, activa barra flotante y elige Vista Compacta/Detallada. Inicia análisis en carpeta con > 300 archivos inofensivos.
7. Comprueba que la barra aparece, se puede **arrastrar** desde la cabecera, ocultar sin cancelar el análisis y abrir Detalles.
8. Pausa durante el lote actual: debe indicar Pausando antes de Pausado. No se deben iniciar lotes nuevos mientras esté pausado.
9. Reanuda y finaliza; comprueba que la barra desaparece al terminar y que el historial registra los resultados.
10. Repite con Detener: debe quedar Incompleto, nunca Limpio. Revisa el informe de errores y detecciones.
11. Activa/desactiva Siempre encima, en modo compacto y detallado; valida en resolución 1366×768 y con escala de Windows 125%/150%.
12. Comprueba que PalClam AI, Guard, Mail, Explorador, firmas, cuarentena y actualización siguen siendo accesibles y que ninguna eliminación se realiza sin confirmación.

La ventana flotante ofrece **último archivo confirmado** y el archivo en examen solo en modo Preciso. En modo Equilibrado indica lote, no inventa un archivo exacto.


## FIX1: selector rápido de temas (11/11/2026)

1. Ejecutar `PRUEBAS.bat` y `EMPEZAR_AQUI.bat`; confirmar que ya no aparece `CS0104 TabControl`.
2. En Inicio, abrir `TEMA DE COLOR` desde la cabecera y comprobar seis opciones con sus muestras.
3. Elegir Azul océano: debe cambiar la interfaz y el nombre del botón sin entrar en Opciones.
4. Ir a Opciones > Apariencia: el selector original debe mostrar Azul océano.
5. Con el monitor flotante abierto, cambiar a Cobre elegante: debe cambiar también el monitor.
6. Cerrar y reiniciar PalClam: el tema seleccionado debe mantenerse.
7. Repetir con todos los temas y tamaño de texto al 130 %, en ventana mínima y maximizada.
8. Pulsar `Más opciones de apariencia…`: debe abrir la pestaña Apariencia.
9. Restablecer preferencias: el botón de cabecera debe volver a `Morado clásico`.

No probar con archivos maliciosos; basta una carpeta de documentos inofensivos.


## FIX3: regresión de temas con análisis activo (11/11/2026)

**Este caso es obligatorio antes de distribuir el instalador.**

1. En una instalación de Preview 9 FIX3, ejecutar `PRUEBAS.bat`. No basta con pasar SmokeTests: debe compilar WPF sin errores.
2. Activar la barra flotante e iniciar un análisis de una carpeta con al menos 300 archivos **inofensivos**. Esperar a que aparezcan resultados y mantener el análisis activo.
3. Desde el selector de la cabecera, cambiar consecutivamente entre los seis temas mientras la barra flotante está visible. La ventana **no debe cerrarse**, la cola no debe reiniciarse y los contadores deben seguir avanzando.
4. Desde **Opciones > Apariencia**, repetir los seis temas; las pestañas seleccionadas deben cambiar de color sin quedar con tonos del tema anterior.
5. Con el mismo análisis en curso, cambiar los textos 90 % → 130 % → 100 %. Comprobar que el tamaño es estable, sin multiplicarse, y que no aparece error ni bloqueo.
6. En modo Preciso, pausar entre lotes, cambiar de tema, reanudar y comprobar que se sigue exactamente desde la cola pendiente. Repetir al detener el análisis para validar la marca *Incompleto*.
7. Ocultar y volver a mostrar el monitor flotante; modificar color y escala y validar que la barra responde y no salta de posición.
8. Cerrar y volver a abrir PalClam, comprobar persistencia de tema y tamaño y que Guard, AI, Mail, cuarentena y los controles de análisis siguen disponibles.
9. Si algo falla, abrir `%LOCALAPPDATA%\PalClam\Diagnostics\fallos.log`. Contiene contexto, tipos de excepción y pila resumida. Revisar el contenido antes de compartirlo.

**No probado automáticamente en el entorno de preparación:** no dispone de Windows / .NET 10 WPF. Las pruebas manuales anteriores siguen siendo necesarias.


## FIX3: revisión visual de pestañas de Opciones

1. Abrir Opciones en 960×660, 1366×768 y pantalla maximizada. Verificar que ninguna de las seis pestañas queda cortada y que se adaptan a una segunda fila cuando no caben.
2. Pasar el puntero, seleccionar cada pestaña y navegar con Tab y flechas. Revisar el borde, el subrayado, el foco y los iconos vectoriales (sin glifos ausentes).
3. Repetir en los seis temas: color del icono, contraste de texto y pestaña activa deben acompañar al tema sin arrastrar colores antiguos.
4. En **Apariencia**, probar texto al 90 %, 100 % y 130 % en ventana estrecha, con la escala de pantalla Windows al 125 %. No debe existir scroll horizontal ni solapamiento.
5. Entrar desde la cabecera (`Más opciones de apariencia…`) y desde Actualizaciones (`Configurar actualizaciones`). Deben seleccionarse respectivamente los índices 1 y 3.
6. Editar cada sección y cerrar/reabrir PalClam. Confirmar persistencia; no deben haberse duplicado controles ni perdido eventos.
7. Con el análisis activo, alternar entre pestañas y cambiar el tema repetidamente desde cabecera y desde Opciones. Los contadores y el estado de pausa/detención deben mantenerse.
