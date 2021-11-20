using System.Collections.Generic;
using System.Text.Json.Serialization;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal record ZWaveEndPoints
    {
        [JsonPropertyName("commandclass")]

        public IReadOnlyList<ZWaveCommandClass>? CommandClass { get; init; }
    }
}