using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Devices;
using Hspi;
using Hspi.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class ZWaveConnectionTest
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

            mock.Verify();
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

            mock.Setup(x => x.GetPluginVersionById(ZWaveInterface));

            ZWaveConnection connection = new(mock.Object);
            await Assert.ThrowsExceptionAsync<ZWaveGetConfigurationFailedException>(() => connection.GetConfiguration(homeId, nodeId, param));

            mock.Verify();
        }

        [TestMethod]
        public async Task GetConfigurationFailWithNoZwavePlugIn()
        {
            string homeId = "4567f";
            byte nodeId = 234;
            byte param = 45;

            var mock = new Mock<IHsController>();
            mock.Setup(x => x.LegacyPluginFunction(ZWaveInterface, string.Empty, "Configuration_Get", new object[3] { homeId, nodeId, param }))
                .Throws(new NullReferenceException());

            mock.Setup(x => x.GetPluginVersionById(ZWaveInterface)).Throws(new KeyNotFoundException());

            ZWaveConnection connection = new(mock.Object);
            await Assert.ThrowsExceptionAsync<ZWavePluginNotRunningException>(() => connection.GetConfiguration(homeId, nodeId, param));

            mock.Verify();
        }

        public static IEnumerable<object[]> GetDeviceZWaveDataData()
        {
            yield return new object[] { new ZWaveData(0x84, 74, 20, 234, "3425", new Version(5, 0), false),
                                                      0x84.ToString(), 74.ToString(), 20.ToString(), 234.ToString(), "3425", "5.0", "0", "0" };
            yield return new object[] { new ZWaveData(0x84, 74, 23, 234, "345", new Version(5, 0), true),
                                                      0x84.ToString(), 74.ToString(), 23.ToString(), 234.ToString(), "345", "5", 0x80.ToString(), "0" };
            yield return new object[] { new ZWaveData(0x84, 74, 23, 234, "345", new Version(5, 0), true),
                                                      0x84.ToString(), 74.ToString(), 23.ToString(), 234.ToString(), "345", "5.0", 0x80.ToString(), "0" };
            yield return new object[] { new ZWaveData(0x84, 70, 23, 234, "345", new Version(5, 0), true),
                                                      0x84.ToString(), 70.ToString(), 23.ToString(), 234.ToString(), "345", "5.0", "0", 0x20.ToString() };
            yield return new object[] { new ZWaveData(0x80, 74, 23, 234, "345", new Version(5, 0), true),
                                                      0x80.ToString(), 74.ToString(), 23.ToString(), 234.ToString(), "345", "5.0", "0", 0x40.ToString() };
        }

        [DataTestMethod]
        [DynamicData(nameof(GetDeviceZWaveDataData), DynamicDataSourceType.Method)]
        public void GetDeviceZWaveData(ZWaveData zwaveData, string manufactureId, string productId, string productType,
                                       string nodeId, string homeId, string firmware, string capability, string security)
        {
            int deviceRef = 9384;

            var mock = CreateMockForHsController(deviceRef, manufactureId, productId, productType, nodeId, homeId,
                                                 firmware, capability, security);

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

            ZWaveConnection connection = new(mock.Object);
            Assert.ThrowsException<ZWavePlugInDataInvalidException>(() => connection.GetDeviceZWaveData(deviceRef));
        }

        [TestMethod]
        public void GetDeviceZWaveDataThrowsForNonZWaveDevice()
        {
            int deviceRef = 9384;
            var mock = new Mock<IHsController>();
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.Interface)).Returns("Something");

            ZWaveConnection connection = new(mock.Object);
            Assert.ThrowsException<NotAZWaveDeviceException>(() => connection.GetDeviceZWaveData(deviceRef));
        }

        [TestMethod]
        public void GetDeviceZWaveDataThrowsForNoPlugInData()
        {
            int deviceRef = 9384;
            var mock = new Mock<IHsController>();
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.Interface)).Returns(ZWaveInterface);
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.PlugExtraData)).Returns(null);

            ZWaveConnection connection = new(mock.Object);
            Assert.ThrowsException<ZWavePlugInDataInvalidException>(() => connection.GetDeviceZWaveData(deviceRef));
        }

        private static Mock<IHsController> CreateMockForHsController(int deviceRef, ZWaveData zwaveData)
        {
            return CreateMockForHsController(deviceRef,
                                             zwaveData.ManufactureId.ToString(),
                                             zwaveData.ProductId.ToString(),
                                             zwaveData.ProductType.ToString(),
                                             zwaveData.HomeId.ToString(),
                                             zwaveData.NodeId.ToString(),
                                             zwaveData.Firmware.ToString(),
                                             zwaveData.Listening ? 0x80.ToString() : "0",
                                             "0");
        }

        private static Mock<IHsController> CreateMockForHsController(int deviceRef,
                                                                     string manufactureId,
                                                                     string productId,
                                                                     string productType,
                                                                     string nodeId,
                                                                     string homeId,
                                                                     string firmware,
                                                                     string capability,
                                                                     string security)
        {
            var mock = new Mock<IHsController>();
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.Interface)).Returns(ZWaveInterface);

            var plugInExtraData = new PlugExtraData();
            plugInExtraData.AddNamed("manufacturer_id", manufactureId);
            plugInExtraData.AddNamed("manufacturer_prod_id", productId);
            plugInExtraData.AddNamed("manufacturer_prod_type", productType);
            plugInExtraData.AddNamed("node_id", nodeId);
            plugInExtraData.AddNamed("homeid", homeId);
            plugInExtraData.AddNamed("node_version_app", firmware);
            plugInExtraData.AddNamed("capability", capability);
            plugInExtraData.AddNamed("security", security);

            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.PlugExtraData)).Returns(plugInExtraData);
            return mock;
        }

        [DataTestMethod]
        [DataRow("Success", false)]
        [DataRow("Queued", false)]
        [DataRow(null, true)]
        [DataRow("Errored", true)]
        [DataRow("Unknown", true)]
        public void SetConfiguration(string result, bool isError)
        {
            string homeId = "4567f";
            byte nodeId = 234;
            byte param = 45;
            int value = 42;
            byte size = 3;

            var mock = new Mock<IHsController>();
            mock.Setup(x => x.LegacyPluginFunction(ZWaveInterface, string.Empty, "SetDeviceParameterValue",
                                                   new object[5] { homeId, nodeId, param, size, value }))
                .Returns(result);

            ZWaveConnection connection = new(mock.Object);
            if (!isError)
            {
                connection.SetConfiguration(homeId, nodeId, param, size, value);
            }
            else
            {
                Assert.ThrowsException<ZWaveSetConfigurationFailedException>(
                    () => connection.SetConfiguration(homeId, nodeId, param, size, value));
            }
        }

        [TestMethod]
        public void SetConfigurationWithHomeSeerException()
        {
            string homeId = "4567f";
            byte nodeId = 234;
            byte param = 45;
            int value = 42;
            byte size = 3;

            var mock = new Mock<IHsController>();
            mock.Setup(x => x.LegacyPluginFunction(ZWaveInterface, string.Empty, "SetDeviceParameterValue",
                                                   new object[5] { homeId, nodeId, param, size, value }))
                .Throws(new TimeoutException());

            ZWaveConnection connection = new(mock.Object);

            Assert.ThrowsException<ZWaveSetConfigurationFailedException>(
                () => connection.SetConfiguration(homeId, nodeId, param, size, value));
        }

        private const string ZWaveInterface = "Z-Wave";
    }
}