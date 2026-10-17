namespace Integrations.Zoho.Bigin;

/// <summary>
/// Error codes documented for Zoho Bigin Modules API.
/// </summary>
public static class BiginErrorCodes
{
    /// <summary>
    /// HTTP 404 - The specified request URL is incorrect.
    /// </summary>
    public const string InvalidUrlPattern = "INVALID_URL_PATTERN";

    /// <summary>
    /// HTTP 401 - Invalid OAuth scope to access this URL.
    /// </summary>
    public const string OAuthScopeMismatch = "OAUTH_SCOPE_MISMATCH";

    /// <summary>
    /// HTTP 403 - Client does not have permission to view this resource.
    /// </summary>
    public const string NoPermission = "NO_PERMISSION";

    /// <summary>
    /// HTTP 400 - The HTTP request method type is not a valid one.
    /// </summary>
    public const string InvalidRequestMethod = "INVALID_REQUEST_METHOD";

    /// <summary>
    /// HTTP 400 - User does not have sufficient privilege to read module details.
    /// </summary>
    public const string AuthorizationFailed = "AUTHORIZATION_FAILED";

    /// <summary>
    /// HTTP 415 - Image resolution > 10 MP or file size > 1 MB for organization photo upload.
    /// </summary>
    public const string FileSizeMoreThanAllowedSize = "FILE_SIZE_MORE_THAN_ALLOWED_SIZE";

    /// <summary>
    /// HTTP 400 - One or more mandatory fields are missing.
    /// </summary>
    public const string MandatoryNotFound = "MANDATORY_NOT_FOUND";

    /// <summary>
    /// HTTP 400 - Invalid data provided.
    /// </summary>
    public const string InvalidData = "INVALID_DATA";
}
