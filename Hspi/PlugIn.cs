using HomeSeer.Jui.Views;
using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Devices;
using Hspi.Utils;
using Nito.AsyncEx;
using System;
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
                DeviceConfigPage page = new DeviceConfigPage(HomeSeerSystem);
                return page.BuildConfigPage(deviceOrFeatureRef, CancellationToken.None).ResultForSync();
            }
            catch (Exception ex)
            {
                var page = PageFactory.CreateDeviceConfigPage(PlugInData.PlugInId, "Z-Wave Information");
                page.WithLabel("exception", ex.GetFullMessage());
                return page.Page.ToJsonString();
            }
        }

        public string RefreshDeviceParameterValues(string homeId, byte nodeId)
        {
            return (string)HomeSeerSystem.LegacyPluginFunction("Z-Wave", "", "RefreshDeviceParameterValues", new object[2] { homeId, nodeId }) as string;
        }



        public override string PostBackProc(string page, string data, string user, int userRights)
        {
            return base.PostBackProc(page, data, user, userRights);
        }

        public override bool HasJuiDeviceConfigPage(int devOrFeatRef)
        {
            return DeviceConfigPage.IsZwaveDevice(HomeSeerSystem, devOrFeatRef);
        }

        public override EPollResponse UpdateStatusNow(int devOrFeatRef)
        {
            try
            {
                return EPollResponse.Ok;
            }
            catch (Exception ex)
            {
                logger.Error(Invariant($"Failed to import value for Ref Id: {devOrFeatRef} with {ex.GetFullMessage()}"));
                return EPollResponse.ErrorGettingStatus;
            }
        }

        protected override void BeforeReturnStatus()
        {
            this.Status = PluginStatus.Ok();
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

                logger.Info("Starting Plugin");

                logger.Info("Plugin Started");
            }
            catch (Exception ex)
            {
                string result = Invariant($"Failed to initialize PlugIn with {ex.GetFullMessage()}");
                logger.Error(result);
                throw;
            }
        }

        private void PluginConfigChanged()
        {
            UpdateDebugLevel();
        }

        private void UpdateDebugLevel()
        {
            this.LogDebug = pluginConfig!.DebugLogging;
            Logger.ConfigureLogging(LogDebug, pluginConfig.LogToFile, HomeSeerSystem);
        }

        private readonly static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly AsyncLock dataLock = new AsyncLock();
        private PluginConfig? pluginConfig;
    }
}