using HomeSeer.Jui.Views;
using Hspi;
using Hspi.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using static Hspi.PlugIn;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class PlugInTest
    {
        [TestMethod]
        public void SupportsDeviceConfigPage()
        {
            var plugin = new PlugIn();
            Assert.AreEqual(plugin.SupportsConfigDeviceAll, true);
            Assert.AreEqual(plugin.Id, PlugInData.PlugInId);
            Assert.AreEqual(plugin.Name, PlugInData.PlugInName);
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
        public void GetJuiDeviceConfigPage()
        {
            int devOrFeatRef = 8374;
            var pluginMock = new Mock<PlugIn>()
            {
                CallBase = true,
            };

            var deviceConfigPageMock = new Mock<IDeviceConfigPage>();

            pluginMock.Protected()
               .Setup<IDeviceConfigPage>("CreateDeviceConfigPage", devOrFeatRef)
               .Returns(deviceConfigPageMock.Object);

            var page = PageFactory.CreateDeviceConfigPage("id", "name");

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
            var pluginMock = new Mock<PlugIn>()
            {
                CallBase = true,
            };

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

        private static void CreatePlugInWithZWaveConnection(out Mock<IZWaveConnection> zwaveConnectionMock,
                                                            out Mock<PlugIn> pluginMock)
        {
            zwaveConnectionMock = new Mock<IZWaveConnection>();
            pluginMock = new Mock<PlugIn>()
            {
                CallBase = true,
            };
            pluginMock.Protected()
               .Setup<IZWaveConnection>("CreateZWaveConnection")
               .Returns(zwaveConnectionMock.Object)
               .Verifiable();
        }
    }
}