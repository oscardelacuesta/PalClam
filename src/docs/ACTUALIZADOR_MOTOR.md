# Motor actualizable sin reinstalar PalClam

El instalador de PalClam 0.8.0 contiene ClamAV 1.5.4 y firmas iniciales para análisis offline. Cuando existe conexión, la comprobación automática (activada por defecto) o el botón del panel del motor actualiza ClamAV sin desinstalar el motor incluido. Se consulta la API `https://api.github.com/repos/Cisco-Talos/clamav/releases/latest` y se descarga únicamente el ZIP portable `clamav-VERSION.win.x64.zip` del repositorio oficial. Se exige digest SHA-256 de GitHub para versiones futuras; si falta, se rechaza. El SHA de ClamAV 1.5.4 está fijado adicionalmente en el código.

- Verificar siempre versión, origen HTTPS, hash y tamaño.
- Limitar extracción y rechazar recorridos de directorio, symlinks y archivos incompletos.
- Mantener una carpeta por versión; activar solo tras probar los ejecutables. No borrar versión anterior en fallo.
- No interrumpir un análisis ni iniciar la actualización del motor durante FreshClam.
- Enlaces a ediciones oficiales: https://www.clamav.net/downloads y https://github.com/Cisco-Talos/clamav/releases
- Una release en GitHub puede no incluir inmediatamente ZIP para Windows. En ese caso, PalClam no cambia de motor y comunica el error.
- GitHub puede limitar solicitudes (rate limit) y pueden existir firewalls/proxies. Sin acceso, no se realizan descargas y se conserva la versión instalada.

El mecanismo necesita pruebas de seguridad y de instalación reales en Windows. La verificación SHA-256 basada en metadatos de GitHub no sustituye a verificar de manera independiente la firma de Cisco sobre el binario.
