namespace Pipeline.Contracts.Errors;

public sealed record ProblemDetailsContract(
    string Type,
    string Title,
    int Status,
    string Detail,
    string Instance,
    string Code,
    string CorrelationId,
    IReadOnlyDictionary<string, string[]>? Errors = null);
