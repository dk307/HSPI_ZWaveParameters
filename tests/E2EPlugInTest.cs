using HomeSeer.Jui.Types;
using HomeSeer.Jui.Views;
using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Logging;
using Hspi;
using Hspi.OpenZWaveDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Serilog;
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
        public void CheckDebugLevelSettingChange()
        {
            var (plugInMock, hsControllerMock) = TestHelper.CreateMockPluginAndHsController();

            PlugIn plugIn = plugInMock.Object;

            var httpQueryMock = TestHelper.CreateAeonLabsSwitchHttpHandler();

            plugInMock.Protected()
                .Setup<IHttpQueryMaker>("CreateHttpQueryMaker")
                .Returns(httpQueryMock.Object);

            const int deviceRef = 8475;
            CreateMockForHsController(hsControllerMock, deviceRef, TestHelper.AeonLabsZWaveData);

            Assert.IsFalse(Log.Logger.IsEnabled(Serilog.Events.LogEventLevel.Debug));

            var settingsCollection = new SettingsCollection
            {
                // invert all values
                SettingsPages.CreateDefault(preferOnlineDatabaseDefault: true,
                                            enableDebugLoggingDefault : true,
                                            logToFileDefault : true,
                                            showSubParameteredValuesAsHexDefault: true)
            };

            Assert.IsTrue(plugIn.SaveJuiSettingsPages(settingsCollection.ToJsonString()));

            Assert.IsTrue(Log.Logger.IsEnabled(Serilog.Events.LogEventLevel.Debug));

            VerifyCorrectDeviceConfigPage(deviceRef, plugIn);

            plugInMock.Verify();
        }

        [TestMethod]
        public void CheckDefaultSettingAreLoadedDuringInitialize()
        {
            var (plugInMock, _) = TestHelper.CreateMockPluginAndHsController();

            PlugIn plugIn = plugInMock.Object;
            Assert.IsTrue(plugIn.HasSettings);

            var settingPages = SettingsCollection.FromJsonString(plugIn.GetJuiSettingsPages());
            Assert.IsNotNull(settingPages);

            var settings = settingPages[SettingsPages.SettingPageId].ToValueMap();

            Assert.AreEqual(settings[SettingsPages.PreferOnlineDatabaseId], false.ToString());
            Assert.AreEqual(settings[SettingsPages.LoggingDebugId], false.ToString());
            Assert.AreEqual(settings[SettingsPages.LogToFileId], false.ToString());
            Assert.AreEqual(settings[SettingsPages.ShowSubParameteredValuesAsHexId], false.ToString());
        }

        [TestMethod]
        public void CheckDevicePageIsReturnedForOfflineCase()
        {
            var settingsFromIni = new Dictionary<string, string>
            {
                { SettingsPages.PreferOnlineDatabaseId, false.ToString()}
            };

            var (plugInMock, hsControllerMock) = TestHelper.CreateMockPluginAndHsController(settingsFromIni);

            const int deviceRef = 8475;
            CreateMockForHsController(hsControllerMock, deviceRef, TestHelper.AeonLabsZWaveData);

            PlugIn plugIn = plugInMock.Object;
            VerifyCorrectDeviceConfigPage(deviceRef, plugIn);
        }

        [TestMethod]
        public void CheckDevicePageIsReturnedForOnlineCase()
        {
            var settingsFromIni = new Dictionary<string, string>()
            {
                { SettingsPages.PreferOnlineDatabaseId, true.ToString()}
            };

            var (plugInMock, hsControllerMock) = TestHelper.CreateMockPluginAndHsController(settingsFromIni);

            var httpQueryMock = TestHelper.CreateAeonLabsSwitchHttpHandler();

            plugInMock.Protected()
                .Setup<IHttpQueryMaker>("CreateHttpQueryMaker")
                .Returns(httpQueryMock.Object);

            const int deviceRef = 8475;
            CreateMockForHsController(hsControllerMock, deviceRef, TestHelper.AeonLabsZWaveData);

            PlugIn plugIn = plugInMock.Object;
            VerifyCorrectDeviceConfigPage(deviceRef, plugIn);

            httpQueryMock.Verify();
        }

        [TestMethod]
        public void CheckPlugInStatus()
        {
            var (plugInMock, _) = TestHelper.CreateMockPluginAndHsController();

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
                { SettingsPages.ShowSubParameteredValuesAsHexId, true.ToString()},
            };

            var (plugInMock, _) = TestHelper.CreateMockPluginAndHsController(settingsFromIni);

            PlugIn plugIn = plugInMock.Object;
            Assert.IsTrue(plugIn.HasSettings);

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



        private static void VerifyCorrectDeviceConfigPage(int deviceRef, PlugIn plugIn)
        {
            var pageJson = plugIn.GetJuiDeviceConfigPage(deviceRef);

            // assert page is not error
            var page = Page.FromJsonString(pageJson);

            Assert.AreEqual(page.Type, EPageType.DeviceConfig);
            Assert.IsFalse(page.ContainsViewWithId("exception"));
        }
    }
}