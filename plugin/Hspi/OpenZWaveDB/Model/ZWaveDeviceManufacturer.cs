#nullable enable

using System.Text.Json.Serialization;

namespace Hspi.OpenZWaveDB.Model
{
    public record ZWaveDeviceManufacturer
    {
        [JsonPropertyName("label")]

        public string? Label { get; init; }
    }
}