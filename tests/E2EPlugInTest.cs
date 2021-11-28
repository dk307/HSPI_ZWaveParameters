using HomeSeer.Jui.Types;
using HomeSeer.Jui.Views;
using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Logging;
using Hspi;
using Hspi.OpenZWaveDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace HSPI_ZWaveParametersTest
{
    // In these tests we also mock IHsController as compared to other tests
    [TestClass]
    public class E2EPlugInTest
    {
        [TestMethod]
        public void CheckDefaultSettingAreLoadedDuringInitialize()
        {
            var (plugInMock, _) = CreateMockPluginAndHsController();

            PlugIn plugIn = plugInMock.Object;
            Assert.AreEqual(plugIn.HasSettings, true);

            var settingPages = SettingsCollection.FromJsonString(plugIn.GetJuiSettingsPages());
            Assert.IsNotNull(settingPages);

            var settings = settingPages[SettingsPages.SettingPageId].ToValueMap();

            Assert.AreEqual(settings[SettingsPages.PreferOnlineDatabaseId], false.ToString());
            Assert.AreEqual(settings[SettingsPages.LoggingDebugId], false.ToString());
            Assert.AreEqual(settings[SettingsPages.LogToFileId], false.ToString());
        }

        [TestMethod]
        public void CheckDevicePageIsReturnedForOnlineCase()
        {
            var settingsFromIni = new Dictionary<string, string>()
            {
                { SettingsPages.PreferOnlineDatabaseId, true.ToString()}
            };

            var (plugInMock, hsControllerMock) = CreateMockPluginAndHsController(settingsFromIni);

            var httpQueryMock = TestHelper.CreateAeonLabsSwitchHttpHandler();

            plugInMock.Protected()
                .Setup<IHttpQueryMaker>("CreateHttpQueryMaker")
                .Returns(httpQueryMock.Object);

            const int deviceRef = 8475;
            CreateMockForHsController(hsControllerMock, deviceRef, TestHelper.AeonLabsZWaveData);

            PlugIn plugIn = plugInMock.Object;
            var pageJson = plugIn.GetJuiDeviceConfigPage(deviceRef);

            // assert page is not error
            var page = Page.FromJsonString(pageJson);

            Assert.AreEqual(page.Type, EPageType.DeviceConfig);
            Assert.IsFalse(page.ContainsViewWithId("exception"));
        }

        [TestMethod]
        public void CheckDevicePageIsReturnedForOfflineCase()
        {
            var settingsFromIni = new Dictionary<string, string>
            {
                { SettingsPages.PreferOnlineDatabaseId, false.ToString()}
            };

            var (plugInMock, hsControllerMock) = CreateMockPluginAndHsController(settingsFromIni);

            const int deviceRef = 8475;
            CreateMockForHsController(hsControllerMock, deviceRef, TestHelper.AeonLabsZWaveData);

            PlugIn plugIn = plugInMock.Object;
            var pageJson = plugIn.GetJuiDeviceConfigPage(deviceRef);

            // assert page is not error
            var page = Page.FromJsonString(pageJson);

            Assert.AreEqual(page.Type, EPageType.DeviceConfig);
            Assert.IsFalse(page.ContainsViewWithId("exception"));
        }

        [TestMethod]
        public void CheckPlugInStatus()
        {
            var (plugInMock, _) = CreateMockPluginAndHsController();

            PlugIn plugIn = plugInMock.Object;
            Assert.AreEqual(plugIn.OnStatusCheck().Status, PluginStatus.Ok().Status);
        }

        [TestMethod]
        public void CheckSettingsWithIniFilledDuringInitialize()
        {
            var settingsFromIni = new Dictionary<string, string>()
            {
                { SettingsPages.PreferOnlineDatabaseId, true.ToString()},
                { SettingsPages.LoggingDebugId, true.ToString()},
                { SettingsPages.LogToFileId, true.ToString()},
            };

            var (plugInMock, _) = CreateMockPluginAndHsController(settingsFromIni);

            PlugIn plugIn = plugInMock.Object;
            Assert.AreEqual(plugIn.HasSettings, true);

            var settingPages = SettingsCollection.FromJsonString(plugIn.GetJuiSettingsPages());
            Assert.IsNotNull(settingPages);

            var settings = settingPages[SettingsPages.SettingPageId].ToValueMap();

            Assert.AreEqual(settings[SettingsPages.PreferOnlineDatabaseId], true.ToString());
            Assert.AreEqual(settings[SettingsPages.LoggingDebugId], true.ToString());
            Assert.AreEqual(settings[SettingsPages.LogToFileId], true.ToString());
        }

        private static void CreateMockForHsController(Mock<IHsController> mock, int deviceRef, ZWaveData zwaveData)
        {
            TestHelper.SetupZWaveDataInHsControllerMock(mock,
                                                         deviceRef,
                                                         zwaveData.ManufactureId.ToString(),
                                                         zwaveData.ProductId.ToString(),
                                                         zwaveData.ProductType.ToString(),
                                                         zwaveData.HomeId.ToString(),
                                                         zwaveData.NodeId.ToString(),
                                                         zwaveData.Firmware.ToString(),
                                                         zwaveData.Listening ? 0x80.ToString() : "0",
                                                         "0");
        }

        private static (Mock<PlugIn> mockPlugin, Mock<IHsController> mockHsController)
                        CreateMockPluginAndHsController()
        {
            return CreateMockPluginAndHsController(new Dictionary<string, string>());
        }

        private static (Mock<PlugIn> mockPlugin, Mock<IHsController> mockHsController)
                         CreateMockPluginAndHsController(Dictionary<string, string> settingsFromIni)
        {
            var mockHsController = new Mock<IHsController>(MockBehavior.Strict);

            var mockPlugin = new Mock<PlugIn>(MockBehavior.Loose)
            {
                CallBase = true,
            };

            // set mock homeseer via reflection
            Type plugInType = typeof(AbstractPlugin);
            var method = plugInType.GetMethod("set_HomeSeerSystem", BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance);
            method.Invoke(mockPlugin.Object, new object[] { mockHsController.Object });

            mockHsController.Setup(x => x.GetIniSection("Settings", PlugInData.PlugInId + ".ini")).Returns(settingsFromIni);
            mockHsController.Setup(x => x.SaveINISetting("Settings", It.IsAny<string>(), It.IsAny<string>(), PlugInData.PlugInId + ".ini"));
            mockHsController.Setup(x => x.WriteLog(It.IsAny<ELogType>(), It.IsAny<string>(), PlugInData.PlugInName, It.IsAny<string>()));

            var offLineDatabase = new OfflineOpenZWaveDatabase(TestHelper.GetOfflineDatabasePath());

            mockPlugin.Protected()
                      .Setup<OfflineOpenZWaveDatabase>("CreateOfflineOpenDBOfflineDatabase")
                      .Returns(offLineDatabase);

            mockPlugin.Object.InitIO();

            return (mockPlugin, mockHsController);
        }
    }
}