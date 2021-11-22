using Hspi.Exceptions;
using Hspi.OpenZWaveDB.Model;
using Hspi.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal class OfflineOpenZWaveDatabase
    {
        public OfflineOpenZWaveDatabase(IHttpQueryMaker queryMaker, string? path = null)
        {
            this.serverInterface = new OpenZWaveDatabaseOnlineInterface(queryMaker);
            this.folderDBPath = path ?? GetDBFolderPath();
        }

        public int EntriesCount => entries.Count;

        public async Task<ZWaveInformation> Create(int manufacturerId, int productType, int productId,
                                                   Version firmware, CancellationToken cancellationToken)
        {
            try
            {
                if (loadTask == null)
                {
                    throw new InvalidOperationException("Database Not loaded");
                }

                await loadTask.ConfigureAwait(false);
                string filePath = FindInEntries(manufacturerId, productType, productId, firmware);

                Log.Information("Found Specific {@file} for manufactureId:{manufactureId} productType:{productType} productId:{productId} firmware:{firmware}",
                                Path.GetFileName(filePath), manufacturerId, productType, productId, firmware);

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return await OpenZWaveDatabase.ParseJson(fileStream, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get data from Offline Open Z-Wave Database", ex);
            }
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
                using var stream = await serverInterface.GetDeviceId(deviceId, token).ConfigureAwait(false);
                using TextReader reader = new StreamReader(stream);
                string json = await reader.ReadToEndAsync().ConfigureAwait(false);
                var data = JsonSerializer.Deserialize<ZWaveInformationBasic>(json);

                if ((data == null) || (data.Deleted != "0") || (data.Approved == "0"))
                {
                    continue;
                }

                string fullPath = Path.ChangeExtension(Path.Combine(folder, data.Id!), ".json");
                await SaveFile(json, fullPath).ConfigureAwait(false);
            }
        }

        public Task StartLoadAsync(CancellationToken token)
        {
            loadTask = Task.Run(async () => await Load(token), token);
            return loadTask;
        }

        private static string GetDBFolderPath()
        {
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            string mainExeFile = process.MainModule.FileName;
            string hsDir = Path.GetDirectoryName(mainExeFile);
            return Path.Combine(hsDir, "data", PlugInData.PlugInId, "db");
        }

        private static async Task<IDictionary<Tuple<int, string>, Entry>> LoadFile(string file,
                                       CancellationToken cancellationToken)
        {
            var dict = new Dictionary<Tuple<int, string>, Entry>();
            try
            {
                using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);

                using var data = await JsonDocument.ParseAsync(fileStream,
                                                               cancellationToken: cancellationToken);

                var id = data.RootElement.GetProperty("database_id").GetInt32();
                var manufacturerId = data.RootElement.GetProperty("manufacturer").GetProperty("reference").GetInt32();
                var deviceRef = data.RootElement.GetProperty("device_ref").GetString();
                var versionMin = data.RootElement.GetProperty("version_min").Deserialize<Version>();
                var versionMax = data.RootElement.GetProperty("version_max").Deserialize<Version>();

                var deviceRefEntries = deviceRef?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var entry in deviceRefEntries ?? throw new Exception("Ref entries is null"))
                {
                    var key = new Tuple<int, string>(manufacturerId, entry.ToUpperInvariant());
                    var value = new Entry(versionMin ?? new Version(0, 0),
                                          versionMax ?? new Version(255, 255),
                                          file,
                                          id);
                    dict[key] = value;
                }
            }
            catch (Exception ex)
            {
                if (ex.IsCancelException())
                {
                    throw;
                }
                Log.Warning("Offline Database: Failed to load {file} with {error}", file, ex.GetFullMessage());
            }
            return dict;
        }

        private static async Task SaveFile(string json, string fullPath)
        {
            byte[] bytes = fileEncoding.GetBytes(json);

            using var stream = File.Open(fullPath, FileMode.OpenOrCreate, FileAccess.Write);
            await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }

        private string FindInEntries(int manufacturerId, int productType, int productId,
                                                                                     Version firmware)
        {
            string? filePath = null;
            var refDevice = string.Format(CultureInfo.InvariantCulture, "{0:X4}:{1:X4}", productType, productId);
            var key = new Tuple<int, string>(manufacturerId, refDevice);

            if (entries.TryGetValue(key, out var valueList))
            {
                foreach (var device in valueList)
                {
                    if ((firmware >= device.VersionMin) && (firmware <= device.VersionMax))
                    {
                        Log.Debug("Found Specific {@device} for manufactureId:{manufactureId} productType:{productType} productId:{productId} firmware:{firmware}",
                                     device, manufacturerId, productType, productId, firmware);
                        filePath = device.FilePath;
                        break;
                    }
                }

                if (filePath == null)
                {
                    Log.Warning("No matching firmware found for manufactureId:{manufactureId} productType:{productType} productId:{productId} firmware:{firmware}. Picking first in list",
                                manufacturerId, productType, productId, firmware);
                    filePath = valueList.FirstOrDefault().FilePath;
                }
            }

            return filePath ?? throw new ShowErrorMessageException("Device not found in the open zwave database");
        }

        private async Task Load(CancellationToken cancellationToken)
        {
            Log.Information("Loading database from {path}", folderDBPath);

            List<Task<IDictionary<Tuple<int, string>, Entry>>> tasks = new();

            foreach (var file in Directory.EnumerateFiles(folderDBPath, "*.json",
                                                          SearchOption.TopDirectoryOnly))
            {
                tasks.Add(LoadFile(file, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            var data = new Dictionary<Tuple<int, string>, ImmutableList<Entry>>();

            //collect and collapse results
            foreach (var task in tasks)
            {
                var result = task.Result;

                foreach (var pair in result)
                {
                    if (data.TryGetValue(pair.Key, out var value))
                    {
                        data[pair.Key] = value.Add(pair.Value)
                                              .Sort((x, y) => Comparer<int>.Default.Compare(x.Id, y.Id));
                    }
                    else
                    {
                        data.Add(pair.Key, new List<Entry> { pair.Value }.ToImmutableList());
                    }
                }
            }

            Interlocked.Exchange(ref entries, data.ToImmutableDictionary());
            Log.Information("Loaded database from {path} with {count} files with {deviceCount} devices",
                            folderDBPath, tasks.Count, entries.Count);
        }

        private static readonly Encoding fileEncoding = Encoding.UTF8;

        private readonly string folderDBPath;

        private readonly OpenZWaveDatabaseOnlineInterface serverInterface;

        private ImmutableDictionary<Tuple<int, string>, ImmutableList<Entry>> entries =
                                    ImmutableDictionary<Tuple<int, string>, ImmutableList<Entry>>.Empty;

        private Task? loadTask;
        private record Entry(Version VersionMin, Version VersionMax, string FilePath, int Id);
    }
}