using Hspi.Exceptions;
using Hspi.OpenZWaveDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Contrib.HttpClient;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class OpenZWaveDBInformationTest
    {
        [TestMethod]
        public async Task ErrorThrowsException()
        {
            var handler = new Mock<HttpMessageHandler>();
            var httpclient = handler.CreateClient();

            handler.SetupAnyRequest().ReturnsResponse(HttpStatusCode.NotFound);

            var obj = new OpenZWaveDBInformation(69, 0, 0, new Version(0, 0, 0), httpclient);
            await Assert.ThrowsExceptionAsync<Exception>(() => obj.Update(CancellationToken.None)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DownloadForSingleDevice()
        {
            var handler = new Mock<HttpMessageHandler>();
            var httpclient = handler.CreateClient();

            handler.SetupRequest(HttpMethod.Get, "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x0086%200003:0006")
                               .ReturnsResponse(Resource.AeonLabsOpenZWaveDBDeviceListJson, "application/json");

            handler.SetupRequest(HttpMethod.Get, "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=75")
                               .ReturnsResponse(Resource.AeonLabsOpenZWaveDBDeviceJson, "application/json");

            var obj1 = new OpenZWaveDBInformation(134, 3, 6, new Version(1, 43, 0), httpclient);
            await obj1.Update(CancellationToken.None);
            Assert.AreEqual(obj1.Data.Id, "75");

            handler.Verify();
        }

        [TestMethod]
        public async Task CorrectFirmwareIsSelected()
        {
            var handler = new Mock<HttpMessageHandler>();
            var httpclient = handler.CreateClient();

            handler.SetupRequest(HttpMethod.Get, "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x000C%204447:3036")
                               .ReturnsResponse(Resource.HomeseerDimmerOpenZWaveDBDeviceListJson, "application/json");

            handler.SetupRequest(HttpMethod.Get, "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=1040")
                               .ReturnsResponse(Resource.HomeseerDimmerOpenZWaveDBFullJson, "application/json");

            handler.SetupRequest(HttpMethod.Get, " https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=806")
                               .ReturnsResponse(Resource.HomeseerDimmerOpenZWaveDBFullOlderJson, "application/json");

            var obj1 = new OpenZWaveDBInformation(12, 17479, 12342, new Version(5, 9, 0), httpclient);
            await obj1.Update(CancellationToken.None);
            Assert.AreEqual(obj1.Data.Id, "806");

            var obj2 = new OpenZWaveDBInformation(12, 17479, 12342, new Version(5, 14, 0), httpclient);
            await obj2.Update(CancellationToken.None);
            Assert.AreEqual(obj2.Data.Id, "1040");

            handler.Verify();
        }

        [TestMethod]
        public async Task FirstDeviceIsSelectedIfNoMatchingFirmware()
        {
            var handler = new Mock<HttpMessageHandler>();
            var httpclient = handler.CreateClient();

            handler.SetupRequest(HttpMethod.Get, "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x000C%204447:3036")
                               .ReturnsResponse(Resource.HomeseerDimmerOpenZWaveDBDeviceListJson, "application/json");

            handler.SetupRequest(HttpMethod.Get, " https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=806")
                               .ReturnsResponse(Resource.HomeseerDimmerOpenZWaveDBFullOlderJson, "application/json");

            var obj1 = new OpenZWaveDBInformation(12, 17479, 12342, new Version(5, 10, 0), httpclient);
            await obj1.Update(CancellationToken.None);
            Assert.AreEqual(obj1.Data.Id, "806");

            handler.Verify();
        }

        [TestMethod]
        public async Task NoDeviceThrowsException()
        {
            var handler = new Mock<HttpMessageHandler>();
            var httpclient = handler.CreateClient();

            handler.SetupRequest(HttpMethod.Get, "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x026E%204252:5A31")
                               .ReturnsResponse("{\"search_filter\":{\"manufacturer\":622,\"filter\":\"4252:5A31\"},\"total\":0,\"devices\":[]}", "application/json");

            var obj1 = new OpenZWaveDBInformation(622, 16978, 23089, new Version(11, 2, 0), httpclient);

            bool thrown = false;
            try
            {
                await obj1.Update(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                thrown = true;
                Assert.IsInstanceOfType(ex.InnerException, typeof(ShowErrorMessageException));
            }

            Assert.IsTrue(thrown);

            handler.Verify();
        }

        [TestMethod]
        public async Task ParameterWithSameIdAreGrouped()
        {
            var handler = new Mock<HttpMessageHandler>();
            var httpclient = handler.CreateClient();

            handler.SetupRequest(HttpMethod.Get, "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x000C%204447:3036")
                               .ReturnsResponse(Resource.HomeseerDimmerOpenZWaveDBDeviceListJson, "application/json");

            handler.SetupRequest(HttpMethod.Get, " https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=1040")
                               .ReturnsResponse(Resource.HomeseerDimmerOpenZWaveDBFullJson, "application/json");

            var obj2 = new OpenZWaveDBInformation(12, 17479, 12342, new Version(5, 14, 0), httpclient);
            await obj2.Update(CancellationToken.None);

            Assert.IsNotNull(obj2.Data.Parameters);
            Assert.AreEqual(obj2.Data.Parameters.Count, 15);

            Assert.AreEqual(obj2.Data.Parameters[14].ParameterId, 31);
            Assert.AreEqual(obj2.Data.Parameters[14].HasSubParameters, true);
            Assert.AreEqual(obj2.Data.Parameters[14].SubParameters.Count, 6);

            handler.Verify();
        }
    }
}