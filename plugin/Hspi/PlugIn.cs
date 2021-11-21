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
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

#nullable enable

namespace Hspi
{
    internal partial class PlugIn : HspiBase
    {
        public PlugIn()
            : base(PlugInData.PlugInId, PlugInData.PlugInName)
        {
        }

        public override bool SupportsConfigDeviceAll => true;

        public void DownloadZWaveDatabase()
        {
            var http = new HttpQueryMaker();
            var downloader = new OfflineOpenZWaveDatabase(http);
            downloader.Download(ShutdownCancellationToken).Wait();
        }

        public override string GetJuiDeviceConfigPage(int deviceOrFeatureRef)
        {
            try
            {
                var page = CreateDeviceConfigPage(deviceOrFeatureRef);
                Task.Run(() => page.BuildConfigPage(ShutdownCancellationToken)).Wait();
                cacheForUpdate[deviceOrFeatureRef] = page;
                return page?.GetPage()?.ToJsonString() ?? throw new Exception("Page is unexpectely null");
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
            Func<ZWaveData, Task<ZWaveInformation>> factoryForOpenZWaveDBInformation = (zwaveData) =>
               {
                   return OnlineOpenZWaveDatabase.Create(zwaveData.ManufactureId, zwaveData.ProductType,
                                                             zwaveData.ProductId, zwaveData.Firmware,
                                                             new HttpQueryMaker(), ShutdownCancellationToken);
               };

            return new DeviceConfigPage(deviceOrFeatureRef, CreateZWaveConnection(),
                                        factoryForOpenZWaveDBInformation);
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
            if (settingsPages != null && settingsPages.OnSettingChange(changedView))
            {
                UpdateDebugLevel();
                return true;
            }

            return base.OnSettingChange(pageId, currentView, changedView);
        }

        protected override void OnShutdown()
        {
            Log.Information("Shutting down");
            base.OnShutdown();
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
                        int value = Task.Run(() => connection.GetConfiguration(homeId!, nodeId.Value, parameter.Value, ShutdownCancellationToken)).Result;

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
            if (settingsPages != null)
            {
                this.LogDebug = settingsPages.DebugLoggingEnabled;
                Logger.ConfigureLogging(LogDebug, settingsPages.LogtoFileEnabled, HomeSeerSystem);
            }
        }

        internal struct ZWaveParameterGetResult
        {
            public string? ErrorMessage { get; init; }
            public int? Value { get; init; }
        }

        private const string DeviceConfigPageOperation = "GET";
        private const string HTMLEndline = "<BR>";
        private readonly IDictionary<int, IDeviceConfigPage> cacheForUpdate = new ConcurrentDictionary<int, IDeviceConfigPage>();
        private SettingsPages? settingsPages;
    }
}