using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Devices;
using Hspi.Exceptions;
using Nito.AsyncEx;
using System;
using System.Globalization;
using System.Threading.Tasks;
using static System.FormattableString;

#nullable enable

namespace Hspi
{
    internal class ZWaveConnection : IZWaveConnection
    {
        public ZWaveConnection(IHsController hsController)
        {
            this.HomeSeerSystem = hsController;
        }

        public async Task<int> GetConfiguration(string homeId, byte nodeId, byte param)
        {
            // we use lock because we read status later from another variable
            using var readLock = await getConfiguationLock.LockAsync().ConfigureAwait(false);
            logger.Debug(Invariant($"Getting HomeId:{homeId} NodeId:{nodeId} Parameter:{param}"));
            var value = (int)HomeSeerSystem.LegacyPluginFunction(ZWaveInterface, string.Empty, "Configuration_Get", new object[3] { homeId, nodeId, param });

            bool wasSuccessful = (bool)HomeSeerSystem.LegacyPluginPropertyGet(ZWaveInterface, string.Empty, "Configuration_Get_Result");

            readLock.Dispose();

            if (!wasSuccessful)
            {
                throw new Exception("Failed to get parameter");
            }

            logger.Debug(Invariant($"For HomeId:{homeId} NodeId:{nodeId} Parameter:{param} got {value}"));
            return value;
        }

        public ZWaveData GetDeviceZWaveData(int deviceOrFeatureRef)
        {
            if (!IsZwaveDevice(deviceOrFeatureRef))
            {
                throw new NotAZWaveDeviceException("Device is not a Z-Wave device");
            }

            var plugInData = (PlugExtraData)HomeSeerSystem.GetPropertyByRef(deviceOrFeatureRef, EProperty.PlugExtraData);
            if (plugInData == null)
            {
                throw new ZWavePlugInDataInvalidException("Device Plugin extra data is not valid");
            }

            var manufacturerId = GetValueFromExtraDataWithTrim<Int32>(plugInData, "manufacturer_id");
            var productId = GetValueFromExtraDataWithTrim<UInt16>(plugInData, "manufacturer_prod_id");
            var productType = GetValueFromExtraDataWithTrim<UInt16>(plugInData, "manufacturer_prod_type");

            var nodeId = GetValueFromExtraDataWithTrim<Byte>(plugInData, "node_id");
            var homeId = GetValueFromExtraData(plugInData, "homeid");
            var firmware = GetValueFromExtraData(plugInData, "node_version_app");

            if (!manufacturerId.HasValue
                || !productType.HasValue
                || !productId.HasValue
                || !nodeId.HasValue
                || homeId == null
                || string.IsNullOrWhiteSpace(firmware))
            {
                throw new ZWavePlugInDataInvalidException("Device Z-Wave plugin data is not valid");
            }

            Version firmwareVersion;
            // some version are simple digits, version parse needs minor version too
            if (int.TryParse(firmware, NumberStyles.Integer, CultureInfo.InvariantCulture, out var singleDigit))
            {
                firmwareVersion = new Version(singleDigit, 0);
            }
            else
            {
                firmwareVersion = new Version(firmware);
            }

            var zwaveData = new ZWaveData(manufacturerId.Value, productId.Value,
                                          productType.Value, nodeId.Value, homeId, firmwareVersion);
            return zwaveData;
        }

        public bool IsZwaveDevice(int devOrFeatRef)
        {
            return ((string)HomeSeerSystem.GetPropertyByRef(devOrFeatRef, EProperty.Interface) == ZWaveInterface);
        }

        public void UpdateDeviceParameter(string homeId, byte nodeId, byte param, byte size, int value)
        {
            logger.Info(Invariant($"Updating HomeId:{homeId} NodeId:{nodeId} Parameter:{param} Size:{size} bytes  Value:{value}"));
            var result = HomeSeerSystem.LegacyPluginFunction("Z-Wave", "", "Configuration_Set", new object[5] { homeId, nodeId, param, size, value }) as string;

            var validResults = new string[4] { "Unknown", "Success", "Queued", "Failed" };
        }

        private static T? GetValueFromExtraDataWithTrim<T>(PlugExtraData plugInData, string name) where T : struct
        {
            if (plugInData.ContainsNamed(name))
            {
                return (T)Convert.ChangeType(plugInData[name].Trim('"', '\\'), typeof(T));
            }
            else
            {
                return null;
            }
        }

        private static string? GetValueFromExtraData(PlugExtraData plugInData, string name)
        {
            return plugInData.ContainsNamed(name) ? plugInData[name] : null;
        }

        private const string ZWaveInterface = "Z-Wave";
        private readonly static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly AsyncLock getConfiguationLock = new();
        private readonly IHsController HomeSeerSystem;
    }
}