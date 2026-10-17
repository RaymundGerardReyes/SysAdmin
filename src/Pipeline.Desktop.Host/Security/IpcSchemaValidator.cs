using System.Text.Json;

namespace Pipeline.Desktop.Host.Security;

public sealed record IpcEnvelope(string Id, string Command, JsonElement Payload, string? CorrelationId);

public static class IpcSchemaValidator
{
    private static readonly HashSet<string> AllowedCommands = new(StringComparer.Ordinal)
    {
        "sheets.read",
        "sheets.write",
        "gmail.send",
        "payments.checkout.create",
        "payments.checkout.approve",
        "admin.users.manage",
        "jobs.read",
        "jobs.cancel"
    };

    public static bool ValidateEnvelope(string json, out IpcEnvelope? envelope, out string? errorCode)
    {
        envelope = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            errorCode = "EMPTY_PAYLOAD";
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("id", out var idProp) || string.IsNullOrWhiteSpace(idProp.GetString()))
            {
                errorCode = "MISSING_ID";
                return false;
            }

            if (!root.TryGetProperty("command", out var cmdProp) || string.IsNullOrWhiteSpace(cmdProp.GetString()))
            {
                errorCode = "MISSING_COMMAND";
                return false;
            }

            var command = cmdProp.GetString()!;
            if (!AllowedCommands.Contains(command))
            {
                errorCode = "UNKNOWN_COMMAND";
                return false;
            }

            var id = idProp.GetString()!;
            var payload = root.TryGetProperty("payload", out var p) ? p.Clone() : default;
            var correlationId = root.TryGetProperty("correlationId", out var c) ? c.GetString() : null;

            envelope = new IpcEnvelope(id, command, payload, correlationId);
            errorCode = null;
            return true;
        }
        catch (JsonException)
        {
            errorCode = "MALFORMED_JSON";
            return false;
        }
    }
}
