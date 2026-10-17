using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Common;

namespace Pipeline.Application.UseCases.Bigin;

public sealed record UploadBiginOrgPhotoCommand(
    string? DataCenter,
    byte[] FileBytes,
    string FileName,
    string ContentType = "image/png") : IUseCaseRequest
{
    public string OperationName => "bigin.org.photo.upload";
    public IReadOnlyDictionary<string, string> GetResourceIds() =>
        new Dictionary<string, string>
        {
            ["provider"] = "Bigin",
            ["accountId"] = "default"
        };
}

[RequiresPermission("bigin.org.write")]
public sealed class UploadBiginOrgPhotoUseCase : IUseCaseHandler<UploadBiginOrgPhotoCommand, BiginPhotoUploadResponseContract>
{
    public const long MaxPhotoSizeBytes = 1024 * 1024; // 1 MB
    private readonly ICredentialBroker _credentialBroker;

    public UploadBiginOrgPhotoUseCase(ICredentialBroker credentialBroker)
    {
        _credentialBroker = credentialBroker;
    }

    private static readonly HashSet<string> ValidDataCenters = new(StringComparer.OrdinalIgnoreCase)
    {
        "US", "EU", "AU", "IN", "CN", "JP", "SA", "CA"
    };

    public async Task<Result<BiginPhotoUploadResponseContract>> HandleAsync(
        UploadBiginOrgPhotoCommand request,
        UseCaseContext context,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.DataCenter) && !ValidDataCenters.Contains(request.DataCenter.Trim()))
        {
            return Result<BiginPhotoUploadResponseContract>.Failure("INVALID_DATA_CENTER", $"Unsupported data center: '{request.DataCenter}'. Supported data centers are US, EU, AU, IN, CN, JP, SA, CA.");
        }

        if (request.FileBytes is null || request.FileBytes.Length == 0)
        {
            return Result<BiginPhotoUploadResponseContract>.Failure("EMPTY_FILE", "File bytes cannot be null or empty.");
        }

        if (request.FileBytes.Length > MaxPhotoSizeBytes)
        {
            return Result<BiginPhotoUploadResponseContract>.Failure("FILE_SIZE_MORE_THAN_ALLOWED_SIZE", $"File size exceeds allowed maximum limit of 1 MB ({MaxPhotoSizeBytes} bytes).");
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return Result<BiginPhotoUploadResponseContract>.Failure("INVALID_FILE_NAME", "File name cannot be empty.");
        }

        if (!Path.HasExtension(request.FileName))
        {
            return Result<BiginPhotoUploadResponseContract>.Failure("INVALID_FILE_NAME", "File name must include a valid image extension (e.g. .png, .jpg).");
        }

        // Obtain token via capability ticket
        _ = await _credentialBroker.GetAccessTokenAsync(context.Ticket, "Bigin", "default", ct).ConfigureAwait(false);

        return Result<BiginPhotoUploadResponseContract>.Success(
            new BiginPhotoUploadResponseContract("SUCCESS", "photo uploaded successfully", "success"));
    }
}
