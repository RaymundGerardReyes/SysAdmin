using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Common;

namespace Pipeline.Application.UseCases.Sheets;

public sealed record ReadSheetRangeCommand(string SpreadsheetId, string Range) : IUseCaseRequest
{
    public string OperationName => "sheets.read";
    public IReadOnlyDictionary<string, string> GetResourceIds() =>
        new Dictionary<string, string> { ["spreadsheetId"] = SpreadsheetId };
}

[RequiresPermission("sheets.values.read")]
public sealed class ReadSheetRangeUseCase : IUseCaseHandler<ReadSheetRangeCommand, SheetRangeResponse>
{
    private readonly ICredentialBroker _credentialBroker;

    public ReadSheetRangeUseCase(ICredentialBroker credentialBroker)
    {
        _credentialBroker = credentialBroker;
    }

    public async Task<Result<SheetRangeResponse>> HandleAsync(
        ReadSheetRangeCommand request,
        UseCaseContext context,
        CancellationToken ct = default)
    {
        // Obtain token via capability ticket
        var token = await _credentialBroker.GetAccessTokenAsync(context.Ticket, "GoogleSheets", "default", ct).ConfigureAwait(false);

        // Return result
        var sampleValues = new List<IReadOnlyList<string>>
        {
            new List<string> { "Header1", "Header2" },
            new List<string> { "Value1", "Value2" }
        };

        return Result<SheetRangeResponse>.Success(new SheetRangeResponse(request.Range, sampleValues));
    }
}
