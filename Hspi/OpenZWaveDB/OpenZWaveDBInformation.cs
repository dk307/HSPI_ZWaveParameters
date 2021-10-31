using Hspi.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static System.FormattableString;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal class OpenZWaveDBInformation
    {
        public OpenZWaveDBInformation(int manufactureId, int productType, int productId, Version firmware,
                                      HttpClient? httpClient = null)
        {
            this.manufactureId = manufactureId;
            this.productType = productType;
            this.productId = productId;
            this.firmware = firmware;
            this.httpClient = httpClient ?? httpClientGlobal;
        }

        public ZWaveInformation? Data => data;

        public async Task Update(CancellationToken cancellationToken)
        {
            try
            {
                var id = await GetDeviceId(cancellationToken).ConfigureAwait(false);

                string deviceUrl = string.Format(deviceUrlFormat, id);
                var deviceJson = await GetCall(deviceUrl, cancellationToken).ConfigureAwait(false);

                ZWaveInformation obj = ParseJson(deviceJson);

                // process parameters
                var map = obj.Parameters.GroupBy(x => x.ParameterId);

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

                        finalParameters.Add(result with
                        {
                            SubParameters = group.Skip(1).ToList().AsReadOnly(),
                        });
                    }
                }

                data = obj with { Parameters = finalParameters.AsReadOnly() };
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get Data from Open Z-Wave DB", ex);
            }
        }

        public static ZWaveInformation ParseJson(string deviceJson)
        {
            var serializer = new JsonSerializer();
            using var stringReader = new StringReader(deviceJson);
            using var reader = new JsonTextReader(stringReader);
            var obj = serializer.Deserialize<ZWaveInformation>(reader);
            if ((obj == null) || string.IsNullOrWhiteSpace(obj.Id))
            {
                throw new ShowErrorMessageException("Json invalid from database");
            }
            return obj;
        }

        private async Task<string> GetCall(string deviceUrl, CancellationToken cancellationToken)
        {
            logger.Info("Getting data from " + deviceUrl);
            var result = await httpClient.GetAsync(new Uri(deviceUrl, UriKind.Absolute), cancellationToken).ConfigureAwait(false);
            result.EnsureSuccessStatusCode();

            var json = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            return json;
        }

        private async Task<int> GetDeviceId(CancellationToken cancellationToken)
        {
            string listUrl = string.Format(listUrlFormat, manufactureId, productType, productId);
            var listJson = await GetCall(listUrl, cancellationToken).ConfigureAwait(false);

            var jobject = JObject.Parse(listJson);
            var devices = jobject?["devices"]?.ToObject<ZWaveDevice[]>();

            if (devices == null || devices.Length == 0)
            {
               throw new ShowErrorMessageException("Device not found in z-wave database");
            }

            logger.Debug(Invariant($"Found {devices.Length} devices for manufactureId:{manufactureId} productType:{productType} productId:{productId}"));

            int? id = null;
            foreach (var device in devices)
            {
                if ((device.VersionMin != null) && (device.VersionMax != null))
                {
                    if ((firmware >= device.VersionMin) && (firmware <= device.VersionMax))
                    {
                        logger.Debug(Invariant($"Found Specific device {device.Id} for manufactureId:{manufactureId} productType:{productType} productId:{productId} firmware:{firmware}"));
                        id = device.Id;
                        break;
                    }
                }
            }

            if (id == null)
            {
                logger.Warn(Invariant($"No matching firmware found for manufactureId:{manufactureId} productType:{productType} productId:{productId} firmware:{firmware}. Picking first in list"));
                id = devices.First().Id;
            }

            return id ?? throw new ShowErrorMessageException("Device not found in the open zwave database");
        }

        private const string deviceUrlFormat = "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id={0}";
        private const string listUrlFormat = "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x{0:X4}%20{1:X4}:{2:X4}";
        private static readonly HttpClient httpClientGlobal = new();
        private readonly HttpClient httpClient;
        private readonly static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly Version firmware;
        private readonly int manufactureId;
        private readonly int productId;
        private readonly int productType;
        private ZWaveInformation? data;
    }
}