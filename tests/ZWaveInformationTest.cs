using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Devices;
using Hspi;
using Hspi.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class ZWaveInformationTest
    {
        [TestMethod]
        public async Task GetConfiguration()
        {
            string homeId = "4567f";
            byte nodeId = 234;
            byte param = 45;
            int value = 42;

            var mock = new Mock<IHsController>();
            mock.Setup(x => x.LegacyPluginFunction(ZWaveInterface, string.Empty, "Configuration_Get", new object[3] { homeId, nodeId, param }))
                .Returns(value);

            mock.Setup(x => x.LegacyPluginPropertyGet(ZWaveInterface, string.Empty, "Configuration_Get_Result"))
                .Returns(true);

            ZWaveConnection connection = new(mock.Object);
            Assert.AreEqual(await connection.GetConfiguration(homeId, nodeId, param), value);
        }

        [TestMethod]
        public async Task GetConfigurationFails()
        {
            string homeId = "4567f";
            byte nodeId = 234;
            byte param = 45;

            var mock = new Mock<IHsController>();
            mock.Setup(x => x.LegacyPluginFunction(ZWaveInterface, string.Empty, "Configuration_Get", new object[3] { homeId, nodeId, param }))
                .Returns(34);

            mock.Setup(x => x.LegacyPluginPropertyGet(ZWaveInterface, string.Empty, "Configuration_Get_Result"))
                .Returns(false);

            ZWaveConnection connection = new(mock.Object);
            await Assert.ThrowsExceptionAsync<ZWaveGetConfigurationFailedException>(() => connection.GetConfiguration(homeId, nodeId, param));
        }

        [TestMethod]
        public async Task GetConfigurationFailWithHomeSeerException()
        {
            string homeId = "4567f";
            byte nodeId = 234;
            byte param = 45;

            var mock = new Mock<IHsController>();
            mock.Setup(x => x.LegacyPluginFunction(ZWaveInterface, string.Empty, "Configuration_Get", new object[3] { homeId, nodeId, param }))
                .Throws(new TimeoutException());

            ZWaveConnection connection = new(mock.Object);
            await Assert.ThrowsExceptionAsync<ZWaveGetConfigurationFailedException>(() => connection.GetConfiguration(homeId, nodeId, param));
        }

        [TestMethod]
        public void GetDeviceZWaveData()
        {
            int deviceRef = 9384;
            var zwaveData = new ZWaveData(0x84, 74, 234, 234, "345", new Version(5, 3));

            var mock = CreateMockForHsController(deviceRef, zwaveData);

            ZWaveConnection connection = new ZWaveConnection(mock.Object);
            Assert.AreEqual(connection.GetDeviceZWaveData(deviceRef), zwaveData);
        }

        [TestMethod]
        public void GetDeviceZWaveDataForSingleStringDigitFirmware()
        {
            int deviceRef = 9384;
            var zwaveData = new ZWaveData(0x84, 74, 234, 234, "345", new Version(5, 0));

            var mock = new Mock<IHsController>();
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.Interface)).Returns(ZWaveInterface);

            var plugInExtraData = new PlugExtraData();
            plugInExtraData.AddNamed("manufacturer_id", zwaveData.ManufactureId.ToString());
            plugInExtraData.AddNamed("manufacturer_prod_id", zwaveData.ProductId.ToString());
            plugInExtraData.AddNamed("manufacturer_prod_type", zwaveData.ProductType.ToString());
            plugInExtraData.AddNamed("node_id", zwaveData.NodeId.ToString());
            plugInExtraData.AddNamed("homeid", zwaveData.HomeId);
            plugInExtraData.AddNamed("node_version_app", "5");

            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.PlugExtraData)).Returns(plugInExtraData);

            ZWaveConnection connection = new(mock.Object);
            Assert.AreEqual(connection.GetDeviceZWaveData(deviceRef), zwaveData);
        }

        [TestMethod]
        public void GetDeviceZWaveDataThrowsForInValidPlugInData()
        {
            int deviceRef = 9384;
            var mock = new Mock<IHsController>();
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.Interface)).Returns(ZWaveInterface);

            var plugInExtraData = new PlugExtraData();
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.PlugExtraData)).Returns(plugInExtraData);

            ZWaveConnection connection = new ZWaveConnection(mock.Object);
            Assert.ThrowsException<ZWavePlugInDataInvalidException>(() => connection.GetDeviceZWaveData(deviceRef));
        }

        [TestMethod]
        public void GetDeviceZWaveDataThrowsForNonZWaveDevice()
        {
            int deviceRef = 9384;
            var mock = new Mock<IHsController>();
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.Interface)).Returns("Something");

            ZWaveConnection connection = new ZWaveConnection(mock.Object);
            Assert.ThrowsException<NotAZWaveDeviceException>(() => connection.GetDeviceZWaveData(deviceRef));
        }

        [TestMethod]
        public void GetDeviceZWaveDataThrowsForNoPlugInData()
        {
            int deviceRef = 9384;
            var mock = new Mock<IHsController>();
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.Interface)).Returns(ZWaveInterface);
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.PlugExtraData)).Returns(null);

            ZWaveConnection connection = new ZWaveConnection(mock.Object);
            Assert.ThrowsException<ZWavePlugInDataInvalidException>(() => connection.GetDeviceZWaveData(deviceRef));
        }

        private static Mock<IHsController> CreateMockForHsController(int deviceRef, ZWaveData zwaveData)
        {
            var mock = new Mock<IHsController>();
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.Interface)).Returns(ZWaveInterface);

            var plugInExtraData = new PlugExtraData();
            plugInExtraData.AddNamed("manufacturer_id", zwaveData.ManufactureId.ToString());
            plugInExtraData.AddNamed("manufacturer_prod_id", zwaveData.ProductId.ToString());
            plugInExtraData.AddNamed("manufacturer_prod_type", zwaveData.ProductType.ToString());
            plugInExtraData.AddNamed("node_id", zwaveData.NodeId.ToString());
            plugInExtraData.AddNamed("homeid", zwaveData.HomeId);
            plugInExtraData.AddNamed("node_version_app", zwaveData.Firmware.ToString());

            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.PlugExtraData)).Returns(plugInExtraData);
            return mock;
        }

        private const string ZWaveInterface = "Z-Wave";
    }
}