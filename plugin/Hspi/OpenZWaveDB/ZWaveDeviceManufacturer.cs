#nullable enable

using System.Text.Json.Serialization;

namespace Hspi.OpenZWaveDB
{
    internal record ZWaveDeviceManufacturer
    {
        [JsonPropertyName("label")]

        public string? Label { get; init; }
    }
}