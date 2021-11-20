using Destructurama.Attributed;
using System;
using System.Text.Json.Serialization;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal record ZWaveDevice
    {
        [LogAsScalar]
        [JsonPropertyName("version_min")]
        public Version? VersionMin { get; init; }

        [LogAsScalar]
        [JsonPropertyName("version_max")]
        public Version? VersionMax { get; init; }

        [JsonPropertyName("id")]
        public int? Id { get; init; }
    }
}