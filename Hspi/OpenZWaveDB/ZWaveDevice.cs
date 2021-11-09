using Newtonsoft.Json;
using System;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal record ZWaveDevice
    {
        [JsonProperty("version_min")]
        public Version? VersionMin { get; init; }

        [JsonProperty("version_max")]
        public Version? VersionMax { get; init; }

        [JsonProperty("id")]
        public int? Id { get; init; }
    }
}