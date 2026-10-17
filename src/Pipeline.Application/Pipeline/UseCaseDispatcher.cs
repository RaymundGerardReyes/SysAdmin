using Microsoft.Extensions.DependencyInjection;
using Pipeline.Application.Abstractions;
using Pipeline.Application.Pipeline.Behaviors;
using Pipeline.Application.UseCases;
using Pipeline.Contracts.Errors;
using Pipeline.Domain.Common;

namespace Pipeline.Application.Pipeline;

public sealed class UseCaseDispatcher : IUseCaseDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AuthenticationBehavior _authnBehavior;
    private readonly ValidationBehavior _validationBehavior;
    private readonly AuthorizationBehavior _authzBehavior;
    private readonly RateLimitBehavior _rateLimitBehavior;
    private readonly IdempotencyBehavior _idempotencyBehavior;
    private readonly AuditBehavior _auditBehavior;
    private readonly IPrincipalAccessor _principalAccessor;

    public UseCaseDispatcher(
        IServiceProvider serviceProvider,
        AuthenticationBehavior authnBehavior,
        ValidationBehavior validationBehavior,
        AuthorizationBehavior authzBehavior,
        RateLimitBehavior rateLimitBehavior,
        IdempotencyBehavior idempotencyBehavior,
        AuditBehavior auditBehavior,
        IPrincipalAccessor principalAccessor)
    {
        _serviceProvider = serviceProvider;
        _authnBehavior = authnBehavior;
        _validationBehavior = validationBehavior;
        _authzBehavior = authzBehavior;
        _rateLimitBehavior = rateLimitBehavior;
        _idempotencyBehavior = idempotencyBehavior;
        _auditBehavior = auditBehavior;
        _principalAccessor = principalAccessor;
    }

    public async Task<Result<TResponse>> DispatchAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken ct = default)
        where TRequest : IUseCaseRequest
    {
        var correlationId = request.CorrelationId ?? Guid.NewGuid().ToString("N");
        var userId = _principalAccessor.Current?.UserId ?? "ANONYMOUS";

        try
        {
            // 1. Authentication
            var authnFailure = _authnBehavior.Check<TResponse>();
            if (authnFailure is not null)
            {
                await _auditBehavior.AuditCompletionAsync(userId, request.OperationName, "Deny", authnFailure.ErrorCode!, authnFailure.PathId, correlationId, ct).ConfigureAwait(false);
                return authnFailure;
            }

            // 2. Validation
            var valFailure = _validationBehavior.Validate<TRequest, TResponse>(request);
            if (valFailure is not null)
            {
                await _auditBehavior.AuditCompletionAsync(userId, request.OperationName, "Deny", valFailure.ErrorCode!, valFailure.PathId, correlationId, ct).ConfigureAwait(false);
                return valFailure;
            }

            // Resolve Handler
            var handler = _serviceProvider.GetService<IUseCaseHandler<TRequest, TResponse>>();
            if (handler is null)
            {
                var unmapped = Result<TResponse>.Failure(ErrorCodes.OperationUnmapped, $"No handler registered for {typeof(TRequest).Name}", "AUTHZ_P04");
                await _auditBehavior.AuditCompletionAsync(userId, request.OperationName, "Deny", unmapped.ErrorCode!, unmapped.PathId, correlationId, ct).ConfigureAwait(false);
                return unmapped;
            }

            // 3. Authorization (S01..S11) -> issues Ticket
            var (authzFailure, ticket) = await _authzBehavior.AuthorizeAsync<TRequest, TResponse>(request, handler.GetType(), ct).ConfigureAwait(false);
            if (authzFailure is not null)
            {
                return authzFailure;
            }

            // 4. Rate Limit
            var rateFailure = await _rateLimitBehavior.CheckRateLimitAsync<TRequest, TResponse>(request, ct).ConfigureAwait(false);
            if (rateFailure is not null)
            {
                await _auditBehavior.AuditCompletionAsync(userId, request.OperationName, "Deny", rateFailure.ErrorCode!, rateFailure.PathId, correlationId, ct).ConfigureAwait(false);
                return rateFailure;
            }

            // 5. Idempotency Check
            var fingerprint = AuthorizationBehavior.ComputeFingerprint(request);
            var (existingResult, requiresExecution) = await _idempotencyBehavior.CheckAsync<TRequest, TResponse>(request, fingerprint, ct).ConfigureAwait(false);
            if (!requiresExecution && existingResult is not null)
            {
                return existingResult;
            }

            // 6. Audit Start
            await _auditBehavior.AuditStartAsync(userId, request.OperationName, correlationId, ct).ConfigureAwait(false);

            // 7. Invoke Handler with Capability Ticket & Context
            var context = new UseCaseContext(_principalAccessor.Current!, ticket!, correlationId);
            Result<TResponse> handlerResult;

            try
            {
                handlerResult = await handler.HandleAsync(request, context, ct).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                // IDEM_P05: Ambiguous timeout marks unknown
                await _idempotencyBehavior.MarkUnknownAsync(request.IdempotencyKey, ct).ConfigureAwait(false);
                var timeoutRes = Result<TResponse>.Failure(ErrorCodes.InternalDeny, "Operation timed out with ambiguous outcome.", "IDEM_P05");
                await _auditBehavior.AuditCompletionAsync(userId, request.OperationName, "Failure", "TIMEOUT", "IDEM_P05", correlationId, ct).ConfigureAwait(false);
                return timeoutRes;
            }

            // 8. Idempotency Completion & Audit
            if (handlerResult.IsSuccess && handlerResult.Value is not null)
            {
                await _idempotencyBehavior.CompleteAsync(request.IdempotencyKey, handlerResult.Value, ct).ConfigureAwait(false);
            }

            await _auditBehavior.AuditCompletionAsync(
                userId,
                request.OperationName,
                handlerResult.Outcome,
                handlerResult.ErrorCode ?? "OK",
                handlerResult.PathId,
                correlationId,
                ct).ConfigureAwait(false);

            return handlerResult;
        }
        catch (Exception)
        {
            var failRes = Result<TResponse>.Failure(ErrorCodes.InternalDeny, "Internal processing error occurred.", "AUTHZ_PX");
            try
            {
                await _auditBehavior.AuditCompletionAsync(userId, request.OperationName, "Failure", ErrorCodes.InternalDeny, "AUTHZ_PX", correlationId, ct).ConfigureAwait(false);
            }
            catch
            {
                // Ignore audit failure in crash path
            }
            return failRes;
        }
    }
}
