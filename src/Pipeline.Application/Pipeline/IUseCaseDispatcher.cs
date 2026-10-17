using Pipeline.Domain.Common;

namespace Pipeline.Application.Pipeline;

public interface IUseCaseRequest
{
    string OperationName { get; }
    string? IdempotencyKey => null;
    string? CorrelationId => null;
    IReadOnlyDictionary<string, string> GetResourceIds() => new Dictionary<string, string>();
}

public interface IUseCaseDispatcher
{
    Task<Result<TResponse>> DispatchAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken ct = default)
        where TRequest : IUseCaseRequest;
}
