using Hspi.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal class OpenZWaveDBInformation
    {
        public OpenZWaveDBInformation(int manufactureId, int productType, int productId, Version firmware,
                                      IHttpQueryMaker fileCachingHttpQuery)
        {
            this.manufactureId = manufactureId;
            this.productType = productType;
            this.productId = productId;
            this.firmware = firmware;
            this.fileCachingHttpQuery = fileCachingHttpQuery;
        }

        public ZWaveInformation? Data => data;

        public static ZWaveInformation ParseJson(string deviceJson)
        {
            var serializer = new JsonSerializer();
            using var stringReader = new StringReader(deviceJson);
            using var reader = new JsonTextReader(stringReader);
            var obj = serializer.Deserialize<ZWaveInformation>(reader);
            if ((obj == null) || string.IsNullOrWhiteSpace(obj.Id))
            {
                throw new ShowErrorMessageException("Invalid Json from database");
            }
            return obj;
        }

        public async Task Update(CancellationToken cancellationToken)
        {
            try
            {
                var id = await GetDeviceId(cancellationToken).ConfigureAwait(false);

                string deviceUrl = string.Format(deviceUrlFormat, id);
                var deviceJson = await fileCachingHttpQuery.GetResponseAsString(deviceUrl, cancellationToken).ConfigureAwait(false);

                var obj = ParseJson(deviceJson);

                var finalParameters = new List<ZWaveDeviceParameter>();
                if (obj.Parameters != null)
                {
                    // process parameters
                    var map = obj.Parameters.GroupBy(x => x.ParameterId);

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
                }

                data = obj with { Parameters = finalParameters.AsReadOnly() };
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get Data from Open Z-Wave DB", ex);
            }
        }

        private async Task<int> GetDeviceId(CancellationToken cancellationToken)
        {
            string listUrl = string.Format(listUrlFormat, manufactureId, productType, productId);
            var listJson = await fileCachingHttpQuery.GetResponseAsString(listUrl, cancellationToken).ConfigureAwait(false);

            var jobject = JObject.Parse(listJson);
            var devices = jobject?["devices"]?.ToObject<ZWaveDevice[]>();

            if (devices == null || devices.Length == 0)
            {
                throw new ShowErrorMessageException("Device not found in z-wave database");
            }

            Log.Debug("Found {count} devices for manufactureId:{manufactureId} productType:{productType} productId:{productId}",
                            devices.Length, manufactureId, productType, productId);

            int? id = null;
            foreach (var device in devices)
            {
                if ((device.VersionMin != null) && (device.VersionMax != null))
                {
                    if ((firmware >= device.VersionMin) && (firmware <= device.VersionMax))
                    {
                        Log.Debug("Found Specific {@device} for manufactureId:{manufactureId} productType:{productType} productId:{productId} firmware:{firmware}",
                                     device, manufactureId, productType, productId, firmware);
                        id = device.Id;
                        break;
                    }
                }
            }

            if (id == null)
            {
                Log.Warning("No matching firmware found for manufactureId:{manufactureId} productType:{productType} productId:{productId} firmware:{firmware}. Picking first in list",
                            manufactureId, productType, productId, firmware);
                id = devices.First().Id;
            }

            return id ?? throw new ShowErrorMessageException("Device not found in the open zwave database");
        }

        private const string deviceUrlFormat = "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id={0}";
        private const string listUrlFormat = "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x{0:X4}%20{1:X4}:{2:X4}";
        private readonly IHttpQueryMaker fileCachingHttpQuery;
        private readonly Version firmware;
        private readonly int manufactureId;
        private readonly int productId;
        private readonly int productType;
        private ZWaveInformation? data;
    }
}