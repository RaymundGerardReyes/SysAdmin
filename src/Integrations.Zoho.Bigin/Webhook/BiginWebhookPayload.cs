using System.Text.Json;
using System.Text.Json.Serialization;

namespace Integrations.Zoho.Bigin.Webhook;

/// <summary>
/// Parsed payload received by the notification webhook URL from Zoho Bigin.
/// </summary>
public sealed record BiginWebhookPayload
{
    [JsonPropertyName("server_time")]
    public long ServerTime { get; init; }

    [JsonIgnore]
    public DateTimeOffset ServerTimeUtc => DateTimeOffset.FromUnixTimeMilliseconds(ServerTime);

    [JsonPropertyName("query_params")]
    [JsonConverter(typeof(TolerantStringDictionaryConverter))]
    public IReadOnlyDictionary<string, string>? QueryParams { get; init; }

    [JsonPropertyName("module")]
    public required string Module { get; init; }

    [JsonPropertyName("resource_uri")]
    public required string ResourceUri { get; init; }

    [JsonPropertyName("ids")]
    public required IReadOnlyList<string> Ids { get; init; }

    [JsonPropertyName("affected_fields")]
    public JsonElement? RawAffectedFields { get; init; }

    [JsonPropertyName("operation")]
    public required string Operation { get; init; }

    [JsonPropertyName("channel_id")]
    public required string ChannelId { get; init; }

    [JsonPropertyName("token")]
    public string? Token { get; init; }

    /// <summary>
    /// Parses affected fields into a lookup of record ID -> modified field names.
    /// In Bigin payloads, affected_fields is an array of objects: [ { "5436046000000930004": [ "Department", "Mobile" ] } ].
    /// Merges multiple elements if the same record ID appears more than once.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetAffectedFieldsLookup()
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        if (!RawAffectedFields.HasValue || RawAffectedFields.Value.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var element in RawAffectedFields.Value.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var prop in element.EnumerateObject())
            {
                var fieldList = new List<string>();
                if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var field in prop.Value.EnumerateArray())
                    {
                        var fieldName = field.GetString();
                        if (!string.IsNullOrEmpty(fieldName))
                        {
                            fieldList.Add(fieldName);
                        }
                    }
                }

                if (result.TryGetValue(prop.Name, out var existingList))
                {
                    var merged = existingList.ToList();
                    merged.AddRange(fieldList.Where(f => !merged.Contains(f, StringComparer.Ordinal)));
                    result[prop.Name] = merged;
                }
                else
                {
                    result[prop.Name] = fieldList;
                }
            }
        }

        return result;
    }
}

/// <summary>
/// Tolerant dictionary converter that handles primitive JSON values (numbers, booleans, strings) as string values.
/// </summary>
public sealed class TolerantStringDictionaryConverter : JsonConverter<IReadOnlyDictionary<string, string>>
{
    public override IReadOnlyDictionary<string, string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected StartObject token for dictionary, got {reader.TokenType}.");
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            string? stringValue = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => null,
                _ => property.Value.GetRawText()
            };

            if (stringValue is not null)
            {
                dictionary[property.Name] = stringValue;
            }
        }

        return dictionary;
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<string, string> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (k, v) in value)
        {
            writer.WriteString(k, v);
        }
        writer.WriteEndObject();
    }
}
