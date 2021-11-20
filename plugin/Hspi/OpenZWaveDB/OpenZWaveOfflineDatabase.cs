using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal class OpenZWaveOfflineDatabase
    {
        public OpenZWaveOfflineDatabase(IHttpQueryMaker fileCachingHttpQuery, string? path = null)
        {
            this.serverInterface = new OpenZWaveDatabaseOnlineInterface(fileCachingHttpQuery);
            this.folderDBPath = path ?? GetDBFolderPath();
        }

        public async Task Download(CancellationToken token)
        {
            var folder = GetDBFolderPath();
            Directory.CreateDirectory(folder);

            int deviceId = 0;
            while (deviceId < 1500) // 1500 based on current upper limit
            {
                await Task.Delay(100, token).ConfigureAwait(false); // wait to avoid RateLimit
                deviceId++;
                string json = await serverInterface.GetDeviceId(deviceId, token).ConfigureAwait(false);
                var data = JsonSerializer.Deserialize<ZWaveInformationBasic>(json);

                if ((data == null) || (data.Deleted != "0") || (data.Approved == "0"))
                {
                    continue;
                }

                string fullPath = Path.ChangeExtension(Path.Combine(folder, data.Id!), ".json");
                await SaveFile(json, fullPath).ConfigureAwait(false);
            }
        }

        //public async Task Load(CancellationToken cancellationToken)
        //{
        //    JsonSerializer serializer = new JsonSerializer();

        //    foreach (var file in Directory.EnumerateFiles(folderDBPath, "*.json"))
        //    {
        //        using var streamReader = new StreamReader(file, fileEncoding);

        //        //var stream = Reader.ReadToEndAsync().ConfigureAwait(false);

        //        using JsonReader reader = new JsonTextReader(streamReader);

        //        // reader
        //    }
        //}

        private static string GetDBFolderPath()
        {
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            string mainExeFile = process.MainModule.FileName;
            string hsDir = Path.GetDirectoryName(mainExeFile);
            return Path.Combine(hsDir, "data", PlugInData.PlugInId, "db");
        }

        private static async Task SaveFile(string json, string fullPath)
        {
            byte[] bytes = fileEncoding.GetBytes(json);

            using var stream = File.Open(fullPath, FileMode.OpenOrCreate, FileAccess.Write);
            await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }

        private static readonly Encoding fileEncoding = Encoding.UTF8;

        private readonly string folderDBPath;
        private readonly OpenZWaveDatabaseOnlineInterface serverInterface;
    }
}