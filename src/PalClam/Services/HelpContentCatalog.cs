// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

// Contenido de ayuda verificable: describe capacidades reales, sin sugerir
// protección preventiva, desinfección total ni autodefensa invulnerable.
internal sealed record HelpFeature(string Title, string Detail, string Page);

internal static class HelpContentCatalog
{
    internal static IReadOnlyList<HelpFeature> Advantages { get; } =
    [
        new("Gratuito y con código fuente", "PalClam es gratuito y el código propio está bajo GPLv3 o posterior. Los servicios externos de IA pueden requerir suscripción o cobrar por uso.", "about"),
        new("Convive con Microsoft Defender", "PalClam no desactiva Defender y puede instalarse como herramienta complementaria de análisis. Mantén Defender activo; dos productos pueden consumir recursos al mismo tiempo.", "diagnostics"),
        new("Tú decides qué se elimina", "Cuarentena, restauración y eliminación exigen confirmación explícita. No se borran amenazas automáticamente.", "quarantine"),
        new("Análisis local y sin conexión", "El motor ClamAV funciona localmente con las firmas disponibles en el equipo. Las actualizaciones de firmas, Outlook y la IA remota sí necesitan Internet.", "signatures"),
        new("Privacidad bajo control", "Los análisis y registros son locales; Outlook y las consultas de IA son funciones opcionales que requieren autorización para usar servicios externos.", "ai")
    ];

    internal static IReadOnlyList<HelpFeature> Capabilities { get; } =
    [
        new("Smart Scan PRO", "Perfiles Rápido, Completo y Personalizado: rutas, prioridades y lotes controlados. El modo completo solicita confirmación y puede tardar mucho.", "scan"),
        new("Análisis rápido y personalizado", "Examina archivos y carpetas con ClamAV. Puedes pausar entre lotes, reanudar y detener; el modo preciso informa del archivo individual, a costa de velocidad.", "scan"),
        new("Inspector local de archivos", "Examina metadatos y SHA-256 sin ejecutar el archivo. Puede identificar un certificado Authenticode, sin verificar confianza o integridad, y lanzar el análisis de ClamAV.", "inspector"),
        new("Motor y firmas actualizables", "Comprueba la versión del motor, la base local, la fecha orientativa de firmas y fallos de FreshClam. Una base instalada no implica que esté actualizada.", "signatures"),
        new("Análisis programados", "Configura análisis en momentos elegidos, con informes y registro del resultado.", "schedule"),
        new("Cuarentena y eliminación manual", "Aísla detecciones, restáuralas cuando corresponda y elimina definitivamente elementos aislados con confirmación.", "quarantine"),
        new("Security Health 360", "Consulta el estado de motor, firmas, Guard, servicio y monitores. Un componente no comprobado no se presenta como seguro.", "health"),
        new("USB Watch y Download Guard", "Observan dispositivos y descargas cuando se activan; no sustituyen un bloqueo antivirus preventivo.", "devices"),
        new("Machine Guard", "Servicio opcional de vigilancia en el equipo, con comprobación de latido y errores del motor; requiere instalación y permisos apropiados.", "health"),
        new("PalClam Guard", "Vigila cambios en archivos y permite examinar actividad. Es vigilancia opcional, no bloqueo preventivo de ejecución.", "guard"),
        new("Análisis de correo", "Permite examinar correos exportados y adjuntos compatibles mediante PalClam Mail, filtrando detecciones, avisos heurísticos y elementos sin examinar.", "mail"),
        new("Outlook en línea opcional", "Puede acceder a mensajes mediante Microsoft Graph con los permisos que autorice el usuario.", "outlook"),
        new("AI Security Analyst", "Prepara consultas guiadas sobre análisis, avisos, salud y cuarentena. Muestra el texto y exige autorización del usuario antes de cualquier consulta remota.", "ai"),
        new("Inteligencia artificial opcional", "Permite consultar diferentes proveedores o modelos locales para interpretar resultados. No decide por ClamAV ni elimina archivos.", "ai"),
        new("Rendimiento e historial", "Registra archivos por segundo, estimaciones, consumo de CPU/RAM, informes y comparaciones de análisis cuando son equivalentes.", "history"),
        new("Self-Defense · Monitor", "Supervisión opcional de interrupciones y registro de incidencias. No impide que malware con privilegios cierre PalClam.", "selfdefense"),
        new("Consejos y diagnóstico", "Ofrece recomendaciones, verificaciones locales, diagnósticos e información de estado para orientar al usuario.", "advice")
    ];

    internal static IReadOnlyList<string> Limitations { get; } =
    [
        "PalClam no sustituye Microsoft Defender ni bloquea preventivamente la ejecución de amenazas.",
        "Un análisis sin detecciones no garantiza que el equipo esté libre de malware. Cancelaciones y errores no se consideran resultados limpios.",
        "Eliminar un archivo no garantiza desinfectar todos los componentes del sistema.",
        "Self-Defense es un monitor, no un servicio protegido contra manipulaciones. La pausa solo actúa entre lotes: el lote en curso no se interrumpe, y en modo equilibrado no se conoce el archivo exacto.",
        "La IA y Outlook son opcionales. Las API de terceros pueden transmitir el texto autorizado y aplicar sus propias políticas o tarifas."
    ];
}
