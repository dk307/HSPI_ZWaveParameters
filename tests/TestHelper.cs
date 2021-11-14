using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Devices;
using Hspi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Contrib.HttpClient;
using System;
using System.Linq;
using System.Net.Http;

namespace HSPI_ZWaveParametersTest
{
    internal static class TestHelper
    {
        public static ZWaveData AeonLabsZWaveData => new(0x0086, 6, 3, 67, "32", new Version(5, 0), true);

        public static ZWaveData HomeseerDimmerZWaveData => new(0x000C, 0x3036, 0x4447, 23, "56", new Version(5, 15), true);

        public static Mock<HttpMessageHandler> CreateAeonLabsSwitchHttpHandler()
        {
            return CreateMockHttpHandler("https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x0086%200003:0006",
                                                                Resource.AeonLabsOpenZWaveDBDeviceListJson,
                                                                "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=75",
                                                                Resource.AeonLabsOpenZWaveDBDeviceJson);
        }

        public static Mock<HttpMessageHandler> CreateHomeseerDimmerHttpHandler()
        {
            return CreateMockHttpHandler("https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x000C%204447:3036",
                                         Resource.HomeseerDimmerOpenZWaveDBDeviceListJson,
                                         "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=1040",
                                         Resource.HomeseerDimmerOpenZWaveDBFullJson);
        }

        public static void SetupGetConfigurationInHsController(string homeId, byte nodeId, byte param, int value, Mock<IHsController> mock)
        {
            mock.Setup(x => x.LegacyPluginFunction(ZWaveInterface, string.Empty, "Configuration_Get", new object[3] { homeId, nodeId, param }))
                .Returns(value);

            mock.Setup(x => x.LegacyPluginPropertyGet(ZWaveInterface, string.Empty, "Configuration_Get_Result"))
                .Returns(true);
        }

        public static void SetupZWaveDataInHsControllerMock(Mock<IHsController> mock,
                                                            int deviceRef,
                                                            string manufactureId,
                                                            string productId,
                                                            string productType,
                                                            string nodeId,
                                                            string homeId,
                                                            string firmware,
                                                            string capability,
                                                            string security)
        {
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.Interface)).Returns(ZWaveInterface);

            var plugInExtraData = new PlugExtraData();
            AddIfNotNull("manufacturer_id", manufactureId);
            AddIfNotNull("manufacturer_prod_id", productId);
            AddIfNotNull("manufacturer_prod_type", productType);
            AddIfNotNull("node_id", nodeId);
            AddIfNotNull("homeid", homeId);
            AddIfNotNull("node_version_app", firmware);
            AddIfNotNull("capability", capability);
            AddIfNotNull("security", security);

            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.PlugExtraData)).Returns(plugInExtraData);

            void AddIfNotNull(string id, string value)
            {
                if (value != null)
                {
                    plugInExtraData.AddNamed(id, value);
                }
            }
        }

        public static void VeryHtmlValid(string html)
        {
            HtmlAgilityPack.HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(html);
            Assert.AreEqual(htmlDocument.ParseErrors.Count(), 0);
        }
        private static Mock<HttpMessageHandler> CreateMockHttpHandler(string deviceListUrl, string deviceListJson, string deviceUrl, string deviceJson)
        {
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            handler.SetupRequest(HttpMethod.Get, deviceListUrl)
                               .ReturnsResponse(deviceListJson, "application/json");

            handler.SetupRequest(HttpMethod.Get, deviceUrl)
                               .ReturnsResponse(deviceJson, "application/json");
            return handler;
        }

        public const string ZWaveInterface = "Z-Wave";
    }
}