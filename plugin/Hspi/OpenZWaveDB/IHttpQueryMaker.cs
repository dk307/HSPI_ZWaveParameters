using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Hspi.OpenZWaveDB
{
    public interface IHttpQueryMaker
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "Converted to url later")]
        Task<Stream> GetUtf8JsonResponse(string url, CancellationToken cancellationToken);
    }
}