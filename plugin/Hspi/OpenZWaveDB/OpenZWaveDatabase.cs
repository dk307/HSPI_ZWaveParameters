using Hspi.Exceptions;
using Hspi.OpenZWaveDB.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal static class OpenZWaveDatabase
    {
        public static async Task<ZWaveInformation> ParseJson(Stream deviceJson, CancellationToken cancellationToken)
        {
            var obj = await JsonSerializer.DeserializeAsync<ZWaveInformation>(deviceJson, 
                                                                              cancellationToken: cancellationToken)
                                          .ConfigureAwait(false);
            CheckValidInformation(obj);
            return CombineParameters(obj);
        }

        public static ZWaveInformation ParseJson(string deviceJson)
        {
            var obj = JsonSerializer.Deserialize<ZWaveInformation>(deviceJson);
            CheckValidInformation(obj);
            return CombineParameters(obj);
        }

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