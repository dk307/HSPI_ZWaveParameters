using Hspi.Exceptions;
using Serilog;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal class OnlineOpenZWaveDBInformation : OpenZWaveDBInformation
    {
        public OnlineOpenZWaveDBInformation(int manufactureId, int productType, int productId, Version firmware,
                                            IHttpQueryMaker fileCachingHttpQuery)
            : base(manufactureId, productType, productId, firmware)
        {
            this.serverInterface = new OpenZWaveDatabaseOnlineInterface(fileCachingHttpQuery);
        }

        protected override async Task<string> GetDeviceJson(CancellationToken cancellationToken)
        {
            var id = await GetDeviceId(cancellationToken).ConfigureAwait(false);
            var deviceJson = await serverInterface.GetDeviceId(id, cancellationToken).ConfigureAwait(false);
            return deviceJson;
        }

        private async Task<int> GetDeviceId(CancellationToken cancellationToken)
        {
            var listJson = await serverInterface.Search(ManufactureId, ProductType, ProductId, cancellationToken).ConfigureAwait(false);

            var jobject = JsonNode.Parse(listJson);
            var devices = JsonSerializer.Deserialize<ZWaveDevice[]>(jobject?["devices"]);

            if (devices == null || devices.Length == 0)
            {
                throw new ShowErrorMessageException("Device not found in z-wave database");
            }

            Log.Debug("Found {count} devices for manufactureId:{manufactureId} productType:{productType} productId:{productId}",
                      devices.Length, ManufactureId, ProductType, ProductId);

            int? id = null;
            foreach (var device in devices)
            {
                if ((device.VersionMin != null) && (device.VersionMax != null))
                {
                    if ((Firmware >= device.VersionMin) && (Firmware <= device.VersionMax))
                    {
                        Log.Debug("Found Specific {@device} for manufactureId:{manufactureId} productType:{productType} productId:{productId} firmware:{firmware}",
                                     device, ManufactureId, ProductType, ProductId, Firmware);
                        id = device.Id;
                        break;
                    }
                }
            }

            if (id == null)
            {
                Log.Warning("No matching firmware found for manufactureId:{manufactureId} productType:{productType} productId:{productId} firmware:{firmware}. Picking first in list",
                            ManufactureId, ProductType, ProductId, Firmware);
                id = devices.First().Id;
            }

            return id ?? throw new ShowErrorMessageException("Device not found in the open zwave database");
        }

        private readonly OpenZWaveDatabaseOnlineInterface serverInterface;
    }
}