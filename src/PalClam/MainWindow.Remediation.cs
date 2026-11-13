// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private TextBlock? _quarantineReviewInfo;

    private void UpdateRemediationSelection()
    {
        if (_quarantineReviewInfo is null || _quarantineList is null) return;
        var selected = _quarantineList.SelectedItems.OfType<QuarantineEntry>().ToArray();
        if (selected.Length == 0)
        {
            _quarantineReviewInfo.Text = "Selecciona uno o varios elementos aislados para reanalizarlos sin restaurarlos.";
            return;
        }
        if (selected.Length > 1)
        {
            _quarantineReviewInfo.Text = $"{selected.Length} seleccionados · Reanálisis en lote disponible " +
                "(máximo 20 por operación). Restaurar y eliminar siguen siendo individuales.";
            return;
        }
        var entry = selected[0];
        _quarantineReviewInfo.Text =
            "SHA-256: " + entry.Sha256 + "\n" +
            "Última revisión ClamAV: " + entry.RecheckLabel + "\n" +
            "Detalle: " + (string.IsNullOrWhiteSpace(entry.LastRecheckDetail) ? "No se ha vuelto a analizar" : entry.LastRecheckDetail) + "\n" +
            "Posible falso positivo: " + entry.ReviewLabel +
            "\nLas revisiones no autorizan restaurar automáticamente.";
    }

    private async Task RecheckQuarantineSelectedAsync()
    {
        if (_busy || _remediationRunning)
        {
            MessageBox.Show(this, "Hay otra operación en curso.", "Remediation Center");
            return;
        }
        var selected = _quarantineList.SelectedItems.OfType<QuarantineEntry>().ToArray();
        if (selected.Length is 0 or > 20 || selected.Any(x => !QuarantineReviewPolicy.CanRecheck(x)))
        {
            MessageBox.Show(this,
                "Selecciona entre 1 y 20 archivos que sigan aislados y sin incidencias de integridad.",
                "Remediation Center", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show(this,
            $"¿Volver a analizar {selected.Length} elemento(s) de cuarentena con las firmas locales?\n\n" +
            "No se restaurará, ejecutará, eliminará ni enviará ningún archivo. " +
            "Los resultados limpios NO garantizan seguridad.",
            "Reanálisis de cuarentena", MessageBoxButton.YesNo,
            MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;

        _remediationRunning = true;
        var success = 0;
        var errors = new List<string>();
        try
        {
            for (var i = 0; i < selected.Length; i++)
            {
                _quarantineStatus.Text = $"Reanalizando {i + 1}/{selected.Length}: " +
                    System.IO.Path.GetFileName(selected[i].OriginalPath);
                try
                {
                    var result = await QuarantineRescanService.RecheckAsync(selected[i], CancellationToken.None);
                    success++;
                    AppendLog("[REMEDIACION] " + selected[i].Id + " · " + result.DisplayVerdict);
                }
                catch (Exception ex)
                {
                    errors.Add(System.IO.Path.GetFileName(selected[i].OriginalPath) + ": " + ex.Message);
                    try { RemediationAuditLog.Append("Reanalizar", selected[i].Id,
                        "No concluyente", ex.Message); }
                    catch (IOException) { /* el fallo de auditoría no oculta el error inicial */ }
                    catch (UnauthorizedAccessException) { }
                    AppendLog("[REMEDIACION] Reanálisis no concluyente: " + ex.Message);
                }
            }
        }
        finally
        {
            _remediationRunning = false;
            RefreshQuarantine();
        }
        MessageBox.Show(this,
            $"Reanálisis registrados: {success}/{selected.Length}.\n" +
            (errors.Count == 0 ? "Sin incidencias de ejecución." :
                "Incidencias:\n" + string.Join("\n", errors.Take(5))) +
            "\n\nUna ausencia de detecciones no confirma que sea seguro restaurar.",
            "Remediation Center", MessageBoxButton.OK,
            errors.Count == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private async Task FlagQuarantineForReviewAsync()
    {
        if (_busy || _remediationRunning) return;
        var selected = _quarantineList.SelectedItems.OfType<QuarantineEntry>().ToArray();
        if (selected.Length is 0 or > 20 || selected.Any(x => !QuarantineReviewPolicy.CanRecheck(x)))
        {
            MessageBox.Show(this, "Selecciona entre 1 y 20 archivos aislados válidos.",
                "Revisión de falso positivo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show(this,
            $"¿Marcar {selected.Length} elemento(s) para revisar posibles falsos positivos?\n\n" +
            "Esta marca es administrativa, NO declara archivos limpios ni restaura nada.",
            "Solicitar revisión", MessageBoxButton.YesNo,
            MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        _remediationRunning = true;
        var errors = new List<string>();
        try
        {
            foreach (var entry in selected)
            {
                try { await QuarantineStore.FlagManualReviewAsync(entry); }
                catch (Exception ex) { errors.Add(ex.Message); }
            }
        }
        finally { _remediationRunning = false; RefreshQuarantine(); }
        if (errors.Count > 0)
            MessageBox.Show(this, "Algunas marcas no pudieron registrarse:\n" +
                string.Join("\n", errors.Take(3)), "Revisión manual",
                MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void ExportRemediationReport()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Title = "Guardar informe local de Remediation Center",
                Filter = "Texto UTF-8 (*.txt)|*.txt", DefaultExt = ".txt", AddExtension = true,
                FileName = "PalClam_Remediation_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".txt"
            };
            if (PalClam.UI.LocalizedDialogs.Show(dialog, this) != true) return;
            var entries = QuarantineStore.List();
            var audit = RemediationAuditLog.ReadRecent();
            var report = new StringBuilder()
                .AppendLine("PalClam 1.1.0 · Informe local de cuarentena")
                .AppendLine("Generado: " + DateTimeOffset.Now)
                .AppendLine("Advertencia: sin detecciones no significa archivo seguro; los registros son locales, no inmutables.")
                .AppendLine("No se incluyen los archivos aislados ni se envían datos a Internet.")
                .AppendLine("\nELEMENTOS (últimos 500)");
            foreach (var e in entries)
                report.AppendLine($"{e.QuarantinedAt:u} | {e.Id} | {e.State} | {e.OriginalPath}")
                    .AppendLine($"  Detección original: {e.Detection} | SHA-256: {e.Sha256}")
                    .AppendLine($"  Reanálisis: {e.RecheckLabel} | {e.LastRecheckDetail} | {e.ReviewLabel}");
            report.AppendLine("\nACCIONES (últimas 250)");
            foreach (var a in audit)
                report.AppendLine($"{a.When:u} | {a.Actor} | {a.EntryId} | {a.Action} | {a.Outcome} | {a.Detail}");
            File.WriteAllText(dialog.FileName, report.ToString(), new UTF8Encoding(false));
            _quarantineStatus.Text = "Informe guardado localmente: " + dialog.FileName;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo exportar: " + ex.Message,
                "Remediation Center", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
