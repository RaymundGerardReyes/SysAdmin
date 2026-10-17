using Pipeline.Contracts.Errors;
using Pipeline.Domain.Common;

namespace Pipeline.Application.Pipeline.Behaviors;

public sealed class ValidationBehavior
{
    public Result<TResponse>? Validate<TRequest, TResponse>(TRequest request)
        where TRequest : IUseCaseRequest
    {
        if (request is null)
        {
            return Result<TResponse>.Failure(ErrorCodes.InvalidRequest, "Request cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(request.OperationName))
        {
            return Result<TResponse>.Failure(ErrorCodes.ValidationFailed, "Operation name cannot be empty.");
        }

        return null;
    }
}
