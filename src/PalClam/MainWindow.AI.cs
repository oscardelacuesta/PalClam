// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PalClam.Infrastructure;
using PalClam.Services;

namespace PalClam;

public sealed partial class MainWindow
{
    private ComboBox _aiProvider = null!;
    private ComboBox _aiModel = null!;
    private TextBox _aiEndpoint = null!;
    private PasswordBox _aiPassword = null!;
    private TextBlock _aiKeyStatus = null!;
    private TextBlock _aiStatus = null!;
    private TextBlock _aiTarget = null!;
    private TextBox _aiPrompt = null!;
    private TextBox _aiAnswer = null!;
    private CheckBox _aiConsent = null!;
    private Button _aiSend = null!;
    private Button _aiCancel = null!;
    private Button _aiListModels = null!;
    private CancellationTokenSource? _aiCancellation;

    private AiProvider CurrentAiProvider =>
        _aiProvider.SelectedItem as AiProvider ?? AiProviderCatalog.All[0];

    private void BuildAiPage(StackPanel page)
    {
        page.Children.Add(T("PalClam AI · Asistencia opcional", 25, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 8)));
        page.Children.Add(T("La IA explica resultados; no reemplaza la detección ClamAV, no elimina archivos ni modifica cuarentena.",
            12, Muted, FontWeights.Normal, new Thickness(0, 0, 0, 15)));

        var notice = Card(BluePale, Outline);
        notice.Margin = new Thickness(0, 0, 0, 13);
        var noticeStack = new StackPanel();
        noticeStack.Children.Add(T("PRIVACIDAD CONTROLADA POR TI", 11, Blue, FontWeights.Bold,
            new Thickness(0, 0, 0, 8)));
        noticeStack.Children.Add(T("Sin llamadas automáticas. No se transmiten ficheros, rutas, correos, adjuntos ni diagnósticos completos. " +
            "Solo se enviará el texto que veas en el cuadro de consulta al pulsar Enviar. " +
            "Ollama y LM Studio se limitan al equipo local; los demás proveedores pueden tener coste y procesarán el texto en sus servicios.",
            12, Slate));
        notice.Child = noticeStack;
        page.Children.Add(notice);
        BuildAiAnalystShortcuts(page);

        var configuration = Card(Surface, Outline);
        configuration.Margin = new Thickness(0, 0, 0, 14);
        var form = new StackPanel();
        form.Children.Add(T("Proveedor y modelo", 17, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 10)));
        form.Children.Add(T("PROVEEDOR", 10, Muted, FontWeights.Bold));
        _aiProvider = new ComboBox
        {
            MinHeight = 38, Margin = new Thickness(0, 6, 0, 11),
            ItemsSource = AiProviderCatalog.All, DisplayMemberPath = "DisplayName",
            Background = Surface
        };
        form.Children.Add(_aiProvider);
        form.Children.Add(T("MODELO · Selecciona uno descubierto o escribe su identificador exacto", 10, Muted, FontWeights.Bold));
        _aiModel = new ComboBox { IsEditable = true, MinHeight = 37,
            Margin = new Thickness(0, 6, 0, 6) };
        form.Children.Add(_aiModel);
        _aiListModels = ActionButton("↻  Consultar modelos disponibles", false,
            async (_, _) => await LoadAiModelsAsync());
        form.Children.Add(_aiListModels);
        form.Children.Add(T("ENDPOINT DE LA API", 10, Muted, FontWeights.Bold,
            new Thickness(0, 12, 0, 0)));
        _aiEndpoint = Input("Endpoint oficial o URL base personalizada");
        _aiEndpoint.Margin = new Thickness(0, 6, 0, 12);
        form.Children.Add(_aiEndpoint);
        form.Children.Add(T("CLAVE API · Se protege en Windows con DPAPI si decides guardarla", 10, Muted,
            FontWeights.Bold));
        _aiPassword = new PasswordBox { MinHeight = 36, Padding = new Thickness(11, 7, 11, 7),
            Background = Surface, BorderBrush = Outline, BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 6, 0, 6) };
        form.Children.Add(_aiPassword);
        var keysRow = new WrapPanel { Margin = new Thickness(0, 2, 0, 5) };
        keysRow.Children.Add(ActionButton("Guardar clave cifrada", false, (_, _) => SaveAiKey()));
        keysRow.Children.Add(ActionButton("Eliminar clave guardada", false, (_, _) => DeleteAiKey()));
        form.Children.Add(keysRow);
        _aiKeyStatus = T("", 11, Muted);
        form.Children.Add(_aiKeyStatus);
        configuration.Child = form;
        page.Children.Add(configuration);

        var consultation = Card(Surface, Outline);
        var consultationStack = new StackPanel();
        consultationStack.Children.Add(T("Consulta asistida", 17, Ink, FontWeights.Bold,
            new Thickness(0, 0, 0, 7)));
        consultationStack.Children.Add(T("TEXTO EXACTO QUE SE ENVIARÁ", 10, Muted,
            FontWeights.Bold, new Thickness(0, 0, 0, 5)));
        _aiPrompt = Input("Introduce el texto que deseas consultar. No incluyas datos privados.");
        _aiPrompt.TextWrapping = TextWrapping.Wrap;
        _aiPrompt.AcceptsReturn = true;
        _aiPrompt.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _aiPrompt.VerticalContentAlignment = VerticalAlignment.Top;
        _aiPrompt.MinHeight = 135; _aiPrompt.MaxHeight = 250;
        _aiPrompt.Text = PalClam.Localization.DisplayText.Translate(
            "Explica de forma didáctica qué significa un resultado de ClamAV " +
            "y qué comprobaciones prudentes conviene hacer sin desactivar Microsoft Defender.");
        consultationStack.Children.Add(_aiPrompt);
        var summaryRow = new WrapPanel { Margin = new Thickness(0, 10, 0, 3) };
        summaryRow.Children.Add(ActionButton("▤  Cargar resumen anónimo del último análisis", false,
            (_, _) => InsertAiScanSummary()));
        consultationStack.Children.Add(summaryRow);
        _aiTarget = T("", 11, Muted, FontWeights.Normal, new Thickness(0, 4, 0, 10));
        consultationStack.Children.Add(_aiTarget);
        _aiConsent = new CheckBox
        {
            Content = "Autorizo el envío de este texto al proveedor remoto seleccionado; puede implicar coste.",
            Margin = new Thickness(0, 9, 0, 10), Foreground = Slate,
            FontSize = 12, IsChecked = false
        };
        consultationStack.Children.Add(_aiConsent);
        var actions = new WrapPanel();
        _aiSend = ActionButton("✦  Consultar con IA", true, async (_, _) => await SubmitAiAsync());
        SetSemanticButton(_aiSend, "scan");
        actions.Children.Add(_aiSend);
        _aiCancel = ActionButton("■  Cancelar solicitud", false, (_, _) => _aiCancellation?.Cancel());
        SetSemanticButton(_aiCancel, "danger");
        _aiCancel.IsEnabled = false;
        actions.Children.Add(_aiCancel);
        consultationStack.Children.Add(actions);
        _aiStatus = T("Sin conexión. No se han enviado datos.", 12, Muted,
            FontWeights.Normal, new Thickness(0, 7, 0, 11));
        consultationStack.Children.Add(_aiStatus);
        consultationStack.Children.Add(T("RESPUESTA DEL PROVEEDOR · Solo informativa", 10, Muted,
            FontWeights.Bold, new Thickness(0, 0, 0, 5)));
        _aiAnswer = Input("La respuesta aparecerá aquí; no ejecuta ninguna acción.");
        _aiAnswer.IsReadOnly = true;
        _aiAnswer.TextWrapping = TextWrapping.Wrap;
        _aiAnswer.AcceptsReturn = true;
        _aiAnswer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _aiAnswer.VerticalContentAlignment = VerticalAlignment.Top;
        _aiAnswer.MinHeight = 195;
        consultationStack.Children.Add(_aiAnswer);
        consultation.Child = consultationStack;
        page.Children.Add(consultation);
        page.Children.Add(T("No se registra el contenido del mensaje ni la respuesta. " +
            "Las API externas requieren una clave y una cuenta independientes de la suscripción de ChatGPT. " +
            "El catálogo de modelos depende de cada servicio y de tu cuenta.",
            11, Muted, FontWeights.Normal, new Thickness(0, 10, 0, 0)));

        _aiProvider.SelectionChanged += (_, _) => UpdateAiProviderSelection();
        _aiPrompt.TextChanged += (_, _) => ResetAiConsent();
        _aiEndpoint.TextChanged += (_, _) =>
        {
            ResetAiConsent();
            _aiTarget.Text = CurrentAiProvider.IsLocal ?
                "Destino: servidor local " + _aiEndpoint.Text :
                "Destino remoto: " + _aiEndpoint.Text;
        };
        _aiModel.SelectionChanged += (_, _) => ResetAiConsent();
        _aiProvider.SelectedItem = AiProviderCatalog.Find(_settings.AiProviderId);
    }

    private void ResetAiConsent()
    {
        if (_aiConsent is not null) _aiConsent.IsChecked = false;
    }

    private void UpdateAiProviderSelection()
    {
        var provider = CurrentAiProvider;
        _aiEndpoint.IsReadOnly = !provider.IsLocal && provider.Id != "compatible";
        _aiEndpoint.Text = provider.Id == "compatible" &&
            _settings.AiProviderId == "compatible" ? _settings.AiCustomBaseUrl : provider.BaseUrl;
        _aiModel.Items.Clear();
        _aiModel.Text = _settings.AiProviderId == provider.Id ? _settings.AiModelId : "";
        _aiPassword.Clear();
        _aiAnswer.Clear();
        _aiKeyStatus.Text = provider.IsLocal ? "Servidor local: no necesita clave API." :
            AiSecretsStore.HasSavedKey(provider.Id) ? "✓  Existe una clave cifrada para este usuario de Windows." :
            "Sin clave guardada. Puedes utilizarla solo durante esta sesión.";
        _aiTarget.Text = provider.IsLocal ? "Destino: servidor de tu propio equipo (loopback)." :
            "Destino remoto: " + (provider.BaseUrl.Length == 0 ? "URL que especifiques" : provider.BaseUrl);
        _aiConsent.Visibility = provider.IsLocal ? Visibility.Collapsed : Visibility.Visible;
        _aiStatus.Text = "Esperando una acción del usuario. No se ha conectado al proveedor.";
        _settings.AiProviderId = provider.Id;
        SettingsStore.Save(_settings);
        ResetAiConsent();
    }

    private void SaveAiKey()
    {
        var provider = CurrentAiProvider;
        if (provider.IsLocal) { _aiKeyStatus.Text = "Este proveedor local no necesita clave."; return; }
        try
        {
            AiSecretsStore.Save(provider.Id, _aiPassword.Password);
            _aiPassword.Clear();
            _aiKeyStatus.Text = "✓  Clave guardada mediante DPAPI para el usuario actual.";
        }
        catch (Exception) { _aiKeyStatus.Text = "No se pudo guardar la clave. Revisa permisos y su contenido."; }
    }

    private void DeleteAiKey()
    {
        try
        {
            AiSecretsStore.Delete(CurrentAiProvider.Id);
            _aiPassword.Clear();
            _aiKeyStatus.Text = "Clave eliminada del almacén de PalClam.";
        }
        catch (Exception) { _aiKeyStatus.Text = "No se pudo eliminar la clave cifrada."; }
    }

    private string ActiveAiKey()
    {
        var typed = _aiPassword.Password.Trim();
        return typed.Length > 0 ? typed : AiSecretsStore.Load(CurrentAiProvider.Id) ?? "";
    }

    private void InsertAiScanSummary()
    {
        OpenAiAnalystWithPrompt(AiSecurityAnalyst.ForScan(ScanHistory.ReadRecent(1).FirstOrDefault()));
    }

    private void PersistAiSelection()
    {
        _settings.AiProviderId = CurrentAiProvider.Id;
        _settings.AiModelId = _aiModel.Text.Trim();
        if (CurrentAiProvider.Id == "compatible") _settings.AiCustomBaseUrl = _aiEndpoint.Text.Trim();
        SettingsStore.Save(_settings);
    }

    private void SetAiBusy(bool busy)
    {
        _aiProvider.IsEnabled = !busy;
        _aiModel.IsEnabled = !busy;
        _aiEndpoint.IsEnabled = !busy;
        _aiPassword.IsEnabled = !busy;
        _aiPrompt.IsEnabled = !busy;
        _aiConsent.IsEnabled = !busy;
        _aiSend.IsEnabled = !busy;
        _aiListModels.IsEnabled = !busy;
        _aiCancel.IsEnabled = busy;
    }

    private async Task LoadAiModelsAsync()
    {
        if (_aiCancellation is not null) return;
        var provider = CurrentAiProvider;
        try
        {
            PersistAiSelection();
            SetAiBusy(true);
            _aiStatus.Text = "Consultando catálogo de " + provider.DisplayName + "…";
            _aiCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var models = await AiGateway.GetModelsAsync(provider, _aiEndpoint.Text, ActiveAiKey(),
                _aiCancellation.Token);
            var previous = _aiModel.Text;
            _aiModel.Items.Clear();
            foreach (var m in models) _aiModel.Items.Add(m);
            _aiModel.Text = previous;
            _aiStatus.Text = $"{models.Count} modelos listados. Selecciona uno o escribe el ID manualmente.";
        }
        catch (OperationCanceledException) { _aiStatus.Text = "Consulta del catálogo cancelada o agotada."; }
        catch (Exception ex) { _aiStatus.Text = "No se pudo consultar el catálogo: " + ex.Message; }
        finally
        {
            _aiCancellation?.Dispose(); _aiCancellation = null;
            SetAiBusy(false);
        }
    }

    private async Task SubmitAiAsync()
    {
        if (_aiCancellation is not null) return;
        var provider = CurrentAiProvider;
        if (!provider.IsLocal && _aiConsent.IsChecked != true)
        {
            _aiStatus.Text = "Debes autorizar expresamente este envío al proveedor remoto.";
            return;
        }
        if (!provider.IsLocal && AiSecurityAnalyst.MayContainPrivateData(_aiPrompt.Text) &&
            MessageBox.Show(this,
                "El texto contiene posibles rutas, direcciones o secretos. Es preferible retirarlos. " +
                "¿Enviar de todas formas exactamente el texto visible al servicio remoto?",
                "Revisión de privacidad antes de enviar", MessageBoxButton.YesNo,
                MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
        {
            _aiStatus.Text = "Envío cancelado. Retira los datos privados y revisa el contenido.";
            return;
        }
        var query = new AiRequest(provider, _aiEndpoint.Text.Trim(), _aiModel.Text.Trim(),
            _aiPrompt.Text.Trim(), ActiveAiKey());
        try
        {
            AiGateway.ValidateRequest(query);
            PersistAiSelection();
            SetAiBusy(true);
            _aiAnswer.Text = "";
            _aiStatus.Text = provider.IsLocal ? "Consultando el motor local…" :
                "Enviando SOLO el texto mostrado a " + provider.DisplayName + "…";
            _aiCancellation = new CancellationTokenSource();
            var result = await AiGateway.QueryAsync(query, _aiCancellation.Token);
            _aiAnswer.Text = result.Text;
            _aiStatus.Text = $"✓  Respuesta recibida de {provider.DisplayName} ({result.Milliseconds:N0} ms). " +
                "Orientativa: no modifica resultados de ClamAV.";
        }
        catch (OperationCanceledException) { _aiStatus.Text = "Solicitud cancelada. No se garantiza que el proveedor remoto la haya descartado."; }
        catch (Exception ex) { _aiStatus.Text = "No se pudo completar la consulta: " + ex.Message; }
        finally
        {
            _aiCancellation?.Dispose(); _aiCancellation = null;
            SetAiBusy(false);
            ResetAiConsent();
        }
    }
}
