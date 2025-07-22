using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReviewMetricsProcessor;

public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateTimeString = reader.GetString();
        if (DateTime.TryParse(dateTimeString, out var dateTime))
        {
            // Convert to UTC if it's not already
            return dateTime.Kind switch
            {
                DateTimeKind.Local => dateTime.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
                _ => dateTime
            };
        }
        throw new JsonException($"Unable to parse DateTime: {dateTimeString}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }
}