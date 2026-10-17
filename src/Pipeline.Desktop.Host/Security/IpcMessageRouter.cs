using System.Text.Json;
using Pipeline.Application.Pipeline;
using Pipeline.Application.UseCases.Bigin;
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

                case "bigin.modules.read":
                    var dc = env.Payload.ValueKind == JsonValueKind.Object && env.Payload.TryGetProperty("dataCenter", out var dcProp) ? dcProp.GetString() : null;
                    var bCmd = new GetBiginModulesCommand(dc);
                    var bRes = await _dispatcher.DispatchAsync<GetBiginModulesCommand, Pipeline.Contracts.Responses.BiginModulesResponseContract>(bCmd, ct).ConfigureAwait(false);
                    return new IpcResponse(env.Id, bRes.IsSuccess, bRes.Value, bRes.ErrorCode, bRes.ErrorMessage);

                case "bigin.org.read":
                    var orgDc = env.Payload.ValueKind == JsonValueKind.Object && env.Payload.TryGetProperty("dataCenter", out var orgDcProp) ? orgDcProp.GetString() : null;
                    var orgCmd = new GetBiginOrgCommand(orgDc);
                    var orgRes = await _dispatcher.DispatchAsync<GetBiginOrgCommand, Pipeline.Contracts.Responses.BiginOrgResponseContract>(orgCmd, ct).ConfigureAwait(false);
                    return new IpcResponse(env.Id, orgRes.IsSuccess, orgRes.Value, orgRes.ErrorCode, orgRes.ErrorMessage);

                case "bigin.org.photo.upload":
                    var photoDc = env.Payload.ValueKind == JsonValueKind.Object && env.Payload.TryGetProperty("dataCenter", out var photoDcProp) ? photoDcProp.GetString() : null;
                    var fileBytesBase64 = env.Payload.ValueKind == JsonValueKind.Object && env.Payload.TryGetProperty("fileBytes", out var fbProp) ? fbProp.GetString() : null;
                    byte[] fileBytes;
                    try
                    {
                        fileBytes = string.IsNullOrEmpty(fileBytesBase64) ? Array.Empty<byte>() : Convert.FromBase64String(fileBytesBase64);
                    }
                    catch (FormatException)
                    {
                        return new IpcResponse(env.Id, false, null, "INVALID_BASE64", "The 'fileBytes' property must be a valid base64-encoded string.");
                    }
                    var fileName = env.Payload.ValueKind == JsonValueKind.Object && env.Payload.TryGetProperty("fileName", out var fnProp) ? fnProp.GetString() ?? "photo.png" : "photo.png";
                    var contentType = env.Payload.ValueKind == JsonValueKind.Object && env.Payload.TryGetProperty("contentType", out var ctProp) ? ctProp.GetString() ?? "image/png" : "image/png";
                    var photoCmd = new UploadBiginOrgPhotoCommand(photoDc, fileBytes, fileName, contentType);
                    var photoRes = await _dispatcher.DispatchAsync<UploadBiginOrgPhotoCommand, Pipeline.Contracts.Responses.BiginPhotoUploadResponseContract>(photoCmd, ct).ConfigureAwait(false);
                    return new IpcResponse(env.Id, photoRes.IsSuccess, photoRes.Value, photoRes.ErrorCode, photoRes.ErrorMessage);

                case "bigin.relatedlists.read":
                    var module = env.Payload.ValueKind == JsonValueKind.Object && env.Payload.TryGetProperty("moduleApiName", out var mProp) ? mProp.GetString() ?? string.Empty : string.Empty;
                    var recordId = env.Payload.ValueKind == JsonValueKind.Object && env.Payload.TryGetProperty("recordId", out var rProp) ? rProp.GetString() ?? string.Empty : string.Empty;
                    var relatedList = env.Payload.ValueKind == JsonValueKind.Object && env.Payload.TryGetProperty("relatedListApiName", out var rlProp) ? rlProp.GetString() ?? string.Empty : string.Empty;
                    var fields = env.Payload.ValueKind == JsonValueKind.Object && env.Payload.TryGetProperty("fields", out var fProp) && fProp.ValueKind == JsonValueKind.Array
                        ? fProp.EnumerateArray().Select(f => f.GetString()!).Where(f => !string.IsNullOrEmpty(f)).ToList()
                        : null;
                    var page = env.Payload.ValueKind == JsonValueKind.Object && env.Payload.TryGetProperty("page", out var pProp) && pProp.ValueKind == JsonValueKind.Number ? pProp.GetInt32() : (int?)null;
                    var perPage = env.Payload.ValueKind == JsonValueKind.Object && env.Payload.TryGetProperty("perPage", out var ppProp) && ppProp.ValueKind == JsonValueKind.Number ? ppProp.GetInt32() : (int?)null;
                    var relDc = env.Payload.ValueKind == JsonValueKind.Object && env.Payload.TryGetProperty("dataCenter", out var relDcProp) ? relDcProp.GetString() : null;
                    var relCmd = new GetBiginRelatedRecordsCommand(module, recordId, relatedList, fields, page, perPage, relDc);
                    var relRes = await _dispatcher.DispatchAsync<GetBiginRelatedRecordsCommand, Pipeline.Contracts.Responses.BiginRelatedRecordsResponseContract>(relCmd, ct).ConfigureAwait(false);
                    return new IpcResponse(env.Id, relRes.IsSuccess, relRes.Value, relRes.ErrorCode, relRes.ErrorMessage);

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
