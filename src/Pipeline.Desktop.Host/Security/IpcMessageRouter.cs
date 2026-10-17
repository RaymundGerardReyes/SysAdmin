using System.Text.Json;
using Pipeline.Application.Pipeline;
using Pipeline.Application.UseCases.Sheets;

namespace Pipeline.Desktop.Host.Security;

public sealed record IpcResponse(string Id, bool Success, object? Data, string? ErrorCode, string? ErrorMessage);

public sealed class IpcMessageRouter
{
    private readonly IUseCaseDispatcher _dispatcher;

    public IpcMessageRouter(IUseCaseDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task<IpcResponse> RouteMessageAsync(string rawJson, CancellationToken ct = default)
    {
        if (!IpcSchemaValidator.ValidateEnvelope(rawJson, out var envelope, out var validationError))
        {
            return new IpcResponse("unknown", false, null, validationError, "Envelope validation failed");
        }

        var env = envelope!;
        try
        {
            switch (env.Command)
            {
                case "sheets.read":
                    var sheetId = env.Payload.GetProperty("spreadsheetId").GetString() ?? "";
                    var range = env.Payload.GetProperty("range").GetString() ?? "";
                    var cmd = new ReadSheetRangeCommand(sheetId, range);
                    var res = await _dispatcher.DispatchAsync<ReadSheetRangeCommand, Pipeline.Contracts.Responses.SheetRangeResponse>(cmd, ct).ConfigureAwait(false);
                    return new IpcResponse(env.Id, res.IsSuccess, res.Value, res.ErrorCode, res.ErrorMessage);

                default:
                    return new IpcResponse(env.Id, false, null, "UNKNOWN_COMMAND", $"Command '{env.Command}' has no route mapping");
            }
        }
        catch (Exception ex)
        {
            return new IpcResponse(env.Id, false, null, "INTERNAL_ROUTER_ERROR", ex.Message);
        }
    }
}
