using System.Text.Json;
using System.Text.Json.Serialization;

namespace ToyStore.API.Extensions;

/// <summary>
/// Accepts TimeSpan values from JSON in multiple formats:
///   "HH:mm"       e.g. "08:00"
///   "HH:mm:ss"    e.g. "08:00:00"
///   "d.HH:mm:ss"  e.g. "0.08:00:00"   (standard .NET round-trip format)
/// Always writes in "HH:mm:ss" format for consistency.
/// </summary>
public sealed class FlexibleTimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            throw new JsonException("TimeSpan value is empty.");

        // Standard .NET TimeSpan.Parse handles "HH:mm:ss", "d.HH:mm:ss", "HH:mm" etc.
        if (TimeSpan.TryParse(raw, out var result))
            return result;

        throw new JsonException($"Cannot convert '{raw}' to TimeSpan. Expected format: HH:mm or HH:mm:ss.");
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        // Always serialize as "HH:mm:ss" so frontend slicing works consistently
        writer.WriteStringValue(value.ToString(@"hh\:mm\:ss"));
    }
}

/// <summary>Same converter but for Nullable&lt;TimeSpan&gt;.</summary>
public sealed class FlexibleNullableTimeSpanConverter : JsonConverter<TimeSpan?>
{
    public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (TimeSpan.TryParse(raw, out var result))
            return result;

        throw new JsonException($"Cannot convert '{raw}' to TimeSpan?. Expected format: HH:mm or HH:mm:ss.");
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value.ToString(@"hh\:mm\:ss"));
    }
}
