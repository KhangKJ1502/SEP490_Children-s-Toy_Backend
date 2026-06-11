using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ToyStore.Application.DTOs.BankAccounts;

public class BankLookupItemDto
{
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("bin")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string Bin { get; set; } = string.Empty;

    [JsonPropertyName("short_name")]
    public string ShortName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("logo_url")]
    public string LogoUrl { get; set; } = string.Empty;

    [JsonPropertyName("icon_url")]
    public string IconUrl { get; set; } = string.Empty;

    [JsonPropertyName("lookup_supported")]
    public JsonElement LookupSupportedElement { get; set; }

    [JsonIgnore]
    public bool LookupSupported
    {
        get
        {
            if (LookupSupportedElement.ValueKind == JsonValueKind.True) return true;
            if (LookupSupportedElement.ValueKind == JsonValueKind.False) return false;
            if (LookupSupportedElement.ValueKind == JsonValueKind.Number)
            {
                if (LookupSupportedElement.TryGetInt32(out var val)) return val == 1;
            }
            if (LookupSupportedElement.ValueKind == JsonValueKind.String)
            {
                var val = LookupSupportedElement.GetString();
                return val == "1" || string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}

public class StringOrNumberConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt64(out var longVal))
            {
                return longVal.ToString();
            }
            return reader.GetDouble().ToString();
        }
        return reader.GetString() ?? string.Empty;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

