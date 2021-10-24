using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static System.FormattableString;

#nullable enable

namespace Hspi
{
#pragma warning disable 0649

    internal record ZWaveDeviceParameterOption
    {
        public string? Label { get; init; }
        public int Value { get; init; }

        [JsonIgnore]
        public string Description => Invariant($"{Value} - {Label}");
    }

    internal record ZWaveDeviceParameter
    {
        [JsonProperty("param_id")]
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
        public string? ReadOnly { get; init; }

        [JsonProperty("write_only")]
        public string? WriteOnly { get; init; }

        public IReadOnlyList<ZWaveDeviceParameterOption>? Options { get; init; }

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
        public bool HasMultipleValues { get; init; } = false;

        [JsonIgnore]
        public bool HasOptions => Options != null && Options.Count > 0 && !HasMultipleValues;
    }

    internal record DeviceManufacturer
    {
        public string? Label { get; init; }
    }

    internal record DeviceDocuments
    {
        public string? Label;
        public string? File;
    }

    internal record DeviceAssociations
    {
        public string? Label;
        public string? Overview;
        public string? Description;
        [JsonProperty("max_nodes")]
        public int MaxNodes;
        [JsonProperty("group_id")]
        public int GroupId;
        public string? Controller;
    }

    internal record ZWaveInformation
    {
        [JsonProperty("database_id")]
        public string? Id { get; init; }
        public string? Overview { get; init; }
        public string? Description { get; init; }
        public string? Label { get; init; }
        public DeviceManufacturer? Manufacturer { get; init; }
        public IReadOnlyList<ZWaveDeviceParameter>? Parameters { get; init; }
        public IReadOnlyList<DeviceDocuments>? Documents { get; init; }
        public IReadOnlyList<DeviceAssociations>? Associations { get; init; }

        [JsonIgnore]
        public string FullName
        {
            get
            {
                var listName = new List<string?>();
                listName.Add(Manufacturer?.Label);
                listName.Add(Description);
                listName.Add("(" + Label + ")");

                return string.Join(" ", listName.Where(s => !string.IsNullOrEmpty(s)));
            }
        }

        [JsonIgnore]
        public Uri WebUrl => new Uri(string.Format(webUrlFormat, Id), UriKind.Absolute);

        private const string webUrlFormat = "https://www.opensmarthouse.org/zwavedatabase/{0}";
    }

#pragma warning restore 0649

    internal class OpenZWaveDBInformation
    {
        public OpenZWaveDBInformation(int manufactureId, int productType, int productId)
        {
            this.manufactureId = manufactureId;
            this.productType = productType;
            this.productId = productId;
        }

        public ZWaveInformation? Data => data;

        public async Task Update(CancellationToken cancellationToken)
        {
            string listUrl = string.Format(listUrlFormat, manufactureId, productType, productId);
            var listJson = await GetCall(listUrl, cancellationToken).ConfigureAwait(false);

            var jobject = JObject.Parse(listJson);
            var id = jobject?["devices"]?.First?["id"]?.ToObject<int>() ?? throw new Exception("Device not found in z-wave database");

            string deviceUrl = string.Format(deviceUrlFormat, id);
            var deviceJson = await GetCall(deviceUrl, cancellationToken).ConfigureAwait(false);

            JsonSerializer serializer = new JsonSerializer();

            using StringReader stringReader = new StringReader(deviceJson);
            using JsonTextReader reader = new JsonTextReader(stringReader);
            var obj = serializer.Deserialize<ZWaveInformation>(reader);
            if (obj == null)
            {
                throw new Exception("Json invalid from database");
            }

            // process parameters
            var map = obj.Parameters.GroupBy(x => x.Id);
            var finalParameters = new List<ZWaveDeviceParameter>();

            foreach (var group in map)
            {
                if (group.Count() == 1)
                {
                    finalParameters.Add(group.First());
                }
                else
                {
                    var result = group.First();

                    List<string> extra = new List<string>();
                    foreach (var item in group)
                    {
                        if (item == result)
                        {
                            continue;
                        }
                        extra.Add(
                            string.Format(CultureInfo.InvariantCulture, "Bitmask:{0:x}: {1}", item.Bitmask, item.Label));
                    }
                    string extraDescription = NewLine + string.Join(NewLine, extra);

                    string overview = result.Overview ?? string.Empty;
                    string description = result.Description ?? string.Empty;

                    overview += extraDescription;
                    description += extraDescription;

                    finalParameters.Add(result with
                    {
                        Overview = overview,
                        Description = description,
                        HasMultipleValues = true
                    });
                }
            }
            data = obj with { Parameters = finalParameters.AsReadOnly() };
        }

        private static async Task<string> GetCall(string deviceUrl, CancellationToken cancellationToken)
        {
            logger.Info("Getting data from " + deviceUrl);
            var result = await httpClient.GetAsync(new Uri(deviceUrl, UriKind.Absolute), cancellationToken).ConfigureAwait(false);
            result.EnsureSuccessStatusCode();

            var json = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

            return json;
        }

        private const string deviceUrlFormat = "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id={0}";
        private const string listUrlFormat = "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x{0:X4}%20{1:X4}:{2:X4}";
        private const string NewLine = "<BR>";
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly int manufactureId;
        private readonly int productId;
        private readonly int productType;
        private ZWaveInformation? data;
    }
}