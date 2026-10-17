using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Integrations.Zoho.Bigin.Webhook;

/// <summary>
/// Result of webhook validation and parsing.
/// </summary>
public sealed record BiginWebhookValidationResult
{
    public bool IsSuccess { get; init; }
    public BiginWebhookPayload? Payload { get; init; }
    public string? ErrorCode { get; init; }
    public string? FailureReason { get; init; }

    public static BiginWebhookValidationResult Success(BiginWebhookPayload payload) =>
        new() { IsSuccess = true, Payload = payload };

    public static BiginWebhookValidationResult Failure(string errorCode, string reason) =>
        new() { IsSuccess = false, ErrorCode = errorCode, FailureReason = reason };
}

/// <summary>
/// Parser and validator for incoming Zoho Bigin webhook notification payloads.
/// </summary>
public static class BiginWebhookValidator
{
    public const string ErrorEmptyPayload = "EMPTY_PAYLOAD";
    public const string ErrorInvalidJson = "INVALID_JSON";
    public const string ErrorInvalidToken = "INVALID_VERIFICATION_TOKEN";
    public const string ErrorChannelMismatch = "CHANNEL_ID_MISMATCH";
    public const string ErrorMissingMandatory = "MANDATORY_FIELD_MISSING";
    public const string ErrorInvalidModule = "INVALID_MODULE";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Validates and parses the incoming webhook JSON body.
    /// Uses constant-time token comparison to prevent timing attacks.
    /// </summary>
    public static BiginWebhookValidationResult ValidateAndParse(
        string? jsonPayload,
        string? expectedToken = null,
        string? expectedChannelId = null,
        bool requireKnownModule = true)
    {
        if (string.IsNullOrWhiteSpace(jsonPayload))
        {
            return BiginWebhookValidationResult.Failure(ErrorEmptyPayload, "Webhook payload is empty.");
        }

        BiginWebhookPayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<BiginWebhookPayload>(jsonPayload, JsonOptions)
                ?? throw new JsonException("Deserialized payload resulted in null.");
        }
        catch (JsonException ex)
        {
            return BiginWebhookValidationResult.Failure(ErrorInvalidJson, $"Malformed JSON payload: {ex.Message}");
        }

        // 1. Mandatory field checks
        if (string.IsNullOrWhiteSpace(payload.ChannelId))
        {
            return BiginWebhookValidationResult.Failure(ErrorMissingMandatory, "Missing mandatory field 'channel_id'.");
        }

        if (string.IsNullOrWhiteSpace(payload.Module))
        {
            return BiginWebhookValidationResult.Failure(ErrorMissingMandatory, "Missing mandatory field 'module'.");
        }

        if (string.IsNullOrWhiteSpace(payload.Operation))
        {
            return BiginWebhookValidationResult.Failure(ErrorMissingMandatory, "Missing mandatory field 'operation'.");
        }

        if (payload.Ids is null || payload.Ids.Count == 0 || payload.Ids.Any(string.IsNullOrWhiteSpace))
        {
            return BiginWebhookValidationResult.Failure(ErrorMissingMandatory, "Missing or empty mandatory field 'ids'.");
        }

        // 2. Channel ID matching
        if (!string.IsNullOrWhiteSpace(expectedChannelId))
        {
            if (!string.Equals(payload.ChannelId, expectedChannelId, StringComparison.Ordinal))
            {
                return BiginWebhookValidationResult.Failure(
                    ErrorChannelMismatch,
                    $"Channel ID '{payload.ChannelId}' does not match expected channel ID '{expectedChannelId}'.");
            }
        }

        // 3. Verification token validation (Constant-time comparison)
        if (!string.IsNullOrWhiteSpace(expectedToken))
        {
            if (string.IsNullOrEmpty(payload.Token))
            {
                return BiginWebhookValidationResult.Failure(
                    ErrorInvalidToken,
                    "Webhook payload is missing the verification token.");
            }

            if (!TimingSafeEquals(payload.Token, expectedToken))
            {
                return BiginWebhookValidationResult.Failure(
                    ErrorInvalidToken,
                    "Verification token in payload does not match the configured channel token.");
            }
        }

        // 4. Module validation
        if (requireKnownModule && !BiginModules.IsKnownModule(payload.Module))
        {
            return BiginWebhookValidationResult.Failure(
                ErrorInvalidModule,
                $"Module '{payload.Module}' is not a recognized Zoho Bigin module.");
        }

        return BiginWebhookValidationResult.Success(payload);
    }

    /// <summary>
    /// Performs a timing-attack safe comparison of two strings using SHA-256 hashes and FixedTimeEquals.
    /// Hashes the UTF-8 representations first so comparisons are always 32 fixed bytes, eliminating length-leak timing channels.
    /// </summary>
    public static bool TimingSafeEquals(string? a, string? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        var hashA = SHA256.HashData(Encoding.UTF8.GetBytes(a));
        var hashB = SHA256.HashData(Encoding.UTF8.GetBytes(b));

        return CryptographicOperations.FixedTimeEquals(hashA, hashB);
    }
}
