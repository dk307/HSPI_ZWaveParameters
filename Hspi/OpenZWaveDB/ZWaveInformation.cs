using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal record ZWaveEndPoints
    {
        public IReadOnlyList<ZWaveCommandClass>? CommandClass { get; init; }
    }

    internal record ZWaveCommandClass
    {
        [JsonProperty("commandclass_name")]
        public string? Name { get; init; }

        public IReadOnlyList<ZWaveCommandClassChannel>? Channels { get; init; }

        [JsonIgnore]
        public bool IsSetCommand => Name == "COMMAND_CLASS_CONFIGURATION";
    }

    internal record ZWaveCommandClassChannel
    {
        [JsonProperty("config")]
        public string? Config { get; init; }

        public string? Label { get; init; }
        public string? Overview { get; init; }

        [JsonIgnore]
        public int? ParameterId
        {
            get
            {
                var list = Config?.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (list != null && list.Length == 2)
                {
                    if (int.TryParse(list[1], out var value))
                    {
                        return value;
                    }
                }
                return null;
            }
        }
    }

    internal record ZWaveInformation
    {
        [JsonProperty("database_id")]
        public string? Id { get; init; }
        public string? Overview { get; init; }
        public string? Description { get; init; }
        public string? Label { get; init; }
        public ZWaveDeviceManufacturer? Manufacturer { get; init; }

        [JsonProperty("parameters")]
        public IReadOnlyList<ZWaveDeviceParameter>? Parameters { get; init; }

        [JsonProperty("endpoints")]
        public IReadOnlyList<ZWaveEndPoints>? EndPoints { get; init; }

        public ZWaveCommandClassChannel? GetCommandClassChannelForParameter(int parameter)
        {
            return EndPoints.FirstOrDefault()?.CommandClass?.
                        FirstOrDefault(x => x.IsSetCommand)?.
                        Channels.FirstOrDefault(x => x.ParameterId == parameter);
        }

        [JsonIgnore]
        public Uri WebUrl => new(string.Format(webUrlFormat, Id), UriKind.Absolute);

        private const string webUrlFormat = "https://www.opensmarthouse.org/zwavedatabase/{0}";
    }
}