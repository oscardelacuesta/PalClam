// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Identity.Client;

namespace PalClam.Services;

/// <summary>
/// An on-demand, read-only Microsoft Graph client. A public application must be registered
/// in Microsoft Entra (personal + organizational accounts, public client/device code flow).
/// The delegated scope Mail.Read is required for MIME, not Mail.ReadBasic.
/// Tokens are kept in memory only, never serialized to settings or disk.
/// </summary>
internal sealed class OutlookGraphClient : IDisposable
{
    private static readonly string[] Scopes = ["Mail.Read"];
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(60) };
    private IPublicClientApplication? _app;
    private IAccount? _account;
    public bool Connected => _account is not null;
    public string AccountName => _account?.Username ?? "No conectado";

    public async Task ConnectAsync(string clientId, Action<string, string> showCode, CancellationToken token)
    {
        if (!Guid.TryParse(clientId, out var id) || id == Guid.Empty)
            throw new ArgumentException("Introduce un Client ID válido de tu aplicación registrada en Microsoft Entra.");
        _app = PublicClientApplicationBuilder.Create(clientId)
            .WithAuthority("https://login.microsoftonline.com/common")
            .Build();
        var auth = await _app.AcquireTokenWithDeviceCode(Scopes, code =>
        {
            showCode(code.VerificationUrl, code.UserCode);
            return Task.CompletedTask;
        }).ExecuteAsync(token);
        _account = auth.Account ?? throw new InvalidOperationException("Microsoft no devolvió una cuenta.");
    }

    public async Task DisconnectAsync()
    {
        if (_app is not null && _account is not null)
            await _app.RemoveAsync(_account);
        _account = null;
        _app = null;
    }

    private async Task<string> TokenAsync(CancellationToken token)
    {
        if (_app is null || _account is null) throw new InvalidOperationException("Conecta primero Outlook.");
        try { return (await _app.AcquireTokenSilent(Scopes, _account).ExecuteAsync(token)).AccessToken; }
        catch (MsalUiRequiredException)
        {
            throw new InvalidOperationException("El acceso a Outlook expiró. Desconecta y vuelve a autorizar la cuenta.");
        }
    }

    private async Task<HttpResponseMessage> GetAsync(string relative, string accessToken, CancellationToken token)
    {
        // The endpoint is fixed, HTTPS only. Never accept links obtained from a Graph response.
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://graph.microsoft.com/v1.0/" + relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        if (!response.IsSuccessStatusCode)
        {
            var status = (int)response.StatusCode;
            response.Dispose();
            throw new InvalidOperationException($"Graph devolvió HTTP {status}. Revisa permisos Mail.Read o la conexión.");
        }
        return response;
    }

    public async Task<IReadOnlyList<OutlookMessage>> RecentAsync(string folderId, int max, CancellationToken token)
    {
        if (folderId is not ("inbox" or "sentitems" or "junkemail" or "archive" or "drafts")) throw new ArgumentException("Carpeta de Outlook no admitida.");
        if (max is < 1 or > 50) throw new ArgumentOutOfRangeException(nameof(max));
        var accessToken = await TokenAsync(token);
        var query = "me/mailFolders/" + folderId + "/messages?$select=id,subject,receivedDateTime&$orderby=receivedDateTime%20desc&$top=" + max;
        using var res = await GetAsync(query, accessToken, token);
        await using var stream = await res.Content.ReadAsStreamAsync(token);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: token);
        if (!json.RootElement.TryGetProperty("value", out var values) || values.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Microsoft Graph no devolvió una lista válida.");
        var items = new List<OutlookMessage>();
        foreach (var msg in values.EnumerateArray())
        {
            var id = msg.TryGetProperty("id", out var i) ? i.GetString() : null;
            if (string.IsNullOrEmpty(id)) continue;
            var subject = msg.TryGetProperty("subject", out var s) ? s.GetString() ?? "(sin asunto)" : "(sin asunto)";
            if (items.Count >= max) break;
            items.Add(new OutlookMessage(id, subject.Length > 180 ? subject[..180] : subject));
        }
        return items;
    }

    public async Task DownloadMimeAsync(string messageId, string destination, long maxBytes, CancellationToken token)
    {
        if (maxBytes is < 1 or > 104857600) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        var accessToken = await TokenAsync(token);
        using var res = await GetAsync("me/messages/" + Uri.EscapeDataString(messageId) + "/$value", accessToken, token);
        if (res.Content.Headers.ContentLength is long n && n > maxBytes)
            throw new InvalidDataException("El mensaje supera el límite configurado; no se analizó.");
        await using var source = await res.Content.ReadAsStreamAsync(token);
        await using var file = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        var data = new byte[81920]; long total = 0;
        while (true)
        {
            var count = await source.ReadAsync(data, token);
            if (count == 0) break;
            total += count;
            if (total > maxBytes) throw new InvalidDataException("Mensaje demasiado grande; no se analizó.");
            await file.WriteAsync(data.AsMemory(0, count), token);
        }
    }

    public void Dispose() => _http.Dispose();
}

internal sealed record OutlookMessage(string Id, string Subject);
