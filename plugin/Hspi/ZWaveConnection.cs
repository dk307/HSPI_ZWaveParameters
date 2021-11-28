using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Devices;
using Hspi.Exceptions;
using Nito.AsyncEx;
using Serilog;
using System;
using System.Globalization;
using System.Threading;
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

        public async Task<int> GetConfiguration(string homeId, byte nodeId, byte param, CancellationToken cancellationtoken)
        {
            try
            {
                object? value = null;
                bool wasSuccessful = false;

                // we use lock because we read status later from another variable
                using (var readLock = await getConfiguationLock.LockAsync(cancellationtoken).ConfigureAwait(false))
                {
                    Log.Debug("Getting homeId:{homeId} nodeId:{nodeId} parameter:{parameter}", homeId, nodeId, param);
                    value = HomeSeerSystem.LegacyPluginFunction(ZWaveInterface, string.Empty, "Configuration_Get", new object[3] { homeId, nodeId, param });

                    wasSuccessful = (bool)HomeSeerSystem.LegacyPluginPropertyGet(ZWaveInterface, string.Empty, "Configuration_Get_Result");
                }

                if (value == null || !wasSuccessful)
                {
                    throw new ZWaveGetConfigurationFailedException(Invariant($"Failed to get parameter {param} for node {nodeId} "));
                }

                int intValue = Convert.ToInt32(value);

                Log.Debug("For homeId:{homeId} nodeId:{nodeId} parameter:{parameter} got {value}", homeId, nodeId, param, intValue);
                return intValue;
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

        public ZWaveData GetDeviceZWaveData(int deviceRef)
        {
            if (!IsZwaveDevice(deviceRef))
            {
                throw new NotAZWaveDeviceException("Device is not a Z-Wave device");
            }

            var plugInData = (PlugExtraData)HomeSeerSystem.GetPropertyByRef(deviceRef, EProperty.PlugExtraData);
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

            Log.Debug("PED Data for deviceRef:{deviceRef} is manufacturerId:{manufacturerId} productId:{productId} productType:{productType} firmware:{firmware}",
                       deviceRef, manufacturerId, productId, productType, firmware);

            if (!manufacturerId.HasValue
                || !productType.HasValue
                || !productId.HasValue
                || !nodeId.HasValue
                || homeId == null
                || string.IsNullOrWhiteSpace(firmware))
            {
                throw new ZWavePlugInDataInvalidException("Device Z-Wave plugin data is not valid");
            }

            if (manufacturerId == 0 && productId == 0 && productType == 0 && firmware == "0")
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

            Log.Debug("ZwaveData for deviceRef:{deviceRef} is {@data}", deviceRef, zwaveData);

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
                Log.Information("Updating homeId:{homeId} nodeId:{nodeId} param:{param} size:{size} bytes value:{value}",
                                homeId, nodeId, param, size, value);

                var result = HomeSeerSystem.LegacyPluginFunction("Z-Wave", string.Empty, "SetDeviceParameterValue", new object[5] { homeId, nodeId, param, size, value }) as string;

                switch (result)
                {
                    case "Queued":
                    case "Success":
                        Log.Information("Updated homeId:{homeId} nodeId:{nodeId} param:{param} size:{size} bytes value:{value} with {result}",
                                         homeId, nodeId, param, size, value, result);
                        break;

                    case "Unknown":
                    case "Failed":
                        throw new ZWaveSetConfigurationFailedException(Invariant($"Failed to set parameter {param} for node {nodeId}"));

                    default:
                        CheckZWavePlugInRunning();
                        throw new ZWaveSetConfigurationFailedException(Invariant($"Failed to set parameter {param} for node {nodeId}"));
                }
            }
            catch (ZWaveSetConfigurationFailedException) { throw; }
            catch (ZWavePluginNotRunningException) { throw; }
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
                string stringValue = plugInData[name].Trim('"', '\\');
                return Hspi.Utils.StringConverter.TryGetFromString<T>(stringValue);
            }
            else
            {
                return null;
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
                throw new ZWavePluginNotRunningException("Z-Wave plugin not found", ex);
            }
        }

        private const string ZWaveInterface = "Z-Wave";
        private readonly AsyncLock getConfiguationLock = new();
        private readonly IHsController HomeSeerSystem;
    }
}