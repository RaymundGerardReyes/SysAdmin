using System.Net;
using System.Text.Json;

namespace Integrations.Zoho.Bigin;

/// <summary>
/// Exception thrown when a Zoho Bigin API request fails or returns an error payload.
/// </summary>
public class BiginApiException : HttpRequestException
{
    public new HttpStatusCode StatusCode { get; init; }
    public string? BiginErrorCode { get; init; }
    public string BiginMessage { get; init; } = string.Empty;
    public string? BiginStatus { get; init; }
    public string? RequestUrl { get; init; }
    public JsonElement? Details { get; init; }
    public string? RawResponseBody { get; init; }

    /// <summary>
    /// Helper to extract the missing parameter name from the 'details' object (e.g. MANDATORY_NOT_FOUND, INVALID_DATA).
    /// </summary>
    public string? MissingApiName
    {
        get
        {
            if (Details.HasValue && Details.Value.ValueKind == JsonValueKind.Object &&
                Details.Value.TryGetProperty("api_name", out var prop) &&
                prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }
            return null;
        }
    }

    public BiginApiException(
        HttpStatusCode statusCode,
        string message)
        : base(message, null, statusCode)
    {
        StatusCode = statusCode;
        BiginMessage = message;
    }

    public BiginApiException(
        HttpStatusCode statusCode,
        string? biginErrorCode,
        string? biginMessage,
        string? requestUrl = null,
        Exception? innerException = null)
        : base(FormatMessage(statusCode, biginErrorCode, biginMessage), innerException, statusCode)
    {
        StatusCode = statusCode;
        BiginErrorCode = biginErrorCode;
        BiginMessage = biginMessage ?? string.Empty;
        RequestUrl = requestUrl;
    }

    public static BiginApiException FromHttpResponse(HttpStatusCode statusCode, string rawBody, string? requestUrl = null)
    {
        string message = $"Bigin API error with HTTP {(int)statusCode} {statusCode}";
        string? code = null;
        string? status = null;
        JsonElement? details = null;

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            // Pattern 1: { "code": "...", "message": "...", "status": "...", "details": {...} }
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("code", out var codeElem))
                {
                    code = codeElem.GetString();
                }

                if (root.TryGetProperty("status", out var statusElem))
                {
                    status = statusElem.GetString();
                }

                if (root.TryGetProperty("message", out var msgElem))
                {
                    message = msgElem.GetString() ?? message;
                }

                if (root.TryGetProperty("details", out var detailsElem))
                {
                    details = detailsElem.Clone();
                }

                // Pattern 2: { "watch": [ { "code": "INVALID_DATA", ... } ] }
                if (code is null && root.TryGetProperty("watch", out var watchElem) && watchElem.ValueKind == JsonValueKind.Array && watchElem.GetArrayLength() > 0)
                {
                    JsonElement errorElem = default;
                    var hasError = false;

                    foreach (var item in watchElem.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        var isError = false;
                        if (item.TryGetProperty("status", out var itemStatus) &&
                            string.Equals(itemStatus.GetString(), "error", StringComparison.OrdinalIgnoreCase))
                        {
                            isError = true;
                        }
                        else if (item.TryGetProperty("code", out var itemCode) &&
                            !string.Equals(itemCode.GetString(), "SUCCESS", StringComparison.OrdinalIgnoreCase))
                        {
                            isError = true;
                        }

                        if (isError)
                        {
                            errorElem = item;
                            hasError = true;
                            break;
                        }
                    }

                    if (!hasError)
                    {
                        errorElem = watchElem[0];
                    }

                    if (errorElem.ValueKind == JsonValueKind.Object)
                    {
                        if (errorElem.TryGetProperty("code", out var firstCode))
                        {
                            code = firstCode.GetString();
                        }

                        if (errorElem.TryGetProperty("message", out var firstMsg))
                        {
                            message = firstMsg.GetString() ?? message;
                        }

                        if (errorElem.TryGetProperty("status", out var firstStatus))
                        {
                            status = firstStatus.GetString();
                        }

                        if (errorElem.TryGetProperty("details", out var firstDetails))
                        {
                            details = firstDetails.Clone();
                        }
                    }
                }
            }
            else if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                JsonElement errorElem = default;
                var hasError = false;

                foreach (var item in root.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var isError = false;
                    if (item.TryGetProperty("status", out var itemStatus) &&
                        string.Equals(itemStatus.GetString(), "error", StringComparison.OrdinalIgnoreCase))
                    {
                        isError = true;
                    }
                    else if (item.TryGetProperty("code", out var itemCode) &&
                        !string.Equals(itemCode.GetString(), "SUCCESS", StringComparison.OrdinalIgnoreCase))
                    {
                        isError = true;
                    }

                    if (isError)
                    {
                        errorElem = item;
                        hasError = true;
                        break;
                    }
                }

                if (!hasError)
                {
                    errorElem = root[0];
                }

                if (errorElem.ValueKind == JsonValueKind.Object)
                {
                    if (errorElem.TryGetProperty("code", out var firstCode))
                    {
                        code = firstCode.GetString();
                    }

                    if (errorElem.TryGetProperty("message", out var firstMsg))
                    {
                        message = firstMsg.GetString() ?? message;
                    }

                    if (errorElem.TryGetProperty("status", out var firstStatus))
                    {
                        status = firstStatus.GetString();
                    }

                    if (errorElem.TryGetProperty("details", out var firstDetails))
                    {
                        details = firstDetails.Clone();
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Raw response not JSON, keep default message
        }

        return new BiginApiException(statusCode, code, message, requestUrl)
        {
            BiginStatus = status,
            Details = details,
            RawResponseBody = rawBody
        };
    }

    private static string FormatMessage(HttpStatusCode statusCode, string? code, string? message)
    {
        if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(message))
        {
            return $"Bigin API returned HTTP {(int)statusCode} ({statusCode}).";
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return $"Bigin API returned HTTP {(int)statusCode} ({statusCode}): {message}";
        }

        return $"Bigin API error [{(int)statusCode} {code}]: {message}";
    }
}
