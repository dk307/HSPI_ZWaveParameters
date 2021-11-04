using HomeSeer.Jui.Views;
using Hspi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Contrib.HttpClient;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class DeviceConfigPageTest
    {
        [TestMethod]
        public async Task SupportsDeviceConfigPageForAeonLabsSwitch()
        {
            int deviceRef = 34;
            var httpclient = CreateMockHttpClientForAeonLabsSwitch();
            var zwaveData = CreateAeonLabsZWaveData();
            var mock = SetupZWaveConnection(deviceRef, zwaveData);

            var deviceConfigPage = new DeviceConfigPage(mock.Object, deviceRef, httpclient);
            var page = await deviceConfigPage.BuildConfigPage(CancellationToken.None);

            Assert.AreEqual(page.Views.Count, 5);

            // verify header link
            VerifyHeader(deviceConfigPage, page.Views[0]);

            // verify refresh script block
            VeryHtmlValid(page.Views[1].ToHtml());

            // verify refresh all button
            VeryHtmlValid(page.Views[2].ToHtml());

            // verify parameters
            VerifyParametersView(deviceConfigPage, (GridView)page.Views[3]);

            // verify auto click refresh all
            VeryHtmlValid(page.Views[4].ToHtml());

            Mock.VerifyAll(mock);
        }

        [TestMethod]
        public async Task SupportsDeviceConfigPageForMinPage()
        {
            int deviceRef = 334;
            var handler = new Mock<HttpMessageHandler>();
            var httpclient = handler.CreateClient();

            handler.SetupRequest(HttpMethod.Get, "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x0086%200003:0006")
                               .ReturnsResponse(Resource.AeonLabsOpenZWaveDBDeviceListJson, "application/json");

            handler.SetupRequest(HttpMethod.Get, "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=75")
                               .ReturnsResponse("{ database_id:1034}", "application/json");

            var zwaveData = CreateAeonLabsZWaveData();
            var mock = SetupZWaveConnection(deviceRef, zwaveData);

            var deviceConfigPage = new DeviceConfigPage(mock.Object, deviceRef, httpclient);
            var page = await deviceConfigPage.BuildConfigPage(CancellationToken.None);

            Assert.AreEqual(page.Views.Count, 1);

            // verify header link
            VerifyHeader(deviceConfigPage, page.Views[0]);

            Mock.VerifyAll(mock, handler);
        }

        private static ZWaveData CreateAeonLabsZWaveData()
        {
            return new ZWaveData(

               ManufactureId: 0x0086,
               ProductId: 6,
               ProductType: 3,
               NodeId: 67,
               HomeId: "485F5",
               Firmware: new Version(5, 0)
           );
        }

        private static HttpClient CreateMockHttpClientForAeonLabsSwitch()
        {
            var handler = new Mock<HttpMessageHandler>();
            var httpclient = handler.CreateClient();

            handler.SetupRequest(HttpMethod.Get, "https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x0086%200003:0006")
                               .ReturnsResponse(Resource.AeonLabsOpenZWaveDBDeviceListJson, "application/json");

            handler.SetupRequest(HttpMethod.Get, "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=75")
                               .ReturnsResponse(Resource.AeonLabsOpenZWaveDBDeviceJson, "application/json");
            return httpclient;
        }

        private static Mock<IZWaveConnection> SetupZWaveConnection(int deviceRef, ZWaveData zwaveData)
        {
            var mock = new Mock<IZWaveConnection>();
            mock.Setup(x => x.GetDeviceZWaveData(deviceRef)).Returns(zwaveData);
            return mock;
        }

        private static void VerifyHeader(DeviceConfigPage deviceConfigPage, AbstractView view)
        {
            Assert.IsInstanceOfType(view, typeof(LabelView));
            string labelHtml = view.ToHtml();

            HtmlAgilityPack.HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(labelHtml);
            Assert.AreEqual(htmlDocument.ParseErrors.Count(), 0);

            var node = htmlDocument.DocumentNode.SelectSingleNode("//*/a");

            Assert.IsNotNull(node);
            Assert.IsTrue(node.InnerHtml.Contains(deviceConfigPage.Data.DisplayFullName()));
            Assert.AreEqual(node.Attributes["href"].Value, deviceConfigPage.Data.WebUrl.ToString());
        }

        private static void VerifyParametersView(DeviceConfigPage deviceConfigPage, GridView view)
        {
            foreach(var subView in view.Views)
            {
                VeryHtmlValid(subView.ToHtml());
            }


            HtmlAgilityPack.HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(view.ToHtml());
            Assert.AreEqual(htmlDocument.ParseErrors.Count(), 0, "Parameters HTML is ill formed");

            // not write only should have refresh buttons
            var refreshButtonNodes = htmlDocument.DocumentNode.SelectNodes("//*/button");
            Assert.AreEqual(refreshButtonNodes.Count, deviceConfigPage.Data.Parameters.Count(x => !x.WriteOnly));

            // check each parameter is present
            // int section = 0;
            // foreach( var parameter in deviceConfigPage.Data.Parameters)
            // {
            //
            // }
        }

        private static void VeryHtmlValid(string html)
        {
            HtmlAgilityPack.HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(html);
            Assert.AreEqual(htmlDocument.ParseErrors.Count(), 0);
        }
    }
}