using Hspi.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal abstract class OpenZWaveDBInformation
    {
        public OpenZWaveDBInformation(int manufactureId, int productType,
                                      int productId, Version firmware)
        {
            this.ManufactureId = manufactureId;
            this.ProductType = productType;
            this.ProductId = productId;
            this.Firmware = firmware;
        }

        public static ZWaveInformation ParseJson(string deviceJson)
        {
            var obj = JsonSerializer.Deserialize<ZWaveInformation>(deviceJson);
            if (obj == null)
            {
                throw new ShowErrorMessageException("Invalid Json from database");
            }

            if (obj.Deleted != 0 || obj.Approved == 0)
            {
                throw new ShowErrorMessageException("Non-Approved Or Deleted Device from database");
            }

            return obj;
        }

        public ZWaveInformation? Data { get; private set; }
        public Version Firmware { get; init; }
        public int ManufactureId { get; init; }
        public int ProductId { get; init; }
        public int ProductType { get; init; }

        protected abstract Task<string> GetDeviceJson(CancellationToken token);

        public async Task Update(CancellationToken cancellationToken)
        {
            try
            {
                var deviceJson = await GetDeviceJson(cancellationToken).ConfigureAwait(false);

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

                Data = obj with { Parameters = finalParameters.AsReadOnly() };
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get data from Open Z-Wave Database", ex);
            }
        }
    }
}