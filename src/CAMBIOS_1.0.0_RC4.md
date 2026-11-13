# PalClam 1.0.0 RC4 — Estado de detención y Acerca de

## Cambios respecto a RC3

- **Detener análisis**: el botón rojo solo se muestra mientras una operación esté en curso. Al solicitar cancelación pasa a ámbar y muestra «Deteniendo…» deshabilitado hasta la confirmación del motor. Al terminar desaparece. Si quedó incompleto aparece «Reintentar análisis incompleto».
- **Indicador de estado**: se diferencia «Sin análisis en curso», «Finalizado» y «Interrumpido/incompleto», sin presentar una cancelación como resultado limpio.
- **Acerca de PalClam**: apartado nuevo en Herramientas y acceso desde Inicio; autor Óscar de la Cuesta, perfil profesional, enlaces, atribuciones ClamAV, GPLv2, bibliotecas de terceros, privacidad, limitaciones de la protección y estado legal previo a publicación.
- **Distribución**: instalador RC4, mismo AppId para actualización sobre RC3, nueva clase `MainWindow.About.cs` incluida automáticamente en el proyecto y el código fuente del instalador.
- **Sin nuevos motores ni modificación de ClamAV**: se conservan los demás módulos y las opciones anteriores.

## Validación necesaria

1. `PRUEBAS.bat`: ejecuta pruebas y compilación WPF .NET 10.
2. `CREAR_INSTALADOR.bat`: genera el instalador offline RC4.
3. Con PalClam abierto, inicia análisis, pulsa Detener: botón ámbar «Deteniendo…», después oculto, estado «Cancelado», opción Reintentar visible.
4. Ejecuta un análisis hasta el final: sin botón rojo, indicación de finalización y sin opción Reintentar.
5. Abre Herramientas → Acerca de PalClam; prueba enlaces y documentos desde el instalador.
6. Comprueba que la instalación conserva configuraciones, firmas, historial, Guard y el acceso desde Outlook/Mail.

**Nota:** Las comprobaciones estáticas no equivalen a compilación y pruebas de Windows; el canal estable continúa bloqueado.
