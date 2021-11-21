using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using static System.FormattableString;

#nullable enable

namespace Hspi.OpenZWaveDB.Model
{
    public record ZWaveDeviceParameter
    {
        [JsonPropertyName("param_id")]
        public byte ParameterId { get; init; }

        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("label")]
        public string? Label { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("overview")]
        public string? Overview { get; init; }

        [JsonPropertyName("units")]
        public string? Units { get; init; }

        [JsonPropertyName("size")]
        public byte Size { get; init; }

        [JsonPropertyName("bitmask")]
        public int Bitmask { get; init; }

        [JsonPropertyName("minimum")]
        public int Minimum { get; init; }

        [JsonPropertyName("maximum")]
        public int Maximum { get; init; }

        [JsonPropertyName("default")]
        public int Default { get; init; }

        [JsonPropertyName("read_only")]
        public string? ReadOnlyJson { get; init; }

        [JsonPropertyName("write_only")]
        public string? WriteOnlyJson { get; init; }

        [JsonPropertyName("limit_options")]
        public string? LimitOptionsJson { get; init; }

        [JsonPropertyName("options")]
        public IReadOnlyList<ZWaveDeviceParameterOption>? Options { get; init; }

        [JsonIgnore]
        public bool WriteOnly => WriteOnlyJson == "1";

        [JsonIgnore]
        public bool LimitOptions => LimitOptionsJson == "1";

        [JsonIgnore]
        public IReadOnlyList<ZWaveDeviceParameter>? SubParameters { get; init; }

        [JsonIgnore]
        public bool ReadOnly => ReadOnlyJson == "1";

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

        [JsonIgnore]
        public bool HasSubParameters => SubParameters != null && SubParameters.Count > 0;
    }
}