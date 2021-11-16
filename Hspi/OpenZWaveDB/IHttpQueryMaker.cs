using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Hspi.OpenZWaveDB
{
    public interface IHttpQueryMaker
    {
        Task<string> GetResponseAsString(string url, CancellationToken cancellationToken);
    }
}