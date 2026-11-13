# PalClam 1.0.0 RC2 — rediseño visual y usabilidad

Se ha partido de la versión 1.0.0 RC1 FIX2 **compilada e instalada en Windows** por el usuario.

## Cambios visibles
- Encabezado actualizado y panel principal con hero de marca, actividad y módulos.
- Menú lateral compacto con grupos desplegables: Inicio y análisis, Protección, Correo y Herramientas. Mantiene todas las secciones y las rutas de navegación.
- Eliminada la barra vertical gris del menú: se desplaza con la rueda en ventanas pequeñas.
- Botones redondeados con jerarquía de color: azul análisis, rojo detener, ámbar pausa, verde estados verificados y violeta para navegación.
- Distinción entre motor disponible, firmas locales, vigilancia opcional y último resultado. **No se afirma protección total ni tiempo real**.
- Tarjetas y estados con color condicionado a la situación real, sin inventar métricas.
- Instalador y BAT renombrados a RC2; mantiene AppId de Inno Setup, la GPLv2, motor offline y módulos existentes.

## Precauciones
- Este paquete contiene **fuentes** y BAT; requiere SDK .NET 10, Inno Setup y preparación offline en el equipo Windows para producir el instalador.
- No hay nuevos módulos antivirus ni cambios en el motor, la cuarentena, Outlook ni el actualizador.
- RC2 no se publica automáticamente al canal estable.
- La compilación WPF/Inno Setup no puede ejecutarse en este entorno Linux. Ejecutar PRUEBAS.bat y CREAR_INSTALADOR.bat antes de instalar y validar los estados y navegación.
