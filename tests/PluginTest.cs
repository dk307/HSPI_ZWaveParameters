using HomeSeer.Jui.Views;
using Hspi;
using Hspi.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
            CreatePlugInAndDeviceConfig(devOrFeatRef, out var pluginMock, out var deviceConfigPageMock);

            var page = PageFactory.CreateDeviceConfigPage("id", "name");
            page = page.WithInput("id1", "name2", "3", HomeSeer.Jui.Types.EInputType.Decimal);

            deviceConfigPageMock.Setup(x => x.BuildConfigPage(Moq.It.IsAny<CancellationToken>()));
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
            var pluginMock = CreatePlugInMock();

            var deviceConfigPageMock = new Mock<IDeviceConfigPage>();

            pluginMock.Protected()
               .Setup<IDeviceConfigPage>("CreateDeviceConfigPage", devOrFeatRef)
               .Returns(deviceConfigPageMock.Object);

            string errorMessage = "sdfsd dfgdfg erter";
            deviceConfigPageMock.Setup(x => x.BuildConfigPage(Moq.It.IsAny<CancellationToken>()))
                .Throws(new Exception(errorMessage));

            var plugIn = pluginMock.Object;
            string pageJson = plugIn.GetJuiDeviceConfigPage(devOrFeatRef);

            JObject result = JsonConvert.DeserializeObject<JObject>(pageJson);

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
            CreatePlugInWithZWaveConnection(out var zwaveConnectionMock, out var pluginMock);

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
            var pluginMock = CreatePlugInMock();
            var page = PageFactory.CreateDeviceConfigPage("id", "name");
            Assert.ThrowsException<ShowErrorMessageException>(() => pluginMock.Object.SaveJuiDeviceConfigPage(page.Page.ToJsonString(), 10));
        }

        [TestMethod]
        public void OnDeviceConfigChangeThrowsError()
        {
            int devOrFeatRef = 10;
            CreatePlugInAndDeviceConfig(devOrFeatRef, out var pluginMock, out var deviceConfigPageMock);

            var page = PageFactory.CreateDeviceConfigPage("id", "name");
            page = page.WithInput("id1", "name2", "3", HomeSeer.Jui.Types.EInputType.Decimal);

            deviceConfigPageMock.Setup(x => x.BuildConfigPage(It.IsAny<CancellationToken>()));
            deviceConfigPageMock.Setup(x => x.GetPage()).Returns(page.Page);

            var plugIn = pluginMock.Object;
            plugIn.GetJuiDeviceConfigPage(devOrFeatRef);

            deviceConfigPageMock.Verify();

            page.Page.Views[0].UpdateValue("34");

            deviceConfigPageMock.Setup(x => x.OnDeviceConfigChange(It.IsAny<Page>()))
                                .Throws(new ZWaveSetConfigurationFailedException());

            Assert.ThrowsException<ShowErrorMessageException>(
                            () => plugIn.SaveJuiDeviceConfigPage(page.Page.ToJsonString(), devOrFeatRef));

            deviceConfigPageMock.Verify();
            pluginMock.Verify();
        }

        [TestMethod]
        public void OnDeviceConfigChangeWithInput()
        {
            int devOrFeatRef = 10;
            CreatePlugInAndDeviceConfig(devOrFeatRef, out var pluginMock, out var deviceConfigPageMock);

            var page = PageFactory.CreateDeviceConfigPage("id", "name");
            page = page.WithInput("id1", "name2", "3", HomeSeer.Jui.Types.EInputType.Decimal);
            page = page.WithDropDownSelectList("id2", "name2", new List<string> { "0", "1", "2" });

            deviceConfigPageMock.Setup(x => x.BuildConfigPage(It.IsAny<CancellationToken>()));
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

        [TestMethod]
        public void PostBackProcforGetConfiguration()
        {
            CreatePlugInWithZWaveConnection(out var zwaveConnectionMock, out var pluginMock);

            int parameterValue = 93745;
            var input = new
            {
                operation = "GET",
                homeId = "23434",
                nodeId = (byte)232,
                parameter = (byte)23
            };
            zwaveConnectionMock
                 .Setup(x => x.GetConfiguration(input.homeId, input.nodeId, input.parameter))
                 .Returns(Task.FromResult(parameterValue));

            var value = pluginMock.Object.PostBackProc("Update", JsonConvert.SerializeObject(input), "user", 0);

            var result = JsonConvert.DeserializeObject<ZWaveParameterGetResult>(value);
            Assert.IsNull(result.ErrorMessage);
            Assert.AreEqual(result.Value, parameterValue);
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
            var value = plugin.PostBackProc("Update", JsonConvert.SerializeObject(input), "user", 0);

            var result = JsonConvert.DeserializeObject<ZWaveParameterGetResult>(value);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsNull(result.Value);
        }

        [TestMethod]
        public void PostBackProcGetConfigurationFailure()
        {
            CreatePlugInWithZWaveConnection(out var zwaveConnectionMock, out var pluginMock);
            var input = new
            {
                operation = "GET",
                homeId = "23434",
                nodeId = (byte)232,
                parameter = (byte)23
            };
            zwaveConnectionMock
                 .Setup(x => x.GetConfiguration(input.homeId, input.nodeId, input.parameter))
                 .ThrowsAsync(new ZWaveGetConfigurationFailedException());

            var value = pluginMock.Object.PostBackProc("Update", JsonConvert.SerializeObject(input), "user", 0);

            var result = JsonConvert.DeserializeObject<ZWaveParameterGetResult>(value);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsNull(result.Value);
        }

        [TestMethod]
        public void SupportsDeviceConfigPage()
        {
            var plugin = new PlugIn();
            Assert.AreEqual(plugin.SupportsConfigDeviceAll, true);
            Assert.AreEqual(plugin.Id, PlugInData.PlugInId);
            Assert.AreEqual(plugin.Name, PlugInData.PlugInName);
        }
        private static void CreatePlugInAndDeviceConfig(int devOrFeatRef, out Mock<PlugIn> pluginMock, out Mock<IDeviceConfigPage> deviceConfigPageMock)
        {
            pluginMock = CreatePlugInMock();
            deviceConfigPageMock = new Mock<IDeviceConfigPage>();
            pluginMock.Protected()
               .Setup<IDeviceConfigPage>("CreateDeviceConfigPage", devOrFeatRef)
               .Returns(deviceConfigPageMock.Object);
        }

        private static Mock<PlugIn> CreatePlugInMock()
        {
            return new Mock<PlugIn>()
            {
                CallBase = true,
            };
        }
        private static void CreatePlugInWithZWaveConnection(out Mock<IZWaveConnection> zwaveConnectionMock,
                                                            out Mock<PlugIn> pluginMock)
        {
            zwaveConnectionMock = new Mock<IZWaveConnection>();
            pluginMock = CreatePlugInMock();
            pluginMock.Protected()
               .Setup<IZWaveConnection>("CreateZWaveConnection")
               .Returns(zwaveConnectionMock.Object)
               .Verifiable();
        }
    }
}