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
        public static IEnumerable<object[]> GetDeviceZWaveDataData()
        {
            yield return new object[] { new ZWaveData(0x84, 74, 20, 234, "3425", new Version(5, 0), false),
                                                      0x84.ToString(), 74.ToString(), 20.ToString(), 234.ToString(), "3425", "5.0", "0", "0" ,"3", "5"};
            // single digit firmware version
            yield return new object[] { new ZWaveData(0x84, 74, 23, 234, "345", new Version(5, 0), true),
                                                      0x84.ToString(), 74.ToString(), 23.ToString(), 234.ToString(), "345", "5", 0x80.ToString(), "0","3", "5" };
            yield return new object[] { new ZWaveData(0x84, 74, 23, 234, "345", new Version(5, 0), true),
                                                      0x84.ToString(), 74.ToString(), 23.ToString(), 234.ToString(), "345", "5.0", 0x80.ToString(), "0" ,"3", "5"};
            yield return new object[] { new ZWaveData(0x84, 70, 23, 234, "345", new Version(5, 0), true),
                                                      0x84.ToString(), 70.ToString(), 23.ToString(), 234.ToString(), "345", "5.0", "0", 0x20.ToString() ,"3", "5"};
            yield return new object[] { new ZWaveData(0x80, 74, 23, 234, "345", new Version(5, 0), true),
                                                      0x80.ToString(), 74.ToString(), 23.ToString(), 234.ToString(), "345", "5.0", "0", 0x40.ToString() ,"3", "5"};
        }

        [TestMethod]
        public async Task GetConfiguration()
        {
            string homeId = "4567f";
            byte nodeId = 234;
            byte param = 45;
            int value = 42;

            var mock = new Mock<IHsController>(MockBehavior.Strict);
            TestHelper.SetupGetConfigurationInHsController(homeId, nodeId, param, value, mock);

            ZWaveConnection connection = new(mock.Object);
            Assert.AreEqual(await connection.GetConfiguration(homeId, nodeId, param), value);
        }

        [TestMethod]
        public async Task GetConfigurationFails()
        {
            string homeId = "4567f";
            byte nodeId = 234;
            byte param = 45;

            var mock = new Mock<IHsController>(MockBehavior.Strict);
            mock.Setup(x => x.LegacyPluginFunction(TestHelper.ZWaveInterface, string.Empty, "Configuration_Get", new object[3] { homeId, nodeId, param }))
                .Returns(34);

            mock.Setup(x => x.LegacyPluginPropertyGet(TestHelper.ZWaveInterface, string.Empty, "Configuration_Get_Result"))
                .Returns(false);

            ZWaveConnection connection = new(mock.Object);
            try
            {
                await connection.GetConfiguration(homeId, nodeId, param);
                Assert.Fail("No exception thrown");
            }
            catch (ZWaveGetConfigurationFailedException ex)
            {
                Assert.IsNull(ex.InnerException);
            }

            mock.Verify();
        }

        [TestMethod]
        public async Task GetConfigurationFailWithHomeSeerException()
        {
            string homeId = "4567f";
            byte nodeId = 234;
            byte param = 45;

            var mock = new Mock<IHsController>(MockBehavior.Strict);
            mock.Setup(x => x.LegacyPluginFunction(TestHelper.ZWaveInterface, string.Empty, "Configuration_Get", new object[3] { homeId, nodeId, param }))
                .Throws(new TimeoutException());

            mock.Setup(x => x.GetPluginVersionById(TestHelper.ZWaveInterface)).Returns("3.7");

            ZWaveConnection connection = new(mock.Object);
            try
            {
                await connection.GetConfiguration(homeId, nodeId, param);
                Assert.Fail("No exception thrown");
            }
            catch (ZWaveGetConfigurationFailedException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(TimeoutException));
            }

            mock.Verify();
        }

        [TestMethod]
        public async Task GetConfigurationFailWithNoZwavePlugIn()
        {
            string homeId = "4567f";
            byte nodeId = 234;
            byte param = 45;

            var mock = new Mock<IHsController>(MockBehavior.Strict);
            mock.Setup(x => x.LegacyPluginFunction(TestHelper.ZWaveInterface, string.Empty, "Configuration_Get", new object[3] { homeId, nodeId, param }))
                .Throws(new NullReferenceException());

            mock.Setup(x => x.GetPluginVersionById(TestHelper.ZWaveInterface)).Throws(new KeyNotFoundException());

            ZWaveConnection connection = new(mock.Object);
            await Assert.ThrowsExceptionAsync<ZWavePluginNotRunningException>(() => connection.GetConfiguration(homeId, nodeId, param));

            mock.Verify();
        }

        [DataTestMethod]
        [DynamicData(nameof(GetDeviceZWaveDataData), DynamicDataSourceType.Method)]
        public void GetDeviceZWaveData(ZWaveData zwaveData, string manufactureId, string productId, string productType,
                                       string nodeId, string homeId, string firmware, string capability, string security,
                                       string basicType, string genericType)
        {
            int deviceRef = 9384;
            var mock = CreateMockForHsController(deviceRef, manufactureId, productId, productType, nodeId, homeId,
                                                 firmware, capability, security, basicType, genericType);

            ZWaveConnection connection = new(mock.Object);
            Assert.AreEqual(connection.GetDeviceZWaveData(deviceRef), zwaveData);
        }

        [DataTestMethod]
        [DataRow("1", null)]
        [DataRow("2", null)]
        [DataRow(null, "1")]
        [DataRow(null, "2")]
        [DataRow("1", "2")]
        public void GetDeviceZWaveDataThrowsForController(string basictype, string genericType)
        {
            int deviceRef = 9384;
            var mock = CreateMockForHsController(deviceRef, basicType: basictype, genericType: genericType);

            ZWaveConnection connection = new(mock.Object);
            Assert.ThrowsException<ZWavePlugIsControllerException>(() => connection.GetDeviceZWaveData(deviceRef));
        }

        [TestMethod]
        public void GetDeviceZWaveDataThrowsForInValidPlugInData()
        {
            int deviceRef = 9384;
            var mock = new Mock<IHsController>(MockBehavior.Strict);
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.Interface)).Returns(TestHelper.ZWaveInterface);

            var plugInExtraData = new PlugExtraData();
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.PlugExtraData)).Returns(plugInExtraData);

            ZWaveConnection connection = new(mock.Object);
            Assert.ThrowsException<ZWavePlugInDataInvalidException>(() => connection.GetDeviceZWaveData(deviceRef));
        }

        [TestMethod]
        public void GetDeviceZWaveDataThrowsForNonZWaveDevice()
        {
            int deviceRef = 9384;
            var mock = new Mock<IHsController>(MockBehavior.Strict);
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.Interface)).Returns("Something");

            ZWaveConnection connection = new(mock.Object);
            Assert.ThrowsException<NotAZWaveDeviceException>(() => connection.GetDeviceZWaveData(deviceRef));
        }

        [TestMethod]
        public void GetDeviceZWaveDataThrowsForNoPlugInData()
        {
            int deviceRef = 9384;
            var mock = new Mock<IHsController>(MockBehavior.Strict);
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.Interface)).Returns(TestHelper.ZWaveInterface);
            mock.Setup(x => x.GetPropertyByRef(deviceRef, EProperty.PlugExtraData)).Returns(null);

            ZWaveConnection connection = new(mock.Object);
            Assert.ThrowsException<ZWavePlugInDataInvalidException>(() => connection.GetDeviceZWaveData(deviceRef));
        }

        [DataTestMethod]
        [DataRow("Success", false, false)]
        [DataRow("Queued", false, false)]
        [DataRow(null, true, true)]
        [DataRow("Errored", true, true)]
        [DataRow("Unknown", true, true)]
        public void SetConfiguration(string result, bool isError, bool expectCheckPlugInCall)
        {
            string homeId = "4567f";
            byte nodeId = 234;
            byte param = 45;
            int value = 42;
            byte size = 3;

            var mock = new Mock<IHsController>(MockBehavior.Strict);
            mock.Setup(x => x.LegacyPluginFunction(TestHelper.ZWaveInterface, string.Empty, "SetDeviceParameterValue",
                                                   new object[5] { homeId, nodeId, param, size, value }))
                .Returns(result);

            ZWaveConnection connection = new(mock.Object);
            if (!isError)
            {
                connection.SetConfiguration(homeId, nodeId, param, size, value);
            }
            else
            {
                try
                {
                    if (expectCheckPlugInCall)
                    {
                        mock.Setup(x => x.GetPluginVersionById(TestHelper.ZWaveInterface)).Returns("3.7");
                    }

                    connection.SetConfiguration(homeId, nodeId, param, size, value);
                    Assert.Fail("No exception thrown");
                }
                catch (ZWaveSetConfigurationFailedException ex)
                {
                    Assert.IsNull(ex.InnerException);
                }
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

            var mock = new Mock<IHsController>(MockBehavior.Strict);
            mock.Setup(x => x.LegacyPluginFunction(TestHelper.ZWaveInterface, string.Empty, "SetDeviceParameterValue",
                                                   new object[5] { homeId, nodeId, param, size, value }))
                .Throws(new TimeoutException());

            ZWaveConnection connection = new(mock.Object);

            try
            {
                connection.SetConfiguration(homeId, nodeId, param, size, value);
                Assert.Fail("No exception thrown");
            }
            catch (ZWaveSetConfigurationFailedException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(TimeoutException));
            }
        }

        [TestMethod]
        public void SetConfigurationWithZWavePlugInRunning()
        {
            string homeId = "4567f";
            byte nodeId = 234;
            byte param = 45;
            int value = 42;
            byte size = 3;

            var mock = new Mock<IHsController>(MockBehavior.Strict);
            mock.Setup(x => x.LegacyPluginFunction(TestHelper.ZWaveInterface, string.Empty, "SetDeviceParameterValue",
                                                   new object[5] { homeId, nodeId, param, size, value }))
                .Returns(null);

            ZWaveConnection connection = new(mock.Object);

            mock.Setup(x => x.GetPluginVersionById(TestHelper.ZWaveInterface)).Throws(new Exception());

            Assert.ThrowsException<ZWavePluginNotRunningException>(() => connection.SetConfiguration(homeId, nodeId, param, size, value));
        }


        private static Mock<IHsController> CreateMockForHsController(int deviceRef,
                                                                     string manufactureId = null,
                                                                     string productId = null,
                                                                     string productType = null,
                                                                     string nodeId = null,
                                                                     string homeId = null,
                                                                     string firmware = null,
                                                                     string capability = null,
                                                                     string security = null,
                                                                     string basicType = null,
                                                                     string genericType = null)
        {
            var mock = new Mock<IHsController>(MockBehavior.Strict);
            TestHelper.SetupZWaveDataInHsControllerMock(mock, deviceRef, manufactureId, productId, productType,
                                                        nodeId, homeId, firmware, capability, security, basicType,
                                                        genericType);
            return mock;
        }
    }
}