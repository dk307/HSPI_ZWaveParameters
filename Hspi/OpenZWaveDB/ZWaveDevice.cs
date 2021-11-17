using Destructurama.Attributed;
using Newtonsoft.Json;
using System;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal record ZWaveDevice
    {
        [LogAsScalar]
        [JsonProperty("version_min")]
        public Version? VersionMin { get; init; }

        [LogAsScalar]
        [JsonProperty("version_max")]
        public Version? VersionMax { get; init; }

        [JsonProperty("id")]
        public int? Id { get; init; }
    }
}