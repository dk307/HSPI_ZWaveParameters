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
            try
            {
                // we use lock because we read status later from another variable
                using var readLock = await getConfiguationLock.LockAsync().ConfigureAwait(false);
                logger.Debug(Invariant($"Getting HomeId:{homeId} NodeId:{nodeId} Parameter:{param}"));
                var value = (int)HomeSeerSystem.LegacyPluginFunction(ZWaveInterface, string.Empty, "Configuration_Get", new object[3] { homeId, nodeId, param });

                bool wasSuccessful = (bool)HomeSeerSystem.LegacyPluginPropertyGet(ZWaveInterface, string.Empty, "Configuration_Get_Result");

                readLock.Dispose();

                if (!wasSuccessful)
                {
                    throw new ZWaveGetConfigurationFailedException(Invariant($"Failed to get parameter {param} for node {nodeId} "));
                }

                logger.Debug(Invariant($"For HomeId:{homeId} NodeId:{nodeId} Parameter:{param} got {value}"));
                return value;
            }
            catch (ZWaveGetConfigurationFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                CheckZWavePlugInRunning();
                throw new ZWaveGetConfigurationFailedException(Invariant($"Failed to get parameter {param} for node {nodeId} "), ex);
            }
        }

        private void CheckZWavePlugInRunning()
        {
            try
            {
                HomeSeerSystem.GetPluginVersionById(ZWaveInterface);
            }
            catch (Exception ex)
            {
                throw new ZWavePluginNotRunningException("ZWave plugin not found", ex);
            }
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

            bool listening = DetermineListeningDevice(plugInData);

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
                                          productType.Value, nodeId.Value, homeId, firmwareVersion, listening);
            return zwaveData;
        }

        public bool IsZwaveDevice(int devOrFeatRef)
        {
            return ((string)HomeSeerSystem.GetPropertyByRef(devOrFeatRef, EProperty.Interface) == ZWaveInterface);
        }

        public void SetConfiguration(string homeId, byte nodeId, byte param, byte size, int value)
        {
            try
            {
                logger.Info(Invariant($"Updating HomeId:{homeId} NodeId:{nodeId} Parameter:{param} Size:{size} bytes  Value:{value}"));
                var result = HomeSeerSystem.LegacyPluginFunction("Z-Wave", string.Empty, "SetDeviceParameterValue", new object[5] { homeId, nodeId, param, size, value }) as string;

                switch (result)
                {
                    case "Queued":
                    case "Success":
                        logger.Info(Invariant($"Updated HomeId:{homeId} NodeId:{nodeId} Parameter:{param} Size:{size} bytes  Value:{value} with result:{result}"));
                        break;

                    case "Unknown":
                    case "Failed":
                        throw new ZWaveSetConfigurationFailedException(Invariant($"Failed to set parameter {param} for node {nodeId}"));

                    default:
                    case null:
                        CheckZWavePlugInRunning();
                        throw new ZWaveSetConfigurationFailedException(Invariant($"Failed to set parameter {param} for node {nodeId}"));
                }
            }
            catch (Exception ex)
            {
                throw new ZWaveSetConfigurationFailedException(Invariant($"Failed to set parameter {param} for node {nodeId}"), ex);
            }
        }

        private static bool DetermineListeningDevice(PlugExtraData plugInData)
        {
            var capability = GetValueFromExtraDataWithTrim<int>(plugInData, "capability");
            var security = GetValueFromExtraDataWithTrim<int>(plugInData, "security");

            return (capability.HasValue && ((capability.Value & 0x80) != 0)) ||
                (security.HasValue && ((security.Value & 0x20) == 0x20)) ||
                (security.HasValue && ((security.Value & 0x40) == 0x40));
        }

        private static string? GetValueFromExtraData(PlugExtraData plugInData, string name)
        {
            return plugInData.ContainsNamed(name) ? plugInData[name] : null;
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

        private const string ZWaveInterface = "Z-Wave";
        private readonly static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly AsyncLock getConfiguationLock = new();
        private readonly IHsController HomeSeerSystem;
    }
}