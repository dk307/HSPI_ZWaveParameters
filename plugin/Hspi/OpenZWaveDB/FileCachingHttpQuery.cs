using Serilog;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal class FileCachingHttpQuery : IHttpQueryMaker
    {
        static FileCachingHttpQuery()
        {
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            httpClientGlobal = new HttpClient(handler, true);
        }

        public FileCachingHttpQuery(HttpClient? httpClient = null)
        {
            this.httpClient = httpClient ?? httpClientGlobal;
        }

        public async Task<string> GetResponseAsString(string url, CancellationToken cancellationToken)
        {
            Log.Information("Getting data from {url}", url);

            var result = await httpClient.GetAsync(new Uri(url, UriKind.Absolute), cancellationToken).ConfigureAwait(false);
            result.EnsureSuccessStatusCode();
            var json = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            return json;
        }

        private static readonly HttpClient httpClientGlobal;
        private readonly HttpClient httpClient;
    }
}