using Serilog;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal class HttpQueryMaker : IHttpQueryMaker
    {
        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            var obj = new HttpClient(handler, true);
            return obj;
        }

        public HttpQueryMaker(HttpClient? httpClient = null)
        {
            this.httpClient = httpClient ?? httpClientGlobal;
        }

        public async Task<Stream> GetUtf8JsonResponse(string url, CancellationToken cancellationToken)
        {
            Log.Information("Getting data from {url}", url);

            var result = await httpClient.GetAsync(new Uri(url, UriKind.Absolute), cancellationToken).ConfigureAwait(false);
            result.EnsureSuccessStatusCode();
            var contentType = result.Content.Headers.ContentType;

            if (contentType.MediaType?.ToUpperInvariant() != "APPLICATION/JSON")
            {
                throw new HttpRequestException("Not a Json response");
            }

            if (contentType.CharSet?.ToUpperInvariant() != "UTF-8")
            {
                throw new HttpRequestException("Not a UTF-8 Json response");
            }

            return await result.Content.ReadAsStreamAsync().ConfigureAwait(false);
        }

        private static readonly HttpClient httpClientGlobal = CreateHttpClient();
        private readonly HttpClient httpClient;
    }
}