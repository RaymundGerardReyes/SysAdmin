namespace Integrations.Zoho.Bigin;

/// <summary>
/// Supported Zoho Bigin Data Centers (DC).
/// Bigin APIs are available across eight different domains.
/// </summary>
public enum BiginDataCenter
{
    US,
    EU,
    AU,
    IN,
    CN,
    JP,
    SA,
    CA
}

public static class BiginDataCenterExtensions
{
    public static string GetDomain(this BiginDataCenter dataCenter) => dataCenter switch
    {
        BiginDataCenter.US => "www.zohoapis.com",
        BiginDataCenter.EU => "www.zohoapis.eu",
        BiginDataCenter.AU => "www.zohoapis.com.au",
        BiginDataCenter.IN => "www.zohoapis.in",
        BiginDataCenter.CN => "www.zohoapis.com.cn",
        BiginDataCenter.JP => "www.zohoapis.jp",
        BiginDataCenter.SA => "www.zohoapis.sa",
        BiginDataCenter.CA => "www.zohoapis.ca",
        _ => throw new ArgumentOutOfRangeException(nameof(dataCenter), dataCenter, "Unknown Bigin Data Center")
    };

    public static string GetBaseUrl(this BiginDataCenter dataCenter) =>
        $"https://{dataCenter.GetDomain()}";

    public static Uri GetEndpointUri(this BiginDataCenter dataCenter, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        var trimmed = relativePath.TrimStart('/');
        return new Uri($"https://{dataCenter.GetDomain()}/bigin/v2/{trimmed}");
    }

    public static string GetWatchEndpointUrl(this BiginDataCenter dataCenter) =>
        $"https://{dataCenter.GetDomain()}/bigin/v2/actions/watch";

    public static string GetModulesEndpointUrl(this BiginDataCenter dataCenter) =>
        $"https://{dataCenter.GetDomain()}/bigin/v2/settings/modules";

    public static string GetOrgEndpointUrl(this BiginDataCenter dataCenter) =>
        $"https://{dataCenter.GetDomain()}/bigin/v2/org";

    public static string GetOrgPhotoEndpointUrl(this BiginDataCenter dataCenter) =>
        $"https://{dataCenter.GetDomain()}/bigin/v2/org/photo";

    public static string GetRelatedListEndpointUrl(
        this BiginDataCenter dataCenter,
        string moduleApiName,
        string recordId,
        string relatedListApiName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleApiName);
        ArgumentException.ThrowIfNullOrWhiteSpace(recordId);
        ArgumentException.ThrowIfNullOrWhiteSpace(relatedListApiName);

        var normalizedModule = BiginModules.NormalizeModuleApiName(moduleApiName);

        return $"https://{dataCenter.GetDomain()}/bigin/v2/{Uri.EscapeDataString(normalizedModule)}/{Uri.EscapeDataString(recordId.Trim())}/{Uri.EscapeDataString(relatedListApiName.Trim())}";
    }

    public static bool TryParse(string? value, out BiginDataCenter dataCenter)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            dataCenter = BiginDataCenter.US;
            return false;
        }

        switch (value.Trim().ToUpperInvariant())
        {
            case "US":
                dataCenter = BiginDataCenter.US;
                return true;
            case "EU":
                dataCenter = BiginDataCenter.EU;
                return true;
            case "AU":
                dataCenter = BiginDataCenter.AU;
                return true;
            case "IN":
                dataCenter = BiginDataCenter.IN;
                return true;
            case "CN":
                dataCenter = BiginDataCenter.CN;
                return true;
            case "JP":
                dataCenter = BiginDataCenter.JP;
                return true;
            case "SA":
                dataCenter = BiginDataCenter.SA;
                return true;
            case "CA":
                dataCenter = BiginDataCenter.CA;
                return true;
            default:
                dataCenter = BiginDataCenter.US;
                return false;
        }
    }
}
