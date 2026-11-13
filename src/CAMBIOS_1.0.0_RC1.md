# PalClam 1.0.0 RC1 — consolidación

Versión candidata, NO versión estable verificada ni sustituto de Microsoft Defender.

## Cambios técnicos

- Compilación para .NET 10, sin Visual Studio, con `COMPILAR.bat`, `CREAR_INSTALADOR.bat` y `PRUEBAS.bat`.
- Pruebas automatizadas independientes del escritorio para el parser de ClamAV, contadores, estimación de progreso, exclusiones y SHA-256.
- El aislamiento compara la huella SHA-256 antes y después del traslado a cuarentena; discrepancias requieren revisión y bloquean la restauración.
- La restauración rechaza directorios con puntos de reanálisis y nunca sobrescribe un archivo existente.
- Corrección del cierre para actualizar la aplicación desde la bandeja: el proceso debe finalizar realmente antes del instalador.
- Aviso en la bandeja la primera vez que se cierra la ventana sin salir.
- Versionado previo a estable: 1.0.0-rc.1. Si se firma y publica 1.0.0 estable, el actualizador podrá detectarla como actualización desde RC. La comparación normaliza la revisión 1.0.0.0 del ensamblado frente a 1.0.0 del manifiesto.
- Diagnóstico actualizado para describir correctamente que Guard es vigilancia de cambios, no protección preventiva.

## Funciones anteriores conservadas

Análisis local, ClamAV offline y actualizable, informes e historial, cuarentena, programación, Guard, Mail EML/MSG/OFT, Outlook Graph (opt-in), iconos residentes y actualizaciones de aplicación.

## Límites y requisitos pendientes

- Sin compilación WPF ni prueba de instalación realizada en este entorno: **no declarar validación en Windows**.
- Requiere .NET 10 SDK e Inno Setup para construir; el instalador integra ClamAV y firmas offline preparados durante la construcción con conexión.
- No hay bloqueo preventivo en tiempo real. Guard puede omitir eventos y corre solo mientras PalClam esté abierto.
- Outlook requiere registro de aplicación Microsoft Entra y consentimiento delegado Mail.Read.
- PalClam AI queda para 1.1.0, no incluido.
- Revisar licencia, marcas, firmas y procedencia del paquete final antes de distribución pública.
