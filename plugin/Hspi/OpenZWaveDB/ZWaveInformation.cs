using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal record ZWaveInformation
    {
        [JsonProperty("database_id")]
        public string? Id { get; init; }
        public string? Overview { get; init; }
        public string? Description { get; init; }
        public string? Label { get; init; }
        public ZWaveDeviceManufacturer? Manufacturer { get; init; }

        public byte Approved { get; init; }
        public byte Deleted { get; init; }

        [JsonProperty("parameters")]
        public IReadOnlyList<ZWaveDeviceParameter>? Parameters { get; init; }

        [JsonProperty("endpoints")]
        public IReadOnlyList<ZWaveEndPoints>? EndPoints { get; init; }

        public ZWaveCommandClassChannel? GetCommandClassChannelForParameter(int parameter)
        {
            return EndPoints?.FirstOrDefault()?.CommandClass?.
                        FirstOrDefault(x => x.IsSetCommand)?.
                        Channels.FirstOrDefault(x => x.ParameterId == parameter);
        }

        [JsonIgnore]
        public Uri WebUrl => new(string.Format(webUrlFormat, Id), UriKind.Absolute);

        private const string webUrlFormat = "https://www.opensmarthouse.org/zwavedatabase/{0}";
    }
}