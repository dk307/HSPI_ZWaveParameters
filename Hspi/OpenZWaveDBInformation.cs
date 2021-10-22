using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Hspi
{
    internal record ZWaveDeviceParameterOption
    {
        public string Label;
        public int Value;
    }

    internal record ZWaveDeviceParameter
    {
        [JsonProperty("param_id")]
        public int Id;

        public string Label;
        public string Description;
        public string Overview;
        public string Units;
        public int Advanced;
        public int Size;
        public int Bitmask;
        public int Minimum;
        public int Maximum;
        public int Default;

        [JsonProperty("read_only")]
        public string ReadOnly;

        [JsonProperty("write_only")]
        public string WriteOnly;

        public IReadOnlyList<ZWaveDeviceParameterOption> Options;

        [JsonProperty]
        public string FinalDescription
        {
            get
            {
                // return the longer one
                string value = Description.Length > Overview.Length ? Description : Overview;
                return string.IsNullOrWhiteSpace(value) ? Label : value;
            }
        }
    }

    internal record DeviceManufacturer
    {
        public string Label;
    }

    internal record DeviceDocuments
    {
        public string Label;
        public string File;
    }

    internal record DeviceAssociations
    {
        public string Label;
        public string Overview;
        public string Description;
        [JsonProperty("max_nodes")]
        public int MaxNodes;
        [JsonProperty("group_id")]
        public int GroupId;
        public string Controller;
    }

    internal record ZWaveInformation
    {
        public string Inclusion;
        public string Overview;
        public string Description;
        public string Label;
        public string Exclusion;
        public DeviceManufacturer Manufacturer;
        public IReadOnlyList<ZWaveDeviceParameter> Parameters;
        public IReadOnlyList<DeviceDocuments> Documents;
        public IReadOnlyList<DeviceAssociations> Associations;
    }

    internal class OpenZWaveDBInformation
    {
        public OpenZWaveDBInformation(int manufactureId, int productType, int productId)
        {
            this.manufactureId = manufactureId;
            this.productType = productType;
            this.productId = productId;
        }

        public ZWaveInformation Data => data;

        public string FullName
        {
            get
            {
                var listName = new List<string>();

                listName.Add(data.Manufacturer.Label);
                listName.Add(data.Description);
                listName.Add("(" + data.Label + ")");

                return string.Join(" ", listName);
            }
        }
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

            data = obj;
        }

        private static async Task<string> GetCall(string deviceUrl, CancellationToken cancellationToken)
        {
            var result = await httpClient.GetAsync(deviceUrl, cancellationToken).ConfigureAwait(false);
            result.EnsureSuccessStatusCode();

            var json = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

            return json;
        }

        private const string deviceUrlFormat = "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id={0}";
        private const string listUrlFormat = "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x{0:X4}%20{1:X4}:{2:X4}";
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly int manufactureId;
        private readonly int productId;
        private readonly int productType;
        private ZWaveInformation data;
    }
}