using MonkeyCache;
using System;
using System.IO;
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
            using var process = System.Diagnostics.Process.GetCurrentProcess();

            string mainExeFile = process.MainModule.FileName;
            string hsDir = Path.GetDirectoryName(mainExeFile);
            string cachePath = Path.Combine(hsDir, "data", PlugInData.PlugInId, "cache");

            barrel = MonkeyCache.FileStore.Barrel.Create(cachePath);
            this.httpClient = httpClient ?? httpClientGlobal;
        }

        public async Task<string> GetResponseAsString(string url, CancellationToken cancellationToken)
        {
            logger.Info("Getting data from " + url);
            if (barrel.Exists(url))
            {
                _ = Task.Run(async () => await UpdateCache(url, cancellationToken), cancellationToken);
                return barrel.Get<string>(url);
            }

            string json = await GetResponseString(url, cancellationToken).ConfigureAwait(false);
            return json;

            async Task<string> GetResponseString(string url, CancellationToken cancellationToken)
            {
                var result = await httpClient.GetAsync(new Uri(url, UriKind.Absolute), cancellationToken).ConfigureAwait(false);
                result.EnsureSuccessStatusCode();
                var json = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                barrel.Add(url, json, TimeSpan.MaxValue);
                return json;
            }

            async Task UpdateCache(string url, CancellationToken cancellationToken)
            {
                var result = await httpClient.GetAsync(new Uri(url, UriKind.Absolute), cancellationToken).ConfigureAwait(false);
                if (result.IsSuccessStatusCode)
                {
                    var json = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                    barrel.Add(url, json, TimeSpan.MaxValue);
                }
            }
        }

        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IBarrel barrel;
        private readonly HttpClient httpClient;
        private static readonly HttpClient httpClientGlobal;
    }
}