using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal sealed class OpenZWaveDatabaseOnlineInterface
    {
        public OpenZWaveDatabaseOnlineInterface(IHttpQueryMaker queryMaker)
        {
            this.queryMaker = queryMaker;
        }

        public async Task<Stream> GetDeviceId(int deviceId, CancellationToken cancellationToken)
        {
            string deviceUrl = string.Format(CultureInfo.InvariantCulture, deviceUrlFormat, deviceId);
            return await queryMaker.GetUtf8JsonResponse(deviceUrl, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Stream> Search(int manufactureId,
                                         int productType,
                                         int productId,
                                         CancellationToken cancellationToken)
        {
            string listUrl = string.Format(CultureInfo.InvariantCulture, listUrlFormat, manufactureId, productType, productId);
            return await queryMaker.GetUtf8JsonResponse(listUrl, cancellationToken).ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "Used as format string")]
        private const string deviceUrlFormat = "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id={0}";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "Used as format string")]
        private const string listUrlFormat = "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x{0:X4}%20{1:X4}:{2:X4}";

        private readonly IHttpQueryMaker queryMaker;
    }
}