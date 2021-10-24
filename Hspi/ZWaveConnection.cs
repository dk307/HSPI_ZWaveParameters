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

            if (!manufacturerId.HasValue || !productType.HasValue ||
                    !productId.HasValue || !nodeId.HasValue || homeId == null)
            {
                throw new Exception("Device Z-Wave plugin data is not valid");
            }

            var zwaveData = new ZWaveData(manufacturerId.Value, productId.Value, productType.Value, nodeId.Value, homeId);
            return zwaveData;
        }

        public void UpdateDeviceParameter(string homeId, byte nodeId, byte param, int size, int value)
        {
            logger.Info(Invariant($"Updating HomeId:{homeId} NodeId:{nodeId} Parameter:{param} Size:{size} bytes  Value:{value}"));
        }

        public bool IsZwaveDevice(int devOrFeatRef)
        {
            return ((string)HomeSeerSystem.GetPropertyByRef(devOrFeatRef, EProperty.Interface) == ZWaveInterface);
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

        private readonly static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private const string ZWaveInterface = "Z-Wave";
        public record ZWaveData(int ManufactureId, ushort productId, ushort ProductType, byte NodeId, string HomeId);
        private readonly IHsController HomeSeerSystem;
    }
}