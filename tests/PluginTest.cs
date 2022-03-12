using HomeSeer.Jui.Views;
using Hspi;
using Hspi.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using static Hspi.PlugIn;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class PlugInTest
    {
        [TestMethod]
        public void GetJuiDeviceConfigPage()
        {
            int devOrFeatRef = 8374;
            var (pluginMock, deviceConfigPageMock) = CreatePlugInAndDeviceConfig(devOrFeatRef);

            var page = PageFactory.CreateDeviceConfigPage("id", "name");
            page = page.WithInput("id1", "name2", "3", HomeSeer.Jui.Types.EInputType.Decimal);

            deviceConfigPageMock.Setup(x => x.BuildConfigPage(Moq.It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            deviceConfigPageMock.Setup(x => x.GetPage()).Returns(page.Page);

            var plugIn = pluginMock.Object;
            string pageJson = plugIn.GetJuiDeviceConfigPage(devOrFeatRef);

            Assert.AreEqual(pageJson, page.Page.ToJsonString());

            deviceConfigPageMock.Verify();
            pluginMock.Verify();
        }

        [TestMethod]
        public void GetJuiDeviceConfigPageErrored()
        {
            int devOrFeatRef = 8374;
            var pluginMock = TestHelper.CreatePlugInMock();

            var deviceConfigPageMock = new Mock<IDeviceConfigPage>(MockBehavior.Strict);

            pluginMock.Protected()
               .Setup<IDeviceConfigPage>("CreateDeviceConfigPage", devOrFeatRef)
               .Returns(deviceConfigPageMock.Object);

            string errorMessage = "sdfsd dfgdfg erter";
            deviceConfigPageMock.Setup(x => x.BuildConfigPage(Moq.It.IsAny<CancellationToken>()))
                .Throws(new Exception(errorMessage));

            var plugIn = pluginMock.Object;
            string pageJson = plugIn.GetJuiDeviceConfigPage(devOrFeatRef);

            var result = JsonNode.Parse(pageJson);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result["views"][0]["value"], errorMessage);

            deviceConfigPageMock.Verify();
            pluginMock.Verify();
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void HasJuiDeviceConfigPageChecksZWave(bool supports)
        {
            var (zwaveConnectionMock, pluginMock) = CreatePlugInWithZWaveConnection();

            int devOrFeatRef = 10;
            zwaveConnectionMock.Setup(x => x.IsZwaveDevice(devOrFeatRef)).Returns(supports);

            var plugIn = pluginMock.Object;
            bool expected = plugIn.HasJuiDeviceConfigPage(devOrFeatRef);
            Assert.AreEqual(expected, supports);

            pluginMock.Verify();
        }

        [TestMethod]
        public void OnDeviceConfigChangeAfterRestart()
        {
            var pluginMock = TestHelper.CreatePlugInMock();
            var page = PageFactory.CreateDeviceConfigPage("id", "name");
            try
            {
                pluginMock.Object.SaveJuiDeviceConfigPage(page.Page.ToJsonString(), 10);
            }
            catch (Exception ex)
            {
                Assert.IsNull(ex.InnerException);
                Assert.IsTrue(ex.Message.Contains("Plugin was restarted"));
            }
        }

        [TestMethod]
        public void OnDeviceConfigChangeThrowsError()
        {
            int devOrFeatRef = 10;
            var (pluginMock, deviceConfigPageMock) = CreatePlugInAndDeviceConfig(devOrFeatRef);

            var page = PageFactory.CreateDeviceConfigPage("id", "name");
            page = page.WithInput("id1", "name2", "3", HomeSeer.Jui.Types.EInputType.Decimal);

            deviceConfigPageMock.Setup(x => x.BuildConfigPage(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            deviceConfigPageMock.Setup(x => x.GetPage()).Returns(page.Page);

            var plugIn = pluginMock.Object;
            plugIn.GetJuiDeviceConfigPage(devOrFeatRef);

            deviceConfigPageMock.Verify();

            page.Page.Views[0].UpdateValue("34");

            string message = "sdksdkhskioweoir";
            deviceConfigPageMock.Setup(x => x.OnDeviceConfigChange(It.IsAny<Page>()))
                                .Throws(new ZWaveSetConfigurationFailedException(message));

            try
            {
                plugIn.SaveJuiDeviceConfigPage(page.Page.ToJsonString(), devOrFeatRef);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains(message));
            }

            deviceConfigPageMock.Verify();
            pluginMock.Verify();
        }

        [TestMethod]
        public void OnDeviceConfigChangeWithInput()
        {
            int devOrFeatRef = 10;
            var (pluginMock, deviceConfigPageMock) = CreatePlugInAndDeviceConfig(devOrFeatRef);

            var page = PageFactory.CreateDeviceConfigPage("id", "name");
            page = page.WithInput("id1", "name2", "3", HomeSeer.Jui.Types.EInputType.Decimal);
            page = page.WithDropDownSelectList("id2", "name2", new List<string> { "0", "1", "2" });

            deviceConfigPageMock.Setup(x => x.BuildConfigPage(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            deviceConfigPageMock.Setup(x => x.GetPage()).Returns(page.Page);

            var plugIn = pluginMock.Object;
            plugIn.GetJuiDeviceConfigPage(devOrFeatRef);

            deviceConfigPageMock.Verify();

            page.Page.Views[0].UpdateValue("34");
            page.Page.Views[1].UpdateValue("1");

            deviceConfigPageMock.Setup(x => x.OnDeviceConfigChange(It.IsAny<Page>()));

            Assert.IsTrue(plugIn.SaveJuiDeviceConfigPage(page.Page.ToJsonString(), devOrFeatRef));

            deviceConfigPageMock.Verify();
            pluginMock.Verify();
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void PostBackProcforGetConfiguration(bool showSubParameteredValuesAsHexId)
        {
            var (zwaveConnectionMock, pluginMock) = CreatePlugInWithZWaveConnection();

            var settingsFromIni = new Dictionary<string, string>
            {
                { SettingsPages.ShowSubParameteredValuesAsHexId, showSubParameteredValuesAsHexId.ToString()}
            };

            TestHelper.SetupHsControllerAndSettings(pluginMock, settingsFromIni);
            pluginMock.Object.InitIO();

            int parameterValue = 93745;
            var input = new
            {
                operation = "GET",
                homeId = "23434",
                nodeId = (byte)232,
                parameter = (byte)23
            };
            zwaveConnectionMock
                 .Setup(x => x.GetConfiguration(input.homeId, input.nodeId, input.parameter, It.IsAny<CancellationToken>()))
                 .Returns(Task.FromResult(parameterValue));

            var value = pluginMock.Object.PostBackProc("Update", JsonSerializer.Serialize(input), "user", 0);

            var result = JsonSerializer.Deserialize<ZWaveParameterGetResult>(value);
            Assert.IsNull(result.ErrorMessage);
            Assert.AreEqual(result.Value, parameterValue);
            Assert.AreEqual(result.ShowSubParameteredValuesAsHex, showSubParameteredValuesAsHexId);
        }

        [TestMethod]
        public void PostBackProcforNonHandled()
        {
            var plugin = new PlugIn();
            Assert.AreEqual(plugin.PostBackProc("Random", "data", "user", 0), string.Empty);
        }

        [DataTestMethod]
        [DataRow("GET2", "23434", (byte)232, (byte)23)]
        [DataRow("GET", "23434", null, (byte)23)]
        [DataRow("GET", null, (byte)232, (byte)23)]
        [DataRow("GET", null, (byte)232, null)]
        public void PostBackProcforUpdatePageWithError(string operation, string homeId, byte? nodeId, byte? parameter)
        {
            var input = new { operation, homeId, nodeId, parameter };

            var plugin = new PlugIn();
            var value = plugin.PostBackProc("Update", JsonSerializer.Serialize(input), "user", 0);

            var result = JsonSerializer.Deserialize<ZWaveParameterGetResult>(value);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsNull(result.Value);
        }

        [TestMethod]
        public void PostBackProcGetConfigurationFailure()
        {
            var (zwaveConnectionMock, pluginMock) = CreatePlugInWithZWaveConnection();
            var input = new
            {
                operation = "GET",
                homeId = "23434",
                nodeId = (byte)232,
                parameter = (byte)23
            };
            zwaveConnectionMock
                 .Setup(x => x.GetConfiguration(input.homeId, input.nodeId, input.parameter, It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new ZWaveGetConfigurationFailedException());

            var value = pluginMock.Object.PostBackProc("Update", JsonSerializer.Serialize(input), "user", 0);

            var result = JsonSerializer.Deserialize<ZWaveParameterGetResult>(value);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsNull(result.Value);
        }

        [TestMethod]
        public void SupportsDeviceConfigPage()
        {
            var plugin = new PlugIn();
            Assert.IsTrue(plugin.SupportsConfigDeviceAll);
            Assert.AreEqual(plugin.Id, PlugInData.PlugInId);
            Assert.AreEqual(plugin.Name, PlugInData.PlugInName);
        }

        private static (Mock<PlugIn>, Mock<IDeviceConfigPage>) CreatePlugInAndDeviceConfig(int devOrFeatRef)
        {
            var pluginMock = TestHelper.CreatePlugInMock();
            var deviceConfigPageMock = new Mock<IDeviceConfigPage>(MockBehavior.Strict);
            pluginMock.Protected()
               .Setup<IDeviceConfigPage>("CreateDeviceConfigPage", devOrFeatRef)
               .Returns(deviceConfigPageMock.Object);
            return (pluginMock, deviceConfigPageMock);
        }


        [TestMethod]
        public void InitFirstTime()
        {
            var plugin = TestHelper.CreatePlugInMock();
            TestHelper.SetupHsControllerAndSettings(plugin, new Dictionary<string, string>());
            Assert.IsTrue(plugin.Object.InitIO());
            plugin.Object.ShutdownIO();
        }


        private static (Mock<IZWaveConnection>, Mock<PlugIn>) CreatePlugInWithZWaveConnection()
        {
            var zwaveConnectionMock = new Mock<IZWaveConnection>(MockBehavior.Strict);
            var pluginMock = TestHelper.CreatePlugInMock();
            pluginMock.Protected()
               .Setup<IZWaveConnection>("CreateZWaveConnection")
               .Returns(zwaveConnectionMock.Object)
               .Verifiable();
            return (zwaveConnectionMock, pluginMock);
        }
    }
}