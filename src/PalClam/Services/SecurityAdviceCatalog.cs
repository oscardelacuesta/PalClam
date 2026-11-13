// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

// Consejos informativos. Cada accion conduce solo a una pagina interna existente.
// Nunca provoca escaneos, borrados, cambios de preferencias ni consultas de IA.
internal sealed record SecurityAdviceItem(string Title, string Text, string Page);

internal static class SecurityAdviceCatalog
{
    internal static IReadOnlyList<SecurityAdviceItem> Items { get; } =
    [
        new("Actualiza las firmas", "Antes de un análisis importante, comprueba la fecha de la base local. Una base presente no implica que esté al día.", "signatures"),
        new("Analiza las descargas", "Comprueba los archivos recién descargados antes de abrirlos. Un resultado limpio no garantiza ausencia de malware.", "scan"),
        new("Atención a los adjuntos", "Examina los mensajes y adjuntos sospechosos. Evita abrir enlaces o ejecutar archivos de remitentes desconocidos.", "mail"),
        new("Cuarentena primero", "Si hay una detección, aísla el archivo antes de decidir. Restaurar o eliminar exige confirmación explícita.", "quarantine"),
        new("Vigila los resultados incompletos", "Una cancelación, un error de lectura o archivos omitidos impiden afirmar que todo está limpio.", "history"),
        new("Guard no bloquea ejecuciones", "PalClam Guard observa cambios cuando está activo, pero no ofrece prevención en tiempo real. Conserva Microsoft Defender.", "guard"),
        new("Mide el rendimiento", "Consulta CPU, RAM y velocidad del motor. El último archivo informado no tiene por qué ser el que se analiza ahora.", "history"),
        new("IA solo cuando tú decidas", "PalClam AI es opcional. Revisa qué datos vas a consultar y autoriza expresamente cualquier petición remota.", "ai"),
        new("Revisa antes de compartir informes", "El diagnóstico y los registros pueden contener rutas personales. Elimina información sensible antes de enviarlos.", "diagnostics"),
        new("Autoprotección: aún pendiente", "Esta versión no incluye un servicio protegido contra manipulación. Ningún icono verde debe interpretarse como invulnerabilidad.", "about")
    ];
}
