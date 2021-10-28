using Newtonsoft.Json;
using static System.FormattableString;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal record ZWaveDeviceParameterOption
    {
        public string? Label { get; init; }
        public int Value { get; init; }

        [JsonIgnore]
        public string Description => Invariant($"{Value} - {Label}");
    }
}