using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Devices;
using System;

#nullable enable

namespace Hspi.DeviceData
{
    internal static class HSDeviceHelper
    {
        public static string GetName(IHsController HS, int refId)
        {
            try
            {
                return HS.GetNameByRef(refId);
            }
            catch
            {
                return $"RefId:{refId}";
            }
        }

        public static PlugExtraData CreatePlugInExtraDataForDeviceType(string deviceType)
        {
            var plugExtra = new PlugExtraData();
            plugExtra.AddNamed(PlugInData.DevicePlugInDataTypeKey, deviceType);
            return plugExtra;
        }

        public static string? GetDeviceTypeFromPlugInData(IHsController HS, int refId)
        {
            var plugInExtra = HS.GetPropertyByRef(refId, EProperty.PlugExtraData) as PlugExtraData;
            return GetDeviceTypeFromPlugInData(plugInExtra);
        }

        public static string? GetDeviceTypeFromPlugInData(PlugExtraData? plugInExtra)
        {
            if (plugInExtra != null && plugInExtra.NamedKeys.Contains(PlugInData.DevicePlugInDataTypeKey))
            {
                return plugInExtra[PlugInData.DevicePlugInDataTypeKey];
            }

            return null;
        }

        public static void UpdateDeviceValue(IHsController HS, int refId, in double? data)
        {
            if (data.HasValue)
            {
                HS.UpdatePropertyByRef(refId, EProperty.InvalidValue, false);

                // only this call triggers events
                if (!HS.UpdateFeatureValueByRef(refId, data.Value))
                {
                    throw new Exception("Failed to update device");
                }
            }
            else
            {
                HS.UpdatePropertyByRef(refId, EProperty.InvalidValue, true);
            }
        }
    }
}