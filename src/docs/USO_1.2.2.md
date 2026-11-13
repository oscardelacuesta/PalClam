# Uso de las ediciones 1.2.2

1. Construye con EMPEZAR_AQUI.bat. Las cinco salidas se encuentran en `dist/installer` y `dist/portable`.
2. Para instalar, comparte el `Setup.exe` automático o el instalador específico. El modo de instalación y las autorizaciones de Windows se mantienen; la protección de servicio sigue siendo opcional.
3. Para llevar en USB, extrae el ZIP portable completo y ejecuta PalClam.exe. Evita una carpeta de solo lectura y conserva el marcador palclam.portable.
4. En la edición instalada se conserva `%LOCALAPPDATA%\PalClam`. La portable utiliza su carpeta `Data`, incluida la cuarentena y sus informes. No copies esas carpetas personales dentro de un paquete para difusión.
5. Las licencias están en `Licenses` y las fuentes en `Source`. Deben acompañar los archivos redistribuidos; no se deben separar para reducir tamaño.
6. Al pasar la portable a otro PC, revisa las carpetas vigiladas y las rutas de análisis. Vuelve a introducir las claves IA si Windows no puede descifrarlas con tu identidad actual.
7. Para actualizar una portable, cierra el programa, conserva una copia de Data y extrae la nueva edición en su lugar. Su interfaz no aplica instaladores sobre la carpeta portable.

En Windows x64 se recomienda la edición x64; la x86 permite difundir el mismo producto a sistemas Windows de 32 bits compatibles con .NET 10. El motor y los paquetes de actualización no se mezclan entre arquitecturas. No ejecutes instaladores sobre la carpeta de una portable.

Para correo, LED y monitor de comportamiento, consulta también USO_1.2.0.md: sus funciones se conservan. El monitor avisa de patrones sospechosos en carpetas vigiladas; no garantiza impedir un cifrado y no recupera archivos cifrados.

`palclam.com` se muestra como la futura web del producto. No se ha configurado como servidor activo de actualizaciones ni se ha publicado una web desde este paquete.
