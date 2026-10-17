using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Common;

namespace Pipeline.Application.UseCases.Sheets;

public sealed record WriteSheetRangeCommand(
    string SpreadsheetId,
    string Range,
    IReadOnlyList<IReadOnlyList<string>> Values,
    string IdempotencyKey) : IUseCaseRequest
{
    public string OperationName => "sheets.write";
    public IReadOnlyDictionary<string, string> GetResourceIds() =>
        new Dictionary<string, string> { ["spreadsheetId"] = SpreadsheetId };
}

[RequiresPermission("sheets.values.write")]
public sealed class WriteSheetRangeUseCase : IUseCaseHandler<WriteSheetRangeCommand, SheetWriteResponse>
{
    private readonly ICredentialBroker _credentialBroker;

    public WriteSheetRangeUseCase(ICredentialBroker credentialBroker)
    {
        _credentialBroker = credentialBroker;
    }

    public async Task<Result<SheetWriteResponse>> HandleAsync(
        WriteSheetRangeCommand request,
        UseCaseContext context,
        CancellationToken ct = default)
    {
        var token = await _credentialBroker.GetAccessTokenAsync(context.Ticket, "GoogleSheets", "default", ct).ConfigureAwait(false);

        var rows = request.Values.Count;
        var cols = rows > 0 ? request.Values[0].Count : 0;
        return Result<SheetWriteResponse>.Success(new SheetWriteResponse(rows, cols, rows * cols));
    }
}
