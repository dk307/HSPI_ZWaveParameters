using HomeSeer.Jui.Views;
using HomeSeer.PluginSdk;
using Hspi.Exceptions;
using Hspi.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using static System.FormattableString;

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

        public override string GetJuiDeviceConfigPage(int deviceOrFeatureRef)
        {
            try
            {
                var page = CreateDeviceConfigPage(deviceOrFeatureRef);
                page.BuildConfigPage(CancellationToken.None).ResultForSync();
                cacheForUpdate[deviceOrFeatureRef] = page;
                return page.GetPage().ToJsonString();
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
            return new DeviceConfigPage(CreateZWaveConnection(), deviceOrFeatureRef);
        }

        protected virtual IZWaveConnection CreateZWaveConnection()
        {
            return new ZWaveConnection(HomeSeerSystem);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }

        protected override void Initialize()
        {
            try
            {
                pluginConfig = new PluginConfig(HomeSeerSystem);
                UpdateDebugLevel();

                logger.Info("Plugin Started");
            }
            catch (Exception ex)
            {
                string result = Invariant($"Failed to initialize PlugIn with {ex.GetFullMessage()}");
                logger.Error(result);
                throw;
            }
        }

        protected override bool OnDeviceConfigChange(Page deviceConfigPage, int devOrFeatRef)
        {
            try
            {
                logger.Debug(Invariant($"OnDeviceConfigChange for {devOrFeatRef}"));

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
            catch (ShowErrorMessageException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(Invariant($"Failed to process OnDeviceConfigChange for {devOrFeatRef} with error {ex.GetFullMessage()}"));
                string errorMessage = ex.GetFullMessage();
                throw new ShowErrorMessageException(errorMessage, ex);
            }
        }

        private string HandleDeviceConfigPostBackProc(string data)
        {
            try
            {
                var input = JObject.Parse(data);

                if (input["operation"]?.ToString() == "GET")
                {
                    var homeId = input["homeId"]?.ToString();
                    var nodeId = (byte?)input["nodeId"];
                    var parameter = (byte?)input["parameter"];

                    if (string.IsNullOrWhiteSpace(homeId) || !nodeId.HasValue || !parameter.HasValue)
                    {
                        throw new ArgumentException("Input not valid");
                    }

                    var connection = CreateZWaveConnection();
                    int value = connection.GetConfiguration(homeId, nodeId.Value, parameter.Value).ResultForSync();

                    return JsonConvert.SerializeObject(new ZWaveParameterGetResult()
                    {
                        Value = value
                    });
                }
                throw new ArgumentException("Unknown operation");
            }
            catch (Exception ex)
            {
                logger.Error(Invariant($"Failed to process PostBackProc for Update with {data} with error {ex.GetFullMessage()}"));
                return JsonConvert.SerializeObject(new ZWaveParameterGetResult()
                {
                    ErrorMessage = ex.GetFullMessage(HTMLEndline)
                });
            }
        }

        private void UpdateDebugLevel()
        {
            this.LogDebug = pluginConfig!.DebugLogging;
            Logger.ConfigureLogging(LogDebug, pluginConfig.LogToFile, HomeSeerSystem);
        }

        internal struct ZWaveParameterGetResult
        {
            public string? ErrorMessage { get; init; }
            public int? Value { get; init; }
        }

        private const string HTMLEndline = "<BR>";
        private readonly static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IDictionary<int, IDeviceConfigPage> cacheForUpdate = new ConcurrentDictionary<int, IDeviceConfigPage>();
        private PluginConfig? pluginConfig;
    }
}