using Hspi.Utils;
using MonkeyCache;
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

        public FileCachingHttpQuery(HttpClient? httpClient = null, string? cachePath = null)
        {
            cachePath ??= GetCachePath();
            barrel = MonkeyCache.FileStore.Barrel.Create(cachePath);
            this.httpClient = httpClient ?? httpClientGlobal;
        }

        public async Task<string> GetResponseAsString(string url, CancellationToken cancellationToken)
        {
            Log.Information("Getting data from {url}", url);

            try
            {
                var result = await httpClient.GetAsync(new Uri(url, UriKind.Absolute), cancellationToken).ConfigureAwait(false);
                result.EnsureSuccessStatusCode();
                var json = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                barrel.Add(url, json, TimeSpan.MaxValue);
                return json;
            }
            catch (Exception ex)
            {
                if (barrel.Exists(url))
                {
                    Log.Information("Failed to get from {url} with {error}. Using cached values.", url, ex.GetFullMessage());
                    return barrel.Get<string>(url);
                }
                throw;
            }
        }

        private static string GetCachePath()
        {
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            string mainExeFile = process.MainModule.FileName;
            string hsDir = Path.GetDirectoryName(mainExeFile);
            return Path.Combine(hsDir, "data", PlugInData.PlugInId, "cache");
        }

        private static readonly HttpClient httpClientGlobal;
        private readonly IBarrel barrel;
        private readonly HttpClient httpClient;
    }
}