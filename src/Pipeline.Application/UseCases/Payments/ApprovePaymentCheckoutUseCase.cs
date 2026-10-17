using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Common;

namespace Pipeline.Application.UseCases.Payments;

public sealed record ApprovePaymentCheckoutCommand(string ApprovalId, string CheckoutId) : IUseCaseRequest
{
    public string OperationName => "payments.checkout.approve";
}

[RequiresPermission("payments.checkout.approve")]
public sealed class ApprovePaymentCheckoutUseCase : IUseCaseHandler<ApprovePaymentCheckoutCommand, PaymentApprovalResponse>
{
    public Task<Result<PaymentApprovalResponse>> HandleAsync(
        ApprovePaymentCheckoutCommand request,
        UseCaseContext context,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<PaymentApprovalResponse>.Success(
            new PaymentApprovalResponse(request.CheckoutId, true, context.Principal.UserId, DateTimeOffset.UtcNow)));
    }
}
