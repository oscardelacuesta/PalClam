// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private readonly OutlookGraphClient _outlookClient = new();
    private readonly ObservableCollection<OutlookRow> _outlookRows = new();
    private TextBox _outlookClientId = null!;
    private TextBlock _outlookStatus = null!;
    private TextBlock _outlookDeviceCode = null!;
    private ComboBox _outlookFolder = null!;
    private ComboBox _outlookLimit = null!;
    private Button _outlookScanButton = null!;
    private Button _outlookConnectButton = null!;
    private Button _outlookDisconnectButton = null!;
    private Button _outlookStopButton = null!;
    private CancellationTokenSource? _outlookCancellation;
    private bool _outlookScanning;

    private void BuildOutlookPage(StackPanel page)
    {
        page.Children.Add(T("Outlook / Microsoft 365", 24, Ink, FontWeights.Bold, new Thickness(0,0,0,8)));
        page.Children.Add(T("Analiza de forma voluntaria mensajes recientes de la carpeta elegida mediante Microsoft Graph. " +
            "Funciona con Outlook.com y buzones Microsoft 365 autorizados. No es vigilancia continua ni lee PST/OST.",
            12, Muted, FontWeights.Normal, new Thickness(0,0,0,14)));
        var box = Card(Surface, Outline);
        var panel = new StackPanel();
        panel.Children.Add(T("Conexión de solo lectura", 16, Ink, FontWeights.Bold, new Thickness(0,0,0,9)));
        panel.Children.Add(T("Registra PalClam como aplicación pública en Microsoft Entra (cuentas personales y de empresa); " +
            "habilita device code flow y el permiso delegado Mail.Read. Introduce el ID de aplicación, NO una clave secreta.",
            12, Muted, FontWeights.Normal, new Thickness(0,0,0,8)));
        panel.Children.Add(T("ID de aplicación (Client ID)",12,Ink,FontWeights.SemiBold,new Thickness(0,0,0,5)));
        _outlookClientId = new TextBox { Text = _settings.OutlookClientId, MaxLength=36,
            Margin=new Thickness(0,0,0,12),Padding=new Thickness(8,6,8,6) };
        panel.Children.Add(_outlookClientId);
        var row = new WrapPanel();
        _outlookConnectButton = ActionButton("Conectar Outlook", true, async (_,_) => await ConnectOutlookAsync());
        _outlookDisconnectButton = ActionButton("Desconectar", false, async (_,_) => await DisconnectOutlookAsync());
        row.Children.Add(_outlookConnectButton); row.Children.Add(_outlookDisconnectButton);
        panel.Children.Add(row);
        _outlookDeviceCode = T("",13,Purple,FontWeights.SemiBold,new Thickness(0,8,0,4));
        _outlookDeviceCode.TextWrapping = TextWrapping.Wrap;
        panel.Children.Add(_outlookDeviceCode);
        var copy = ActionButton("Copiar código",false,(_,_) =>
        {
            var code = _outlookDeviceCode.Tag as string;
            if (!string.IsNullOrEmpty(code)) Clipboard.SetText(code);
        });
        panel.Children.Add(copy);
        _outlookStatus = T("Sin cuenta conectada.",12,Muted,FontWeights.Normal,new Thickness(0,7,0,0));
        _outlookStatus.TextWrapping = TextWrapping.Wrap;
        panel.Children.Add(_outlookStatus);
        box.Child = panel; page.Children.Add(box);

        var scan = Card(Surface, Outline);
        scan.Margin = new Thickness(0,12,0,0);
        var stack = new StackPanel();
        stack.Children.Add(T("Analizar carpeta de Outlook",16,Ink,FontWeights.Bold,new Thickness(0,0,0,7)));
        stack.Children.Add(T("Número de mensajes recientes (sin sincronización de todo el buzón). El análisis " +
            "descarga temporalmente el contenido MIME, aplica ClamAV y elimina los temporales.",
            12,Muted,FontWeights.Normal,new Thickness(0,0,0,9)));
        _outlookFolder = new ComboBox { ItemsSource = new[] { "Bandeja de entrada", "Enviados", "Correo no deseado", "Archivo", "Borradores" },
            SelectedIndex = 0, MinWidth = 220, HorizontalAlignment = System.Windows.HorizontalAlignment.Left, Margin = new Thickness(0,0,0,10) };
        stack.Children.Add(_outlookFolder);
        _outlookLimit = new ComboBox { ItemsSource=new[] {5,10,25,50},
            SelectedItem=_settings.OutlookMessageLimit,Width=110,HorizontalAlignment= System.Windows.HorizontalAlignment.Left,
            Margin=new Thickness(0,0,0,10) };
        _outlookLimit.SelectionChanged += (_,_) =>
        {
            if (_outlookLimit.SelectedItem is int n) { _settings.OutlookMessageLimit=n;SaveSettings(); }
        };
        stack.Children.Add(_outlookLimit);
        var actions = new WrapPanel();
        _outlookScanButton=ActionButton("Analizar mensajes recientes",true, async (_,_)=>await ScanOutlookAsync());
        _outlookStopButton=ActionButton("Cancelar",false,(_,_)=>_outlookCancellation?.Cancel());
        _outlookStopButton.IsEnabled=false;
        actions.Children.Add(_outlookScanButton);actions.Children.Add(_outlookStopButton);
        stack.Children.Add(actions);
        stack.Children.Add(T("Un correo sin detecciones NO equivale a correo seguro. Los mensajes omitidos " +
            "se informarán como incompletos; los correos originales nunca se modifican.",
            11,Muted,FontWeights.Normal,new Thickness(0,11,0,7)));
        var results = new ListView { ItemsSource=_outlookRows,MinHeight=180,MaxHeight=310,
            BorderBrush=Outline,BorderThickness=new Thickness(1) };
        var columns=new GridView();
        columns.Columns.Add(new GridViewColumn{Header="Asunto", Width=285,DisplayMemberBinding=new Binding(nameof(OutlookRow.Subject))});
        columns.Columns.Add(new GridViewColumn{Header="Resultado",Width=135,DisplayMemberBinding=new Binding(nameof(OutlookRow.Status))});
        columns.Columns.Add(new GridViewColumn{Header="Detalle",Width=330,DisplayMemberBinding=new Binding(nameof(OutlookRow.Detail))});
        results.View=columns;
        stack.Children.Add(results);
        scan.Child=stack;page.Children.Add(scan);
    }

    private async Task ConnectOutlookAsync()
    {
        if (_outlookScanning || _busy) return;
        _outlookConnectButton.IsEnabled=false;
        _outlookDeviceCode.Text="Preparando autorización en Microsoft...";
        _outlookDeviceCode.Tag=null;
        _outlookCancellation=new CancellationTokenSource(TimeSpan.FromMinutes(15));
        try
        {
            var id=_outlookClientId.Text.Trim();
            if (!Guid.TryParse(id,out _)) throw new ArgumentException("Client ID no válido. Registra la aplicación en Entra.");
            _settings.OutlookClientId=id; SaveSettings();
            await _outlookClient.ConnectAsync(id,(url,code)=>Dispatcher.BeginInvoke(new Action(()=>
            {
                _outlookDeviceCode.Text=$"Abre {url} y escribe el código: {code} (caduca).";
                _outlookDeviceCode.Tag=code;
            })),_outlookCancellation.Token);
            _outlookDeviceCode.Text="Autorización completada.";
            _outlookStatus.Text="Cuenta conectada (solo lectura): "+_outlookClient.AccountName;
        }
        catch (OperationCanceledException) { _outlookStatus.Text="Conexión cancelada o caducada."; }
        catch (Exception ex) { _outlookStatus.Text="No conectado: "+ex.Message; }
        finally { _outlookConnectButton.IsEnabled=true; _outlookCancellation.Dispose(); _outlookCancellation=null; }
    }

    private async Task DisconnectOutlookAsync()
    {
        if (_outlookScanning) { _outlookStatus.Text="Cancela el análisis antes de desconectar."; return; }
        await _outlookClient.DisconnectAsync();
        _outlookStatus.Text="Desconectado. Tokens de esta sesión eliminados de memoria.";
        _outlookDeviceCode.Text=""; _outlookDeviceCode.Tag=null;
    }

    private async Task ScanOutlookAsync()
    {
        if (_outlookScanning || _busy) return;
        if (!_outlookClient.Connected) { _outlookStatus.Text="Conecta primero Outlook con autorización Mail.Read.";return; }
        var scanner=ClamAvLocator.FindScanner();
        if (!ClamAvLocator.IsValidScanner(scanner) || !SignatureStore.HasSignatures())
        { _outlookStatus.Text="No hay motor ClamAV o firmas operativas."; return; }
        _outlookRows.Clear();
        _outlookScanning=true;_outlookStopButton.IsEnabled=true;_outlookScanButton.IsEnabled=false;
        _outlookCancellation = new CancellationTokenSource();
        SetBusy(true); UpdateTrayState();
        var token=_outlookCancellation.Token;
        try
        {
            var maximum=(int)(_outlookLimit.SelectedItem ?? 10);
            var folderId = new[] { "inbox", "sentitems", "junkemail", "archive", "drafts" }[Math.Clamp(_outlookFolder.SelectedIndex, 0, 4)];
            var messages=await _outlookClient.RecentAsync(folderId,maximum,token);
            int n=0;
            foreach (var msg in messages)
            {
                token.ThrowIfCancellationRequested();
                n++;
                _outlookStatus.Text=$"Correo {n}/{messages.Count}: {msg.Subject}";
                var dir=Path.Combine(AppPaths.PrivateTemp,"PalClam-Outlook-"+Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(dir);
                var file=Path.Combine(dir,"message.eml");
                try
                {
                    await _outlookClient.DownloadMimeAsync(msg.Id,file,
                        _settings.MailMaxMessageMb*1024L*1024L,token);
                    var opts=new MailScanOptions(false,_settings.MailExtractAttachments,_settings.MailIncludeInline,
                        _settings.MailPhishingHeuristics,_settings.MailCheckLinks,_settings.ScanArchives,_settings.DetectPua,
                        _settings.MailMaxMessageMb,_settings.MailMaxAttachmentMb,1);
                    var summary=await MailScanService.ScanAsync(file,scanner,SignatureStore.DatabaseDirectory,opts,
                        finding=>Dispatcher.BeginInvoke(new Action(()=>
                        {
                            if (finding.Status=="DETECCIÓN") MarkThreatForTray();
                            if (_outlookRows.Count<2500 && finding.Status!="Limpio")
                                _outlookRows.Add(new OutlookRow(msg.Subject,finding.Status,finding.Detail));
                        })),(_done,_total)=>{},token);
                    var status=summary.Detections>0 ? "AMENAZA" :
                        summary.Errors>0 || summary.Skipped>0 || summary.Truncated ? "Incompleto" :
                        summary.LinkWarnings>0 ? "Advertencia de enlaces" : "Sin detecciones";
                    _outlookRows.Add(new OutlookRow(msg.Subject,status,
                        $"Adjuntos: {summary.AttachmentsAnalyzed}; detecciones: {summary.Detections}; omitidos: {summary.Skipped}"));
                    if (summary.Detections>0) MarkThreatForTray();
                }
                catch (OperationCanceledException) {throw;}
                catch(Exception ex) { _outlookRows.Add(new OutlookRow(msg.Subject,"ERROR",ex.Message)); }
                finally
                {
                    try { Directory.Delete(dir,true); }
                    catch (Exception ex) { _outlookRows.Add(new OutlookRow(msg.Subject,"AVISO","Temporal no eliminado: "+ex.Message)); }
                }
            }
            _outlookStatus.Text=$"Procesados {n} de {messages.Count} mensajes de la carpeta seleccionada. " +
                "Revisa los resultados; no constituye protección preventiva.";
        }
        catch(OperationCanceledException) { _outlookStatus.Text="Cancelado: quedan mensajes sin examinar."; }
        catch(Exception ex) { _outlookStatus.Text="Error en Outlook: "+ex.Message; }
        finally
        {
            _outlookScanning=false;
            _outlookStopButton.IsEnabled=false;_outlookScanButton.IsEnabled=true;
            _outlookCancellation?.Dispose();_outlookCancellation=null;
            SetBusy(false); UpdateTrayState();
        }
    }
}

internal sealed record OutlookRow(string Subject, string Status, string Detail);
