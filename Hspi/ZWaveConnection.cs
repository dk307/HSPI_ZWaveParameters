using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Devices;
using System;

using static System.FormattableString;

#nullable enable

namespace Hspi
{
    internal class ZWaveConnection
    {
        public ZWaveConnection(IHsController hsController)
        {
            this.HomeSeerSystem = hsController;
        }

        public int GetConfiguration(string homeId, byte nodeId, byte param)
        {
            logger.Debug(Invariant($"Getting HomeId:{homeId} NodeId:{nodeId} Parameter:{param}"));
            var value = (int)HomeSeerSystem.LegacyPluginFunction(ZWaveInterface, string.Empty, "Configuration_Get", new object[3] { homeId, nodeId, param });
            logger.Debug(Invariant($"For HomeId:{homeId} NodeId:{nodeId} Parameter:{param} got {value}"));
            return value;
        }

        public ZWaveData GetDeviceZWaveData(int deviceOrFeatureRef)
        {
            if (!IsZwaveDevice(deviceOrFeatureRef))
            {
                throw new Exception("Device is not a Z-Wave device");
            }

            var plugInData = (PlugExtraData)HomeSeerSystem.GetPropertyByRef(deviceOrFeatureRef, EProperty.PlugExtraData);
            if (plugInData == null)
            {
                throw new Exception("Device Plugin extra data is not valid");
            }

            var manufacturerId = GetValueFromExtraData<Int32>(plugInData, "manufacturer_id");
            var productId = GetValueFromExtraData<UInt16>(plugInData, "manufacturer_prod_id");
            var productType = GetValueFromExtraData<UInt16>(plugInData, "manufacturer_prod_type");

            var nodeId = GetValueFromExtraData<Byte>(plugInData, "node_id");
            var homeId = plugInData["homeid"];
            var firmware = plugInData["node_version_app"];

            if (!manufacturerId.HasValue || !productType.HasValue ||
                    !productId.HasValue || !nodeId.HasValue || homeId == null || string.IsNullOrWhiteSpace(firmware))
            {
                throw new Exception("Device Z-Wave plugin data is not valid");
            }

            var zwaveData = new ZWaveData(manufacturerId.Value, productId.Value, 
                                            productType.Value, nodeId.Value, homeId, new Version(firmware));
            return zwaveData;
        }

        public bool IsZwaveDevice(int devOrFeatRef)
        {
            return ((string)HomeSeerSystem.GetPropertyByRef(devOrFeatRef, EProperty.Interface) == ZWaveInterface);
        }

        public void UpdateDeviceParameter(string homeId, byte nodeId, byte param, int size, int value)
        {
            logger.Info(Invariant($"Updating HomeId:{homeId} NodeId:{nodeId} Parameter:{param} Size:{size} bytes  Value:{value}"));
        }
        private static T? GetValueFromExtraData<T>(PlugExtraData plugInData, string name) where T : struct
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
        public record ZWaveData(int ManufactureId, ushort ProductId, ushort ProductType, byte NodeId, string HomeId, Version Firmware);
        private readonly IHsController HomeSeerSystem;
    }
}