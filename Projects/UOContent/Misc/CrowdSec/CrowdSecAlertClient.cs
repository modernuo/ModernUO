/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CrowdSecAlertClient.cs                                          *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Network.Bans.CrowdSec;

/// <summary>Reporter-side LAPI operations, mockable for tests.</summary>
public interface ICrowdSecAlertClient : IDisposable
{
    ValueTask PostAlertsAsync(IReadOnlyList<CrowdSecAlert> alerts, CancellationToken token);
    ValueTask DeleteDecisionsAsync(string origin, IPAddress ip, CancellationToken token);
}

/// <summary>
/// CrowdSec LAPI watcher client: authenticates with machine credentials and posts/deletes decisions.
/// Holds a JWT refreshed on expiry or a 401.
/// </summary>
public sealed class CrowdSecAlertClient : ICrowdSecAlertClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly string _machineId;
    private readonly string _password;

    private string _token;
    private DateTime _tokenExpiresUtc = DateTime.MinValue;

    public CrowdSecAlertClient(CrowdSecSettings settings)
    {
        var baseUri = new Uri(settings.LapiUrl, UriKind.Absolute); // fails loud on malformed url
        _http = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.Add("User-Agent", "ModernUO-watcher/1.0");
        _machineId = settings.MachineId;
        _password = settings.Password;
    }

    private async Task EnsureAuthAsync(CancellationToken token)
    {
        if (_token != null && DateTime.UtcNow < _tokenExpiresUtc - TimeSpan.FromMinutes(1))
        {
            return;
        }

        var request = new CrowdSecLoginRequest { MachineId = _machineId, Password = _password };
        using var response = await _http.PostAsJsonAsync("/v1/watchers/login", request, _jsonOptions, token);
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<CrowdSecLoginResponse>(_jsonOptions, token);
        _token = login?.Token ?? throw new InvalidOperationException("CrowdSec login returned no token.");
        _tokenExpiresUtc = DateTime.TryParse(login.Expire, out var exp) ? exp.ToUniversalTime() : DateTime.UtcNow.AddHours(1);
    }

    private void Authorize(HttpRequestMessage message) =>
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

    public async ValueTask PostAlertsAsync(IReadOnlyList<CrowdSecAlert> alerts, CancellationToken token)
    {
        if (alerts.Count == 0)
        {
            return;
        }

        await SendWithRetryAsync(() =>
        {
            var message = new HttpRequestMessage(HttpMethod.Post, "/v1/alerts")
            {
                Content = JsonContent.Create(alerts, options: _jsonOptions)
            };
            Authorize(message);
            return message;
        }, token);
    }

    public async ValueTask DeleteDecisionsAsync(string origin, IPAddress ip, CancellationToken token)
    {
        var query = BuildDeleteQuery(origin, ip);
        await SendWithRetryAsync(() =>
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, query);
            Authorize(message);
            return message;
        }, token);
    }

    /// <summary>
    /// Builds the decisions-delete query string. <paramref name="origin"/> is operator-controlled config
    /// (crowdsec.json), so it must be escaped like any other untrusted value going into a URL.
    /// </summary>
    internal static string BuildDeleteQuery(string origin, IPAddress ip) =>
        $"/v1/decisions?origin={Uri.EscapeDataString(origin)}&ip={Uri.EscapeDataString(ip.ToString())}";

    private async ValueTask SendWithRetryAsync(Func<HttpRequestMessage> build, CancellationToken token)
    {
        await EnsureAuthAsync(token);

        using var first = build();
        using var response = await _http.SendAsync(first, token);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            response.EnsureSuccessStatusCode();
            return;
        }

        // Token rejected mid-flight: force a re-login and retry once.
        _token = null;
        await EnsureAuthAsync(token);
        using var retry = build();
        using var retryResponse = await _http.SendAsync(retry, token);
        retryResponse.EnsureSuccessStatusCode();
    }

    public void Dispose() => _http.Dispose();
}
