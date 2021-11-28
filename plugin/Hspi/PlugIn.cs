using HomeSeer.Jui.Views;
using HomeSeer.PluginSdk;
using Hspi.Exceptions;
using Hspi.OpenZWaveDB;
using Hspi.OpenZWaveDB.Model;
using Hspi.Utils;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

#nullable enable

namespace Hspi
{
    internal class PlugIn : HspiBase
    {
        public PlugIn()
            : base(PlugInData.PlugInId, PlugInData.PlugInName)
        {
        }

        public override bool SupportsConfigDeviceAll => true;

        public void DownloadZWaveDatabase()
        {
            CheckNotNull(offlineOpenZWaveDatabase);

            OfflineOpenZWaveDatabase.Download(CreateHttpQueryMaker(),
                                              OfflineOpenZWaveDatabase.GetDefaultDatabaseFolderPath(),
                                              token: ShutdownCancellationToken).Wait();
        }

        public override string GetJuiDeviceConfigPage(int deviceOrFeatureRef)
        {
            try
            {
                Log.Debug("Asking for page for {deviceOrFeatureRef}", deviceOrFeatureRef);
                var page = CreateDeviceConfigPage(deviceOrFeatureRef);
                Task.Run(() => page.BuildConfigPage(ShutdownCancellationToken)).Wait();
                cacheForUpdate[deviceOrFeatureRef] = page;
                var devicePage = page?.GetPage()?.ToJsonString() ?? throw new InvalidOperationException("Page is unexpectedly null");
                Log.Debug("Returning page for {deviceOrFeatureRef}", deviceOrFeatureRef);
                return devicePage;
            }
            catch (Exception ex)
            {
                var page = PageFactory.CreateDeviceConfigPage(PlugInData.PlugInId, "Z-Wave Information");
                page = page.WithView(new LabelView("exception", string.Empty, ex.GetFullMessage())
                {
                    LabelType = HomeSeer.Jui.Types.ELabelType.Preformatted
                });
                return page.Page.ToJsonString();
            }
        }

        public override bool HasJuiDeviceConfigPage(int devOrFeatRef)
        {
            var connection = CreateZWaveConnection();
            return connection.IsZwaveDevice(devOrFeatRef);
        }

        public override string PostBackProc(string page, string data, string user, int userRights)
        {
            if (page == "Update")
            {
                return HandleDeviceConfigPostBackProc(data);
            }
            return base.PostBackProc(page, data, user, userRights);
        }

        protected override void BeforeReturnStatus()
        {
            this.Status = PluginStatus.Ok();
        }

        protected virtual IDeviceConfigPage CreateDeviceConfigPage(int deviceOrFeatureRef)
        {
            CheckNotNull(settingsPages);

            Func<ZWaveData, Task<ZWaveInformation>> factoryForOpenZWaveDatabase;

            if (settingsPages.PreferOnlineDatabase)
            {
                factoryForOpenZWaveDatabase = (zwaveData) =>
                             OnlineOpenZWaveDatabase.Create(zwaveData.ManufactureId, zwaveData.ProductType,
                                                            zwaveData.ProductId, zwaveData.Firmware,
                                                            CreateHttpQueryMaker(), ShutdownCancellationToken);
            }
            else
            {
                CheckNotNull(offlineOpenZWaveDatabase);

                factoryForOpenZWaveDatabase = (zwaveData) =>
                    offlineOpenZWaveDatabase.Create(zwaveData.ManufactureId, zwaveData.ProductType,
                                                    zwaveData.ProductId, zwaveData.Firmware, ShutdownCancellationToken);
            }

            return new DeviceConfigPage(deviceOrFeatureRef, CreateZWaveConnection(),
                                        factoryForOpenZWaveDatabase);
        }

        protected virtual IHttpQueryMaker CreateHttpQueryMaker()
        {
            return new HttpQueryMaker();
        }

        protected virtual OfflineOpenZWaveDatabase CreateOfflineOpenDBOfflineDatabase()
        {
            return new(OfflineOpenZWaveDatabase.GetDefaultDatabaseFolderPath());
        }

        protected virtual IZWaveConnection CreateZWaveConnection()
        {
            return new ZWaveConnection(HomeSeerSystem);
        }

        protected override void Initialize()
        {
            try
            {
                Log.Information("Plugin Starting");
                Settings.Add(SettingsPages.CreateDefault());
                LoadSettingsFromIni();
                settingsPages = new SettingsPages(Settings);
                UpdateDebugLevel();
                offlineOpenZWaveDatabase = CreateOfflineOpenDBOfflineDatabase();
                offlineOpenZWaveDatabase.StartLoadAsync(ShutdownCancellationToken);

                Log.Information("Plugin Started");
            }
            catch (Exception ex)
            {
                Log.Error("Failed to initialize PlugIn with {error}", ex.GetFullMessage());
                throw;
            }
        }

        protected override bool OnDeviceConfigChange(Page deviceConfigPage, int devOrFeatRef)
        {
            try
            {
                Log.Debug("OnDeviceConfigChange for devOrFeatRef:{devOrFeatRef}", devOrFeatRef);

                if (cacheForUpdate.TryGetValue(devOrFeatRef, out var page))
                {
                    page.OnDeviceConfigChange(deviceConfigPage);
                }
                else
                {
                    throw new ShowErrorMessageException("Plugin was restarted after page was created. Please refresh.");
                }

                return true;
            }
            catch (Exception ex)
            {
                // This needs to Exception class to show error message
                string errorMessage = ex.GetFullMessage();
                Log.Error("Failed to process OnDeviceConfigChange for devOrFeatRef:{devOrFeatRef} with error {error}", devOrFeatRef, errorMessage);
                throw new Exception(errorMessage);
            }
        }

        protected override bool OnSettingChange(string pageId, AbstractView currentView, AbstractView changedView)
        {
            Log.Information("Page:{pageId} has changed value of id:{id} to {value}", pageId, changedView.Id, changedView.GetStringValue());

            CheckNotNull(settingsPages);

            if (settingsPages.OnSettingChange(changedView))
            {
                UpdateDebugLevel();
                if (changedView.Id == SettingsPages.PreferOnlineDatabaseId)
                {
                    cacheForUpdate.Clear();
                }
                return true;
            }

            return base.OnSettingChange(pageId, currentView, changedView);
        }

        protected override void OnShutdown()
        {
            Log.Information("Shutting down");
            base.OnShutdown();
        }

        private static void CheckNotNull([NotNull] object? obj)
        {
            if (obj is null)
            {
                throw new InvalidOperationException("Plugin Not Initialized");
            }
        }
        private string HandleDeviceConfigPostBackProc(string data)
        {
            try
            {
                var input = JsonNode.Parse(data);

                if (input != null)
                {
                    if (((string?)input["operation"]) == DeviceConfigPageOperation)
                    {
                        var homeId = input["homeId"]?.ToString();
                        var nodeId = (byte?)input["nodeId"];
                        var parameter = (byte?)input["parameter"];

                        if (string.IsNullOrWhiteSpace(homeId) || !nodeId.HasValue || !parameter.HasValue)
                        {
                            throw new ArgumentException("Input not valid");
                        }

                        var connection = CreateZWaveConnection();
                        int value = Task.Run(() => connection.GetConfiguration(homeId, nodeId.Value, parameter.Value, ShutdownCancellationToken)).Result;

                        return JsonSerializer.Serialize(new ZWaveParameterGetResult()
                        {
                            Value = value
                        });
                    }
                }
                throw new ArgumentException("Unknown operation");
            }
            catch (Exception ex)
            {
                Log.Error("Failed to process PostBackProc for Update with {data} with {error}", data, ex.GetFullMessage());
                return JsonSerializer.Serialize(new ZWaveParameterGetResult()
                {
                    ErrorMessage = ex.GetFullMessage(HTMLEndline)
                });
            }
        }

        private void UpdateDebugLevel()
        {
            CheckNotNull(settingsPages);

            this.LogDebug = settingsPages.DebugLoggingEnabled;
            Logger.ConfigureLogging(LogDebug, settingsPages.LogtoFileEnabled, HomeSeerSystem);
        }

        internal record ZWaveParameterGetResult(string? ErrorMessage = null, int? Value = null);

        private const string DeviceConfigPageOperation = "GET";
        private const string HTMLEndline = "<BR>";
        private readonly IDictionary<int, IDeviceConfigPage> cacheForUpdate = new ConcurrentDictionary<int, IDeviceConfigPage>();
        private OfflineOpenZWaveDatabase? offlineOpenZWaveDatabase;
        private SettingsPages? settingsPages;
    }
}