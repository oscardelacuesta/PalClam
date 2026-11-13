// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PalClam.Infrastructure;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private string _inspectorSelectedPath = "";
    private FileInspectionReport? _inspectorReport;
    private CancellationTokenSource? _inspectorReadCancellation;
    private bool _inspectorScanActive;
    private TextBox _inspectorPathBox = null!;
    private TextBox _inspectorHashBox = null!;
    private TextBlock _inspectorReadStatus = null!;
    private TextBlock _inspectorFileData = null!;
    private TextBlock _inspectorSignature = null!;
    private TextBlock _inspectorWarning = null!;
    private TextBlock _inspectorScanStatus = null!;
    private ProgressBar _inspectorBusyIndicator = null!;
    private Button _inspectorScanButton = null!;
    private Button _inspectorCopyButton = null!;
    private Button _inspectorCancelReadButton = null!;
    private Button _inspectorReportButton = null!;
    private string _inspectorLastLogPath = "";

    private void BuildInspectorPage(StackPanel page)
    {
        page.Children.Add(T("Inspector de archivos", 26, Ink, FontWeights.Bold));
        page.Children.Add(T("Metadatos, hash SHA-256, certificado Authenticode y resultado de ClamAV en una sola vista.",
            12, Muted, FontWeights.Normal, new Thickness(0, 7, 0, 17)));

        var drop = new Border
        {
            BorderBrush = Purple, BorderThickness = new Thickness(1.5),
            Background = PurplePale, CornerRadius = new CornerRadius(16),
            Padding = new Thickness(20, 19, 20, 19), Margin = new Thickness(0, 0, 0, 14)
        };
        var dropContent = new StackPanel { HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
        dropContent.Children.Add(T("⌕  SUELTA AQUÍ UN ARCHIVO", 15, Purple, FontWeights.Bold));
        dropContent.Children.Add(T("O pulsa Examinar. Se analiza localmente y nunca se ejecuta el archivo.",
            11, Muted, FontWeights.Normal, new Thickness(0, 7, 0, 0)));
        drop.Child = dropContent;
        page.Children.Add(drop);

        var selectRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        selectRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        selectRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _inspectorPathBox = Input("Selecciona un archivo...");
        _inspectorPathBox.IsReadOnly = true;
        _inspectorPathBox.ToolTip = "Ruta del archivo que está siendo inspeccionado";
        _inspectorPathBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
        selectRow.Children.Add(_inspectorPathBox);
        var browse = ActionButton("Examinar archivo", false, (_, _) => PickInspectorFile());
        browse.Margin = new Thickness(10, 0, 0, 0);
        Grid.SetColumn(browse, 1);
        selectRow.Children.Add(browse);
        page.Children.Add(selectRow);

        var actionRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 11) };
        _inspectorScanButton = ActionButton("◈  Analizar con ClamAV", true,
            (_, _) => _ = StartInspectorScanAsync());
        _inspectorScanButton.Margin = new Thickness(0, 0, 9, 7);
        _inspectorScanButton.ToolTip = "Reutiliza el análisis oficial de PalClam; no modifica ni mueve el archivo";
        actionRow.Children.Add(_inspectorScanButton);
        _inspectorCancelReadButton = ActionButton("Cancelar lectura", false,
            (_, _) => _inspectorReadCancellation?.Cancel());
        _inspectorCancelReadButton.Margin = new Thickness(0, 0, 9, 7);
        actionRow.Children.Add(_inspectorCancelReadButton);
        _inspectorReportButton = ActionButton("Ver informe ClamAV", false,
            (_, _) => { if (!string.IsNullOrWhiteSpace(_inspectorLastLogPath)) OpenLocalReport(_inspectorLastLogPath); });
        _inspectorReportButton.Margin = new Thickness(0, 0, 9, 7);
        actionRow.Children.Add(_inspectorReportButton);
        page.Children.Add(actionRow);

        _inspectorBusyIndicator = new ProgressBar
        {
            Height = 4, IsIndeterminate = true, Visibility = Visibility.Collapsed,
            Foreground = Purple, Background = PurplePale, Margin = new Thickness(0, 0, 0, 10)
        };
        page.Children.Add(_inspectorBusyIndicator);
        _inspectorReadStatus = T("Selecciona un archivo para comenzar.", 12, Muted);
        _inspectorReadStatus.TextWrapping = TextWrapping.Wrap;
        _inspectorReadStatus.Margin = new Thickness(0, 0, 0, 12);
        page.Children.Add(_inspectorReadStatus);

        _inspectorFileData = T("Pendiente de inspección.", 12, Slate);
        _inspectorFileData.TextWrapping = TextWrapping.Wrap;
        page.Children.Add(InspectorCard("01  INFORMACIÓN DEL ARCHIVO", _inspectorFileData));

        var hashContent = new StackPanel();
        hashContent.Children.Add(T("SHA-256 · identificación del contenido leído", 11, Muted,
            FontWeights.SemiBold, new Thickness(0, 0, 0, 8)));
        var hashRow = new Grid();
        hashRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        hashRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _inspectorHashBox = Input("Sin calcular");
        _inspectorHashBox.IsReadOnly = true;
        _inspectorHashBox.FontFamily = new FontFamily("Consolas");
        _inspectorHashBox.FontSize = 12;
        _inspectorHashBox.ToolTip = "Hash SHA-256 hexadecimal. No es un diagnóstico de malware.";
        hashRow.Children.Add(_inspectorHashBox);
        _inspectorCopyButton = ActionButton("Copiar", false, (_, _) => CopyInspectorHash());
        _inspectorCopyButton.Margin = new Thickness(10, 0, 0, 0);
        Grid.SetColumn(_inspectorCopyButton, 1);
        hashRow.Children.Add(_inspectorCopyButton);
        hashContent.Children.Add(hashRow);
        hashContent.Children.Add(T("El hash identifica los bytes leídos, pero no garantiza que el archivo sea seguro.",
            10, Muted, FontWeights.Normal, new Thickness(0, 9, 0, 0)));
        page.Children.Add(InspectorCard("02  HUELLA DIGITAL", hashContent));

        _inspectorSignature = T("Pendiente de inspección.", 12, Slate);
        _inspectorSignature.TextWrapping = TextWrapping.Wrap;
        var signatureContent = new StackPanel();
        signatureContent.Children.Add(_inspectorSignature);
        signatureContent.Children.Add(T("Solo se identifica el certificado Authenticode embebido (cuando existe). " +
            "No se valida la integridad, la cadena de confianza ni la revocación de la firma.",
            10, Muted, FontWeights.Normal, new Thickness(0, 9, 0, 0)));
        page.Children.Add(InspectorCard("03  FIRMA DIGITAL · INFORMACIÓN LIMITADA", signatureContent));

        _inspectorWarning = T("", 11, Amber);
        _inspectorWarning.TextWrapping = TextWrapping.Wrap;
        _inspectorWarning.Visibility = Visibility.Collapsed;
        page.Children.Add(_inspectorWarning);

        _inspectorScanStatus = T("Todavía no se ha iniciado un análisis con ClamAV desde el inspector.", 12, Slate);
        _inspectorScanStatus.TextWrapping = TextWrapping.Wrap;
        page.Children.Add(InspectorCard("04  RESULTADO CLAMAV", _inspectorScanStatus));
        page.Children.Add(T("El Inspector no ejecuta el archivo, no lo envía a Internet y no toma acciones automáticas " +
            "de cuarentena. Un resultado sin detecciones no garantiza ausencia de amenazas.",
            10, Muted, FontWeights.Normal, new Thickness(0, 2, 0, 12)));
        RefreshInspectorActions();
    }

    private static Border InspectorCard(string heading, UIElement body)
    {
        var content = new StackPanel();
        content.Children.Add(T(heading, 12, Purple, FontWeights.Bold, new Thickness(0, 0, 0, 12)));
        content.Children.Add(body);
        return new Border
        {
            Background = Surface, BorderBrush = Outline, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14), Padding = new Thickness(18),
            Margin = new Thickness(0, 0, 0, 13), Child = content
        };
    }

    private void PickInspectorFile()
    {
        if (_inspectorScanActive || _busy || _installingAppUpdate) return;
        var dialog = new OpenFileDialog
        {
            Title = "Selecciona un archivo para inspeccionar (sin ejecutar)",
            Filter = "Todos los archivos (*.*)|*.*", CheckFileExists = true
        };
        if (PalClam.UI.LocalizedDialogs.Show(dialog, this) == true) _ = InspectSelectedFileAsync(dialog.FileName);
    }

    private async Task InspectSelectedFileAsync(string path)
    {
        if (_inspectorScanActive || _busy || _installingAppUpdate) return;
        _inspectorReadCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        _inspectorReadCancellation = cancellation;
        _inspectorSelectedPath = path;
        _inspectorReport = null;
        _inspectorLastLogPath = "";
        _inspectorPathBox.Text = path;
        _inspectorHashBox.Text = PalClam.Localization.DisplayText.Translate("Calculando SHA-256...");
        _inspectorFileData.Text = "Leyendo metadatos del archivo...";
        _inspectorSignature.Text = "Consultando certificado Authenticode, si está disponible...";
        _inspectorWarning.Visibility = Visibility.Collapsed;
        _inspectorScanStatus.Text = "Pendiente: elige Analizar con ClamAV cuando termine la inspección.";
        _inspectorReadStatus.Text = "Leyendo archivo localmente. Puedes cancelar la operación.";
        _inspectorReadStatus.Foreground = Purple;
        _inspectorBusyIndicator.Visibility = Visibility.Visible;
        RefreshInspectorActions();
        try
        {
            var result = await FileInspectorService.InspectAsync(path, cancellation.Token);
            if (cancellation.IsCancellationRequested || !ReferenceEquals(_inspectorReadCancellation, cancellation))
                return;
            _inspectorReport = result;
            _inspectorPathBox.Text = result.FullPath;
            _inspectorHashBox.Text = result.Sha256;
            _inspectorFileData.Text = $"Nombre: {result.Name}\n" +
                $"Tamaño: {FileInspectorService.FormatSize(result.SizeBytes)} ({result.SizeBytes:N0} bytes)\n" +
                $"Extensión declarada: {(result.Extension.Length == 0 ? "(ninguna)" : result.Extension)} " +
                "· no se verifica el formato real\n" +
                $"Creación (Windows): {result.CreatedLocal:dd/MM/yyyy HH:mm:ss}\n" +
                $"Última modificación: {result.ModifiedLocal:dd/MM/yyyy HH:mm:ss}";
            var cert = result.Certificate;
            _inspectorSignature.Text = cert.Status + (cert.Subject.Length == 0 ? "" :
                $"\nTitular: {cert.Subject}\nEmisor: {cert.Issuer}\nHuella del certificado: {cert.Thumbprint}" +
                $"\nVigencia del certificado: {cert.NotBefore:dd/MM/yyyy} – {cert.NotAfter:dd/MM/yyyy}");
            _inspectorWarning.Text = result.NameWarning;
            _inspectorWarning.Visibility = result.NameWarning.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            _inspectorReadStatus.Text = "Inspección local terminada. El archivo NO se ha ejecutado ni enviado fuera del equipo.";
            _inspectorReadStatus.Foreground = Green;
        }
        catch (OperationCanceledException)
        {
            if (ReferenceEquals(_inspectorReadCancellation, cancellation))
            {
                _inspectorReadStatus.Text = "Lectura cancelada: no hay huella SHA-256 válida.";
                _inspectorReadStatus.Foreground = Amber;
                _inspectorHashBox.Text = "No calculado";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or
            System.Security.SecurityException or NotSupportedException)
        {
            if (ReferenceEquals(_inspectorReadCancellation, cancellation))
            {
                _inspectorReadStatus.Text = "No se ha podido inspeccionar el archivo: " + ex.Message;
                _inspectorReadStatus.Foreground = Red;
                _inspectorFileData.Text = "Inspección incompleta.";
                _inspectorHashBox.Text = "No disponible";
                _inspectorSignature.Text = "No comprobada";
            }
        }
        finally
        {
            if (ReferenceEquals(_inspectorReadCancellation, cancellation))
            {
                _inspectorReadCancellation = null;
                _inspectorBusyIndicator.Visibility = Visibility.Collapsed;
            }
            cancellation.Dispose();
            RefreshInspectorActions();
        }
    }

    private async Task StartInspectorScanAsync()
    {
        var report = _inspectorReport;
        if (report is null || _inspectorScanActive || _busy || _installingAppUpdate) return;
        if (!File.Exists(report.FullPath))
        {
            _inspectorScanStatus.Text = "El archivo ya no existe. Vuelve a seleccionarlo.";
            _inspectorScanStatus.Foreground = Amber;
            return;
        }
        _inspectorScanActive = true;
        _inspectorLastLogPath = "";
        _inspectorScanStatus.Text = "Iniciando análisis ClamAV mediante el motor de PalClam...";
        _inspectorScanStatus.Foreground = Purple;
        RefreshInspectorActions();
        var requestedAt = DateTimeOffset.Now;
        try
        {
            await StartScanAsync(report.FullPath);
            // No confundir un informe antiguo de la misma ruta con el recién solicitado.
            var recent = ScanHistory.ReadRecent(8).FirstOrDefault(item =>
                item.CompletedAt >= requestedAt &&
                string.Equals(item.Target, report.FullPath, StringComparison.OrdinalIgnoreCase));
            if (recent is null)
            {
                _inspectorScanStatus.Text = "No hay resultado nuevo registrado. Comprueba el motor, las firmas y el panel de análisis.";
                _inspectorScanStatus.Foreground = Amber;
                return;
            }
            var verdict = ScanVerdictPolicy.Evaluate(recent.ExitCode, recent.Detections,
                recent.Errors, recent.Detections);
            _inspectorScanStatus.Foreground = verdict.Status == ScanVerdictStatus.Clean ? Green :
                verdict.HasThreats ? Red : Amber;
            _inspectorScanStatus.Text = $"Resultado: {recent.Outcome}\n" +
                $"Detecciones: {recent.Detections:N0} · Errores: {recent.Errors:N0} · " +
                $"Archivos confirmados: {(recent.EngineScannedFiles >= 0 ? recent.EngineScannedFiles.ToString("N0") : "no disponible")}\n" +
                $"ClamAV: código {(recent.ExitCode?.ToString() ?? "sin código")}. " +
                "Si hubo errores o cancelación, el resultado es incompleto; sin detecciones no significa seguro.";
            try
            {
                var current = new FileInfo(report.FullPath);
                current.Refresh();
                if (!current.Exists || current.Length != report.SizeBytes ||
                    current.LastWriteTime != report.ModifiedLocal)
                {
                    PalClam.UI.PresentationLocalization.Append(_inspectorScanStatus,
                        "\nAviso: el archivo ha cambiado desde el cálculo del SHA-256; " +
                        "la huella mostrada ya no identifica necesariamente los bytes analizados.");
                    _inspectorScanStatus.Foreground = Amber;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                PalClam.UI.PresentationLocalization.Append(_inspectorScanStatus,
                    "\nNo se ha podido verificar si el archivo cambió tras inspeccionarlo.");
            }
            _inspectorLastLogPath = recent.LogPath;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _inspectorScanStatus.Text = "El análisis terminó, pero no se pudo recuperar su historial: " + ex.Message;
            _inspectorScanStatus.Foreground = Amber;
        }
        finally
        {
            _inspectorScanActive = false;
            RefreshInspectorActions();
        }
    }

    private void CopyInspectorHash()
    {
        if (_inspectorReport is null) return;
        try
        {
            Clipboard.SetText(_inspectorReport.Sha256);
            _inspectorReadStatus.Text = "SHA-256 copiado al portapapeles.";
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            _inspectorReadStatus.Text = "El portapapeles está ocupado. Vuelve a intentarlo.";
            _inspectorReadStatus.Foreground = Amber;
        }
    }

    private void RefreshInspectorActions()
    {
        if (_inspectorScanButton is null) return;
        _inspectorScanButton.IsEnabled = _inspectorReport is not null && !_inspectorScanActive &&
            _inspectorReadCancellation is null && !_busy && !_installingAppUpdate;
        _inspectorCopyButton.IsEnabled = _inspectorReport is not null;
        _inspectorCancelReadButton.IsEnabled = _inspectorReadCancellation is not null;
        _inspectorReportButton.IsEnabled = !_inspectorScanActive &&
            _inspectorLastLogPath.Length > 0 && File.Exists(_inspectorLastLogPath);
    }
}
