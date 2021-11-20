using System.Collections.Generic;
using System.Text.Json.Serialization;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal record ZWaveCommandClass
    {
        [JsonPropertyName("commandclass_name")]
        public string? Name { get; init; }

        [JsonPropertyName("channels")]
        public IReadOnlyList<ZWaveCommandClassChannel>? Channels { get; init; }

        [JsonIgnore]
        public bool IsSetCommand => Name == "COMMAND_CLASS_CONFIGURATION";
    }
}