# Validación obligatoria de PalClam 1.0.0 RC1

## 1. Antes de compilar (Windows 10/11 x64)

- Instalar SDK .NET 10 x64 e Inno Setup 6 reciente/7; no se necesita Visual Studio.
- Ejecutar `PRUEBAS.bat` (debe devolver 0: pruebas de parser y rutas + compilación WPF).
- Ejecutar `CREAR_INSTALADOR.bat` con Internet y comprobar que **falla** si no logra preparar el motor y las firmas offline.
- Verificar el EXE, el icono y los textos del instalador. Instalar y desinstalar con usuario estándar.
- Conservar fuera del proyecto la clave privada del firmador de actualizaciones. NUNCA subirla al servidor.

## 2. Escaneo y estados (manual)

- Abrir PalClam sin Internet, comprobar firmas y motor, y analizar archivos inofensivos.
- Interrumpir un escaneo grande y verificar estado «Cancelado», no «Sin detecciones».
- Simular archivo inaccesible y comprobar estado «Incompleto / errores».
- Verificar que el porcentaje es estimado y los archivos de archivos comprimidos no se confunden con 100% garantizado.
- Verificar exportación TXT e historial. Revisar rutas personales antes de compartir un informe.

## 3. Cuarentena (manual, únicamente archivos inofensivos)

- Aislar un archivo de prueba, restaurarlo, comprobar SHA-256, ubicación y permisos.
- Restaurar con destino ya existente: debe negarse sin sobrescribir.
- Restaurar con carpeta original eliminada o sustituida por enlace: debe negarse.
- No utilizar malware real en un equipo de producción; para pruebas de detección puede utilizarse EICAR conforme a sus instrucciones.

## 4. Programador, bandeja y Guard

- Programar un análisis diario y semanal con una cuenta sin permisos de administrador.
- Comprobar creación, ejecución, cancelación y borrado de tareas sin dejar tareas huérfanas.
- Configurar Guard, generar modificaciones repetidas; comprobar cola, eventos perdidos y ausencia de bloqueo de ejecución.
- Cerrar ventana: queda en bandeja si está habilitado; salir desde menú contextual: termina el proceso y la vigilancia.
- Revisar iconos de analizando/pausado/error/alerta/actualizando y cambios de estado.

## 5. Mail, Outlook y actualizaciones

- Examinar `.eml` y `.msg` inofensivos, adjuntos y mensajes mayores que el límite; verificar omisiones y eliminación de temporales.
- Comprobar Outlook con una cuenta de pruebas Microsoft 365/Outlook.com, delegación Mail.Read y revocación posterior.
- Verificar firmador RSA, fallo ante manifiesto manipulado, hash incorrecto y cancelación.
- Publicar una RC en entorno de prueba y luego un estable 1.0.0: comprobar que la RC detecta la estable.
- Confirmar restauración de motor anterior si falla actualización y uso offline sin conexión.

**Hasta superar esta matriz, no publicar como «1.0 estable».**

## Publicación

`PUBLICAR_ACTUALIZACION.bat` está intencionadamente deshabilitado en RC1.
Antes de habilitarlo en la 1.0.0 estable, cambiar `Version` a `1.0.0`,
conservar la misma clave pública/privada, construir y verificar el instalador,
y completar esta matriz en Windows.
