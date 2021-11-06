using Hspi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

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

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void HasJuiDeviceConfigPageChecksZWave(bool supports)
        {
            var zwaveConnectionMock = new Mock<IZWaveConnection>();
            var pluginMock = new Mock<PlugIn>()
            {
                CallBase = true,
            };

            pluginMock.Protected()
               .Setup<IZWaveConnection>("CreateZWaveConnection")
               .Returns(zwaveConnectionMock.Object)
               .Verifiable();

            int devOrFeatRef = 10;
            zwaveConnectionMock.Setup(x => x.IsZwaveDevice(devOrFeatRef)).Returns(supports);

            var plugIn = pluginMock.Object;

            bool expected = plugIn.HasJuiDeviceConfigPage(devOrFeatRef);
            Assert.AreEqual(expected, supports);

            pluginMock.Verify();
        }
    }
}