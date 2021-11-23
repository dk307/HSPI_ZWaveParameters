using System.Text.Json.Serialization;
using static System.FormattableString;

#nullable enable

namespace Hspi.OpenZWaveDB.Model
{
    public record ZWaveDeviceParameterOption
    {
        [JsonPropertyName("label")]

        public string? Label { get; init; }

        [JsonPropertyName("value")]
        public int Value { get; init; }

        [JsonIgnore]
        public string Description => Invariant($"{Value} - {Label}");
    }
}