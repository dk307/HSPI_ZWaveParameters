using Hspi.Exceptions;
using Hspi.OpenZWaveDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HSPI_ZWaveParametersTest
{

    [TestClass]
    public class OnlineOpenZWaveDatabaseTest
    {
        [TestMethod]
        public async Task CorrectFirmwareIsSelected()
        {
            var mock = new Mock<IHttpQueryMaker>(MockBehavior.Strict);

            TestHelper.SetupRequest(mock, "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x000C%204447:3036",
                                    Resource.HomeseerDimmerOpenZWaveDBDeviceListJson);

            TestHelper.SetupRequest(mock, "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=806",
                                    Resource.HomeseerDimmerOpenZWaveDBFullOlderJson);

            var obj1 = new OnlineOpenZWaveDatabase(12, 17479, 12342, new Version(5, 9, 0), mock.Object);
            var data1 = await obj1.Create(CancellationToken.None);
            Assert.AreEqual(data1.Id, 806);

            TestHelper.SetupRequest(mock, "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x000C%204447:3036",
                        Resource.HomeseerDimmerOpenZWaveDBDeviceListJson);

            TestHelper.SetupRequest(mock, "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=1040",
                        Resource.HomeseerDimmerOpenZWaveDBFullJson);

            var obj2 = new OnlineOpenZWaveDatabase(12, 17479, 12342, new Version(5, 14, 0), mock.Object);
            var data2 = await obj2.Create(CancellationToken.None);
            Assert.AreEqual(data2.Id, 1040);

            mock.Verify();
        }

        [TestMethod]
        public async Task DownloadForSingleDevice()
        {
            var mock = new Mock<IHttpQueryMaker>(MockBehavior.Strict);

            TestHelper.SetupRequest(mock, "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x0086%200003:0006",
                                    Resource.AeonLabsOpenZWaveDBDeviceListJson);

            TestHelper.SetupRequest(mock, "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=75",
                                    Resource.AeonLabsOpenZWaveDBDeviceJson);

            var obj1 = new OnlineOpenZWaveDatabase(134, 3, 6, new Version(1, 43, 0), mock.Object);
            var data = await obj1.Create(CancellationToken.None);
            Assert.AreEqual(data.Id, 75);

            mock.Verify();
        }

        [TestMethod]
        public async Task FirstDeviceIsSelectedIfNoMatchingFirmware()
        {
            var mock = new Mock<IHttpQueryMaker>(MockBehavior.Strict);

            TestHelper.SetupRequest(mock, "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x000C%204447:3036",
                                    Resource.HomeseerDimmerOpenZWaveDBDeviceListJson);

            TestHelper.SetupRequest(mock, "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=806",
                                    Resource.HomeseerDimmerOpenZWaveDBFullOlderJson);

            var obj1 = new OnlineOpenZWaveDatabase(12, 17479, 12342, new Version(5, 10, 0), mock.Object);
            var data = await obj1.Create(CancellationToken.None);
            Assert.AreEqual(data.Id, 806);

            mock.Verify();
        }

        [TestMethod]
        public async Task HttpErrorThrowsException()
        {
            var mock = new Mock<IHttpQueryMaker>(MockBehavior.Strict);

            mock.Setup(x => x.GetUtf8JsonResponse(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .Throws(new HttpRequestException());

            var obj = new OnlineOpenZWaveDatabase(69, 0, 0, new Version(0, 0, 0), mock.Object);
            await Assert.ThrowsExceptionAsync<Exception>(() => obj.Create(CancellationToken.None)).ConfigureAwait(false);

            mock.Verify();
        }

        [TestMethod]
        public async Task NoDeviceThrowsException()
        {
            var httpQueryMock = new Mock<IHttpQueryMaker>(MockBehavior.Strict);

            TestHelper.SetupRequest(httpQueryMock, "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x026E%204252:5A31",
                                    "{\"search_filter\":{\"manufacturer\":622,\"filter\":\"4252:5A31\"},\"total\":0,\"devices\":[]}");

            var obj1 = new OnlineOpenZWaveDatabase(622, 16978, 23089, new Version(11, 2, 0), httpQueryMock.Object);

            bool thrown = false;
            try
            {
                await obj1.Create(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                thrown = true;
                Assert.IsInstanceOfType(ex.InnerException, typeof(ShowErrorMessageException));
                Assert.IsTrue(ex.InnerException.Message.Contains("Device not found"));
            }

            Assert.IsTrue(thrown);

            httpQueryMock.Verify();
        }

        [TestMethod]
        public async Task ParameterWithSameIdAreGrouped()
        {
            var mock = new Mock<IHttpQueryMaker>(MockBehavior.Strict);

            TestHelper.SetupRequest(mock, "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x000C%204447:3036",
                                    Resource.HomeseerDimmerOpenZWaveDBDeviceListJson);

            TestHelper.SetupRequest(mock, "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=1040",
                                    Resource.HomeseerDimmerOpenZWaveDBFullJson);

            var obj2 = new OnlineOpenZWaveDatabase(12, 17479, 12342, new Version(5, 14, 0), mock.Object);
            var data2 = await obj2.Create(CancellationToken.None);

            Assert.IsNotNull(data2.Parameters);
            Assert.AreEqual(data2.Parameters.Count, 15);

            Assert.AreEqual(data2.Parameters[14].ParameterId, 31);
            Assert.AreEqual(data2.Parameters[14].HasSubParameters, true);
            Assert.AreEqual(data2.Parameters[14].SubParameters.Count, 6);

            mock.Verify();
        }

        [DataTestMethod]
        [DataRow("{\"database_id\" : 7756, \"approved\": 0, \"deleted\": 0, \"label\": \"HS-WD200+\"}", DisplayName = "Non Approved")]
        [DataRow("{\"database_id\" : 7756, \"approved\": 1, \"deleted\": 1, \"label\": \"HS-WD200+\"}", DisplayName = "Deleted")] // Deleted
        public async Task VariousErrorInJsonThrowsException(string json)
        {
            var httpQueryMock = new Mock<IHttpQueryMaker>(MockBehavior.Strict);

            TestHelper.SetupRequest(httpQueryMock, "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x000C%204447:3036",
                                    Resource.HomeseerDimmerOpenZWaveDBDeviceListJson);

            TestHelper.SetupRequest(httpQueryMock, "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=806", json);

            var obj1 = new OnlineOpenZWaveDatabase(12, 17479, 12342, new Version(5, 10, 0), httpQueryMock.Object);

            bool thrown = false;
            try
            {
                await obj1.Create(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                thrown = true;
                Assert.IsInstanceOfType(ex.InnerException, typeof(ShowErrorMessageException));
                Assert.IsTrue(ex.InnerException.Message.Contains("Non-Approved Or Deleted Device"));
            }

            Assert.IsTrue(thrown);

            httpQueryMock.Verify();
        }
    }
}