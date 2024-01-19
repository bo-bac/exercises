using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            DateOnly.ParseExact(reader.GetString()!,
                "yyyy-MM-dd", CultureInfo.InvariantCulture);

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(
                "yyyy-MM-dd", CultureInfo.InvariantCulture));
}


public class SummaryItem 
{
    [JsonConverter(typeof(DateOnlyJsonConverter))]
    [JsonPropertyName("date")]
    public DateOnly Date { get; set; } = default!;

    [JsonPropertyName("count")]
    public ulong Count { get; set; }
}

public class Summary
{
    [JsonPropertyName("hashes")]
    public SummaryItem[] Hashes { get; set; } = default!;
}
