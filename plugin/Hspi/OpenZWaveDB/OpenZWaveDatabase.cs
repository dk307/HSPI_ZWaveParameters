using Hspi.Exceptions;
using Hspi.OpenZWaveDB.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal abstract class OpenZWaveDatabase
    {
        public OpenZWaveDatabase(int manufactureId, int productType,
                                      int productId, Version firmware)
        {
            this.ManufactureId = manufactureId;
            this.ProductType = productType;
            this.ProductId = productId;
            this.Firmware = firmware;
        }

        public Version Firmware { get; init; }

        public int ManufactureId { get; init; }

        public int ProductId { get; init; }

        public int ProductType { get; init; }

        public static async Task<ZWaveInformation> ParseJson(Stream deviceJson)
        {
            var obj = await JsonSerializer.DeserializeAsync<ZWaveInformation>(deviceJson).ConfigureAwait(false);
            CheckValidInformation(obj);
            return CombineParameters(obj);
        }

        public static ZWaveInformation ParseJson(string deviceJson)
        {
            var obj = JsonSerializer.Deserialize<ZWaveInformation>(deviceJson);
            CheckValidInformation(obj);
            return CombineParameters(obj);
        }

        public async Task<ZWaveInformation> Create(CancellationToken cancellationToken)
        {
            try
            {
                var stream = await GetDeviceJson(cancellationToken).ConfigureAwait(false);
                return await ParseJson(stream).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get data from Open Z-Wave Database", ex);
            }
        }

        protected abstract Task<Stream> GetDeviceJson(CancellationToken token);

        private static void CheckValidInformation(ZWaveInformation? obj)
        {
            if (obj == null)
            {
                throw new ShowErrorMessageException("Invalid Json from database");
            }

            if (obj.Deleted != 0 || obj.Approved == 0)
            {
                throw new ShowErrorMessageException("Non-Approved Or Deleted Device from database");
            }
        }

        private static ZWaveInformation CombineParameters(ZWaveInformation obj)
        {
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

                return obj with { Parameters = finalParameters.AsReadOnly() };
            }

            return obj;
        }
    }
}