using Hspi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
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
        public void PostBackProcforUpdatePage()
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

        private static void CreatePlugInWithZWaveConnection(out Mock<IZWaveConnection> zwaveConnectionMock, out Mock<PlugIn> pluginMock)
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