using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal record ZWaveDeviceParameter
    {
        [JsonProperty("param_id")]
        public int ParameterId { get; init; }

        [JsonProperty("id")]
        public int Id { get; init; }

        public string? Label { get; init; }
        public string? Description { get; init; }
        public string? Overview { get; init; }
        public string? Units { get; init; }
        public int Advanced { get; init; }
        public int Size { get; init; }
        public int Bitmask { get; init; }
        public int Minimum { get; init; }
        public int Maximum { get; init; }
        public int Default { get; init; }

        [JsonProperty("read_only")]
        private string? ReadOnlyJson { get; init; }

        [JsonProperty("write_only")]
        private string? WriteOnlyJson { get; init; }

        public IReadOnlyList<ZWaveDeviceParameterOption>? Options { get; init; }

        [JsonIgnore]
        public bool WriteOnly => WriteOnlyJson == "1";

        [JsonIgnore]
        public bool ReadOnly => ReadOnlyJson == "1";

        [JsonIgnore]
        public string LongerDescription
        {
            get
            {
                var list = new[] { Description, Overview, Label };
                return list.OrderByDescending(x => x?.Length ?? 0).First() ?? string.Empty;
            }
        }

        [JsonIgnore]
        public string DefaultValueDescription
        {
            get
            {
                if (HasOptions)
                {
                    return Options.FirstOrDefault(x => x.Value == Default)?.Description ?? string.Empty;
                }
                else
                {
                    return Invariant($"{Default} {Units}");
                }
            }
        }

        [JsonIgnore]
        public bool HasOptions => Options != null && Options.Count > 0;
    }
}