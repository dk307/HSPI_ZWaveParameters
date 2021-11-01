using HomeSeer.Jui.Views;
using Hspi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Contrib.HttpClient;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class DeviceConfigPageTest
    {
        [TestMethod]
        public async Task SupportsDeviceConfigPage()
        {
            int deviceRef = 34;
            var httpclient = CreateMockHttpClient();

            var zwaveData = new ZWaveData(

               ManufactureId: 0x0086,
               ProductId: 6,
               ProductType: 3,
               NodeId: 67,
               HomeId: "485F5",
               Firmware: new Version(5, 0)
           );

            var mock = new Mock<IZWaveConnection>();
            mock.Setup(x => x.GetDeviceZWaveData(deviceRef)).Returns(zwaveData);

            var deviceConfigPage = new DeviceConfigPage(mock.Object, deviceRef, httpclient);
            var page = await deviceConfigPage.BuildConfigPage(CancellationToken.None);


            Assert.AreEqual(page.Views.Count, 5);

            // verify first
            Assert.IsInstanceOfType(page.Views[0], typeof(LabelView));

            Mock.VerifyAll(mock);
        }

        private static HttpClient CreateMockHttpClient()
        {
            var handler = new Mock<HttpMessageHandler>();
            var httpclient = handler.CreateClient();

            handler.SetupRequest(HttpMethod.Get, "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x0086%200003:0006")
                               .ReturnsResponse(Resource.AeonLabsOpenZWaveDBDeviceListJson, "application/json");

            handler.SetupRequest(HttpMethod.Get, "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=75")
                               .ReturnsResponse(Resource.AeonLabsOpenZWaveDBDeviceJson, "application/json");
            return httpclient;
        }
    }
}