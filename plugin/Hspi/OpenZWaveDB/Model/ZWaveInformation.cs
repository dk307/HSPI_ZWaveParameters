using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;

#nullable enable

namespace Hspi.OpenZWaveDB.Model
{
    internal record ZWaveInformationBasic
    {
        [JsonPropertyName("database_id")]
        public int Id { get; init; }

        [JsonPropertyName("approved")]
        public int? Approved { get; init; }

        [JsonPropertyName("deleted")]
        public int? Deleted { get; init; }
    }

    public record ZWaveInformation
    {
        [JsonPropertyName("database_id")]
        public int Id { get; init; }

        [JsonPropertyName("approved")]
        public byte Approved { get; init; }

        [JsonPropertyName("deleted")]
        public byte Deleted { get; init; }

        [JsonPropertyName("overview")]
        public string? Overview { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("label")]
        public string? Label { get; init; }

        [JsonPropertyName("manufacturer")]
        public ZWaveDeviceManufacturer? Manufacturer { get; init; }

        [JsonPropertyName("parameters")]
        public IReadOnlyList<ZWaveDeviceParameter>? Parameters { get; init; }

        [JsonPropertyName("endpoints")]
        public IReadOnlyList<ZWaveEndPoints>? EndPoints { get; init; }

        public ZWaveCommandClassChannel? GetCommandClassChannelForParameter(int parameter)
        {
            return EndPoints?.FirstOrDefault()?.CommandClass?.
                        FirstOrDefault(x => x.IsSetCommand)?.
                        Channels.FirstOrDefault(x => x.ParameterId == parameter);
        }

        [JsonIgnore]
        public bool HasRefreshableParameters => Parameters?.Any(x => !x.WriteOnly) ?? false;

        [JsonIgnore]
        public Uri WebUrl => new(string.Format(CultureInfo.InvariantCulture, webUrlFormat, Id), UriKind.Absolute);

#pragma warning disable S1075 // URIs should not be hardcoded
        private const string webUrlFormat = "https://www.opensmarthouse.org/zwavedatabase/{0}";
#pragma warning restore S1075 // URIs should not be hardcoded
    }
}