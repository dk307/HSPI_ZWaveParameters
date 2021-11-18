using Newtonsoft.Json;
using System;

#nullable enable

namespace Hspi.OpenZWaveDB
{
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
}