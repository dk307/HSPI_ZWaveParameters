using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal class OpenZWaveDatabaseOnlineInterface
    {
        public OpenZWaveDatabaseOnlineInterface(IHttpQueryMaker fileCachingHttpQuery)
        {
            this.fileCachingHttpQuery = fileCachingHttpQuery;
        }

        public async Task<string> GetDeviceId(int deviceId, CancellationToken cancellationToken)
        {
            string deviceUrl = string.Format(CultureInfo.InvariantCulture, deviceUrlFormat, deviceId);
            return await fileCachingHttpQuery.GetResponseAsString(deviceUrl, cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> Search(int manufactureId,
                                         int productType,
                                         int productId,
                                         CancellationToken cancellationToken)
        {
            string listUrl = string.Format(CultureInfo.InvariantCulture, listUrlFormat, manufactureId, productType, productId);
            return await fileCachingHttpQuery.GetResponseAsString(listUrl, cancellationToken).ConfigureAwait(false);
        }

        private const string deviceUrlFormat = "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id={0}";
        private const string listUrlFormat = "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x{0:X4}%20{1:X4}:{2:X4}";
        private readonly IHttpQueryMaker fileCachingHttpQuery;
    }
}