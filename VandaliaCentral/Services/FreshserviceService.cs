using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

namespace VandaliaCentral.Services;

public sealed class FreshserviceService : IFreshserviceService
{
    private readonly HttpClient _http;
    private readonly FreshserviceOptions _opts;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public FreshserviceService(HttpClient http, IOptions<FreshserviceOptions> opts)
    {
        _http = http;
        _opts = opts.Value;
    }

    public async Task<FreshserviceCreateTicketResult> CreateTicketAsync(
        FreshserviceCreateTicketInput input,
        CancellationToken ct = default)
    {
        var domain = (_opts.Domain ?? "").Trim();
        var apiKey = (_opts.ApiKey ?? "").Trim();

        if (string.IsNullOrWhiteSpace(domain))
            throw new FreshserviceApiException("Freshservice Domain is not configured.");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new FreshserviceApiException("Freshservice API key is not configured.");

        if (_opts.DepartmentId <= 0)
            throw new FreshserviceApiException("Freshservice DepartmentId is not configured.");

        if (_opts.ResponderId <= 0)
            throw new FreshserviceApiException("Freshservice ResponderId is not configured.");

        // input.PriorityLabel is your stable key: LOW/HIGH/CRITICAL (from Razor dropdown)
        var priorityKey = (input.PriorityLabel ?? "").Trim().ToUpperInvariant();

        // Freshservice custom dropdown (exact labels Jeff set)
        var customPriorityLabel = ResolveCustomPriorityLabel(priorityKey);

        // Freshservice system priority numeric 1..4
        var systemPriority = ResolveSystemPriority(priorityKey);

        var payload = new FreshserviceCreateTicketRequest
        {
            Subject = input.Subject,
            Description = input.Description,
            Email = input.RequesterEmail,

            DepartmentId = _opts.DepartmentId,
            ResponderId = _opts.ResponderId,

            Category = input.Category,
            SubCategory = input.SubCategory,

            Priority = systemPriority,
            Status = 2,

            CustomFields = new Dictionary<string, object?>
            {
                // Your tenant’s required custom dropdown field is ALSO named "priority"
                ["priority"] = customPriorityLabel
            }
        };

        var (resp, body) = await SendAsync(domain, apiKey, payload, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var requestId = GetRequestId(resp);

            // Parse validation errors: { description, errors: [ { field, message, code } ] }
            FreshserviceErrorResponse? parsed = null;
            try { parsed = JsonSerializer.Deserialize<FreshserviceErrorResponse>(body, JsonOpts); } catch { /* ignore */ }

            if (parsed?.Errors?.Count > 0)
            {
                // Enrich priority error with what we sent
                var enriched = parsed.Errors
                    .Select(e =>
                    {
                        if (string.Equals(e.Field, "priority", StringComparison.OrdinalIgnoreCase) &&
                            (e.Message?.Contains("one of these values", StringComparison.OrdinalIgnoreCase) ?? false))
                        {
                            return new FreshserviceFieldError
                            {
                                Field = e.Field,
                                Message = $"{e.Message} | Sent={MakeWhitespaceVisible(customPriorityLabel)}"
                            };
                        }

                        return e;
                    })
                    .ToList();

                var msg = parsed.Description ?? "Validation failed";
                if (!string.IsNullOrWhiteSpace(requestId))
                    msg += $" | x-request-id: {requestId}";

                throw new FreshserviceApiException(msg, enriched);
            }

            // Auth hint on 401/403
            if (resp.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
            {
                var authHint = await TryAuthSanityCheckAsync(domain, BuildBasicAuthB64(apiKey), ct);
                throw BuildFreshserviceException(resp, body, authHint);
            }

            throw BuildFreshserviceException(resp, body, null);
        }

        var created = JsonSerializer.Deserialize<FreshserviceCreateTicketResponse>(body, JsonOpts);
        var id = created?.Ticket?.Id;

        if (id is null or 0)
            throw new FreshserviceApiException("Ticket created, but no ticket id was returned.");

        return new FreshserviceCreateTicketResult { TicketId = id.Value };
    }

    private static int ResolveSystemPriority(string priorityKey)
    {
        // If you want ALL system priority to stay Low (1), tell me and I’ll set them all to 1.
        return priorityKey switch
        {
            "LOW" => 1,
            "HIGH" => 2,
            "CRITICAL" => 4,
            _ => 1
        };
    }

    private static string ResolveCustomPriorityLabel(string priorityKey)
    {
        // These must match exactly what Jeff configured
        return priorityKey switch
        {
            "LOW" => "Low-No due date",
            "HIGH" => "High-I can operate but need help",
            "CRITICAL" => "Critical-I am Down",
            _ => throw new FreshserviceApiException($"Invalid priority selected: '{priorityKey}'")
        };
    }

    private async Task<(HttpResponseMessage Resp, string Body)> SendAsync(
        string domain,
        string apiKey,
        FreshserviceCreateTicketRequest payload,
        CancellationToken ct)
    {
        var url = $"https://{domain}/api/v2/tickets";
        var json = JsonSerializer.Serialize(payload, JsonOpts);

        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        req.Headers.Accept.Clear();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", BuildBasicAuthB64(apiKey));

        var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        return (resp, body);
    }

    private static string BuildBasicAuthB64(string apiKey)
    {
        var raw = $"{apiKey.Trim()}:X";
        return Convert.ToBase64String(Encoding.ASCII.GetBytes(raw));
    }

    private async Task<string?> TryAuthSanityCheckAsync(string domain, string basicAuthB64, CancellationToken ct)
    {
        try
        {
            var meReq = new HttpRequestMessage(HttpMethod.Get, $"https://{domain}/api/v2/agents/me");
            meReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            meReq.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuthB64);

            var meResp = await _http.SendAsync(meReq, ct);
            var meBody = await meResp.Content.ReadAsStringAsync(ct);

            if (meResp.IsSuccessStatusCode)
                return "Auth sanity check OK (GET /agents/me succeeded).";

            return $"Auth sanity check failed (GET /agents/me => {(int)meResp.StatusCode}). Body: {TrimForEmail(meBody)}";
        }
        catch
        {
            return null;
        }
    }

    private FreshserviceApiException BuildFreshserviceException(HttpResponseMessage resp, string body, string? authHint)
    {
        var requestId = GetRequestId(resp);

        // 1) Validation style errors
        try
        {
            var err = JsonSerializer.Deserialize<FreshserviceErrorResponse>(body, JsonOpts);
            if (err?.Errors?.Count > 0)
            {
                var msg = err.Description ?? "Validation failed";
                msg += $" | HTTP {(int)resp.StatusCode}";
                if (!string.IsNullOrWhiteSpace(requestId))
                    msg += $" | x-request-id: {requestId}";
                if (!string.IsNullOrWhiteSpace(authHint))
                    msg += $" | {authHint}";

                return new FreshserviceApiException(msg, err.Errors);
            }
        }
        catch { /* ignore */ }

        // 2) Access denied style errors
        try
        {
            var denied = JsonSerializer.Deserialize<FreshserviceAccessDeniedResponse>(body, JsonOpts);
            if (!string.IsNullOrWhiteSpace(denied?.Code) || !string.IsNullOrWhiteSpace(denied?.Message))
            {
                var msg = $"Freshservice error (HTTP {(int)resp.StatusCode}): {denied?.Code}: {denied?.Message}";
                if (!string.IsNullOrWhiteSpace(requestId))
                    msg += $" | x-request-id: {requestId}";
                if (!string.IsNullOrWhiteSpace(authHint))
                    msg += $" | {authHint}";
                return new FreshserviceApiException(msg);
            }
        }
        catch { /* ignore */ }

        // 3) Fallback: raw body
        var fallback = $"Freshservice error (HTTP {(int)resp.StatusCode})";
        if (!string.IsNullOrWhiteSpace(requestId))
            fallback += $" | x-request-id: {requestId}";
        if (!string.IsNullOrWhiteSpace(authHint))
            fallback += $" | {authHint}";
        fallback += $" | Body: {TrimForEmail(body)}";

        return new FreshserviceApiException(fallback);
    }

    private static string? GetRequestId(HttpResponseMessage resp)
    {
        if (resp.Headers.TryGetValues("x-request-id", out var vals))
            return vals.FirstOrDefault();
        return null;
    }

    private static string TrimForEmail(string? s, int max = 3000)
    {
        if (string.IsNullOrWhiteSpace(s)) return "<empty>";
        s = s.Trim();
        if (s.Length <= max) return s;
        return s.Substring(0, max) + "…<truncated>";
    }

    private static string MakeWhitespaceVisible(string s)
    {
        // regular space -> ␠, NBSP -> ⍽
        return "\"" + s.Replace("\u00A0", "⍽").Replace(" ", "␠") + "\"";
    }

    // ---- API DTOs ----
    private sealed class FreshserviceCreateTicketRequest
    {
        public string? Subject { get; set; }
        public string? Description { get; set; }
        public string? Email { get; set; }

        [JsonPropertyName("department_id")]
        public long DepartmentId { get; set; }

        [JsonPropertyName("responder_id")]
        public long ResponderId { get; set; }

        public string? Category { get; set; }

        [JsonPropertyName("sub_category")]
        public string? SubCategory { get; set; }

        public int Priority { get; set; }
        public int Status { get; set; }

        [JsonPropertyName("custom_fields")]
        public Dictionary<string, object?>? CustomFields { get; set; }
    }

    private sealed class FreshserviceCreateTicketResponse
    {
        public TicketDto? Ticket { get; set; }

        public sealed class TicketDto
        {
            public long? Id { get; set; }
        }
    }

    private sealed class FreshserviceErrorResponse
    {
        public string? Description { get; set; }
        public List<FreshserviceFieldError>? Errors { get; set; }
    }

    private sealed class FreshserviceAccessDeniedResponse
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
    }
}

public sealed class FreshserviceApiException : Exception
{
    public List<FreshserviceFieldError>? FieldErrors { get; }

    public FreshserviceApiException(string message, List<FreshserviceFieldError>? fieldErrors = null) : base(message)
    {
        FieldErrors = fieldErrors;
    }
}
