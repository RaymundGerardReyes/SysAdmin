using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Domain.Common;
using Pipeline.Domain.Security;

namespace Pipeline.Application.UseCases;

public sealed record UseCaseContext(
    Principal Principal,
    AuthorizationTicket Ticket,
    string CorrelationId);

public interface IUseCaseHandler
{
}

public interface IUseCaseHandler<in TRequest, TResponse> : IUseCaseHandler
    where TRequest : IUseCaseRequest
{
    Task<Result<TResponse>> HandleAsync(
        TRequest request,
        UseCaseContext context,
        CancellationToken ct = default);
}
