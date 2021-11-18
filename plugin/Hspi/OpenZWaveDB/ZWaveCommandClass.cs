using Newtonsoft.Json;
using System.Collections.Generic;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal record ZWaveCommandClass
    {
        [JsonProperty("commandclass_name")]
        public string? Name { get; init; }

        public IReadOnlyList<ZWaveCommandClassChannel>? Channels { get; init; }

        [JsonIgnore]
        public bool IsSetCommand => Name == "COMMAND_CLASS_CONFIGURATION";
    }
}