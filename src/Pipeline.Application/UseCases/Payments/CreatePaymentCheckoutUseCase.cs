using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Common;

namespace Pipeline.Application.UseCases.Payments;

public sealed record CreatePaymentCheckoutCommand(
    string AccountId,
    decimal Amount,
    string Currency,
    string Reference,
    string IdempotencyKey) : IUseCaseRequest
{
    public string OperationName => "payments.checkout.create";
    public IReadOnlyDictionary<string, string> GetResourceIds() =>
        new Dictionary<string, string> { ["paymentAccountId"] = AccountId };
}

[RequiresPermission("payments.checkout.create")]
public sealed class CreatePaymentCheckoutUseCase : IUseCaseHandler<CreatePaymentCheckoutCommand, PaymentCheckoutResponse>
{
    private readonly ICredentialBroker _credentialBroker;

    public CreatePaymentCheckoutUseCase(ICredentialBroker credentialBroker)
    {
        _credentialBroker = credentialBroker;
    }

    public async Task<Result<PaymentCheckoutResponse>> HandleAsync(
        CreatePaymentCheckoutCommand request,
        UseCaseContext context,
        CancellationToken ct = default)
    {
        var token = await _credentialBroker.GetAccessTokenAsync(context.Ticket, "PayMongo", request.AccountId, ct).ConfigureAwait(false);
        var checkoutId = $"chk_{Guid.NewGuid():N}";
        return Result<PaymentCheckoutResponse>.Success(new PaymentCheckoutResponse(checkoutId, "pending", $"https://checkout.paymongo.com/{checkoutId}"));
    }
}
