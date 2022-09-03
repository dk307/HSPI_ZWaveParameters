using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Devices;
using HomeSeer.PluginSdk.Logging;
using Hspi;
using Hspi.OpenZWaveDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HSPI_ZWaveParametersTest
{
    internal static class TestHelper
    {
        public static ZWaveData AeonLabsZWaveData => new(0x0086, 6, 3, 67, "32", new Version(5, 0), true);

        public static ZWaveData HomeseerDimmerZWaveData => new(0x000C, 0x3036, 0x4447, 23, "56", new Version(5, 15), true);

        public static Mock<IHttpQueryMaker> CreateAeonLabsSwitchHttpHandler()
        {
            return CreateMockHttpHandler("https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x0086%200003:0006",
                                                                Resource.AeonLabsOpenZWaveDBDeviceListJson,
                                                                "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=75",
                                                                Resource.AeonLabsOpenZWaveDBDeviceJson);
        }

        public static Mock<IHttpQueryMaker> CreateMockHttpHandler(string deviceListUrl, string deviceListJson, string deviceUrl, string deviceJson)
        {
            var mock = new Mock<IHttpQueryMaker>(MockBehavior.Strict);

            SetupRequest(mock, deviceListUrl, deviceListJson);
            SetupRequest(mock, deviceUrl, deviceJson);

            return mock;
        }

        public static (Mock<PlugIn> mockPlugin, Mock<IHsController> mockHsController)
                CreateMockPluginAndHsController()
        {
            return CreateMockPluginAndHsController(new Dictionary<string, string>());
        }

        public static (Mock<PlugIn> mockPlugin, Mock<IHsController> mockHsController)
                 CreateMockPluginAndHsController(Dictionary<string, string> settingsFromIni)
        {
            var mockPlugin = new Mock<PlugIn>(MockBehavior.Loose)
            {
                CallBase = true,
            };

            var mockHsController = SetupHsControllerAndSettings(mockPlugin, settingsFromIni);

            var offLineDatabase = new OfflineOpenZWaveDatabase(TestHelper.GetOfflineDatabasePath());

            mockPlugin.Protected()
                      .Setup<OfflineOpenZWaveDatabase>("CreateOfflineOpenDBOfflineDatabase")
                      .Returns(offLineDatabase);

            mockPlugin.Object.InitIO();

            return (mockPlugin, mockHsController);
        }

        public static string GetOfflineDatabasePath()
        {
            string dllPath = Assembly.GetExecutingAssembly().Location;

            var parentDirectory = new DirectoryInfo(Path.GetDirectoryName(dllPath));
            return Path.Combine(parentDirectory.Parent.Parent.Parent.FullName, "plugin", "db");
        }

        public static void SetupGetConfigurationInHsController(string homeId, byte nodeId, byte param, int value, Mock<IHsController> mock)
        {
            mock.Setup(x => x.LegacyPluginFunction(ZWaveInterface, string.Empty, "Configuration_Get", new object[3] { homeId, nodeId, param }))
                .Returns(value);

            mock.Setup(x => x.LegacyPluginPropertyGet(ZWaveInterface, string.Empty, "Configuration_Get_Result"))
                .Returns(true);
        }

        public static Mock<IHsController> SetupHsControllerAndSettings(Mock<PlugIn> mockPlugin,

                                                         Dictionary<string, string> settingsFromIni)
        {
            var mockHsController = new Mock<IHsController>(MockBehavior.Strict);

            // set mock homeseer via reflection
            Type plugInType = typeof(AbstractPlugin);
            var method = plugInType.GetMethod("set_HomeSeerSystem", BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance);
            method.Invoke(mockPlugin.Object, new object[] { mockHsController.Object });

            mockHsController.Setup(x => x.GetIniSection("Settings", PlugInData.PlugInId + ".ini")).Returns(settingsFromIni);
            mockHsController.Setup(x => x.SaveINISetting("Settings", It.IsAny<string>(), It.IsAny<string>(), PlugInData.PlugInId + ".ini"));
            mockHsController.Setup(x => x.WriteLog(It.IsAny<ELogType>(), It.IsAny<string>(), PlugInData.PlugInName, It.IsAny<string>()));
            return mockHsController;
        }

        public static void SetupRequest(Mock<IHttpQueryMaker> mock,
                                        string deviceListUrl, string deviceListJson)
        {
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(deviceListJson));
            mock.Setup(x => x.GetUtf8JsonResponse(deviceListUrl, It.IsAny<CancellationToken>()))
                               .Returns(Task.FromResult(stream));
        }

        public static void SetupZWaveDataInHsControllerMock(Mock<IHsController> mock,
                                                            int deviceRef,
                                                            string manufactureId,
                                                            string productId,
                                                            string productType,
                                                            string nodeId,
                                                            string homeId,
                                                            string firmware,
                                                            string firmwareStr,
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
            AddIfNotNull("node_version_app_string", firmwareStr);
            AddIfNotNull("node_version_app_string", firmwareStr);
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

        public static void VerifyHtmlValid(string html)
        {
            HtmlAgilityPack.HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(html);
            Assert.AreEqual(0, htmlDocument.ParseErrors.Count());
        }

        public static Mock<PlugIn> CreatePlugInMock()
        {
            return new Mock<PlugIn>(MockBehavior.Loose)
            {
                CallBase = true,
            };
        }

        public const string ZWaveInterface = "Z-Wave";
    }
}