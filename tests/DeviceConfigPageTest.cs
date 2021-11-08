using HomeSeer.Jui.Views;
using Hspi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Contrib.HttpClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static System.FormattableString;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class DeviceConfigPageTest
    {
        private static ZWaveData AeonLabsZWaveData => new(0x0086, 6, 3, 67, "Dr5", new Version(5, 0), true);
        private static ZWaveData HomeseerSwitchZWaveData => new(0x000C, 0x3036, 0x4447, 23, "Drw5", new Version(5, 15), true);

        public static IEnumerable<object[]> GetSupportsDeviceConfigPageData()
        {
            yield return new object[] { AeonLabsZWaveData, CreateAeonLabsSwitchHttpHandler() };
            yield return new object[] { AeonLabsZWaveData with { Listening = false }, CreateAeonLabsSwitchHttpHandler() };
            yield return new object[] { HomeseerSwitchZWaveData,
                              CreateMockHttpHandler("https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x000C%204447:3036",
                                                    Resource.HomeseerDimmerOpenZWaveDBDeviceListJson,
                                                    "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=1040",
                                                    Resource.AeonLabsOpenZWaveDBDeviceJson)
            };
        }

        [TestMethod]
        public async Task OnDeviceConfigChangeWithNoChange()
        {
            DeviceConfigPage deviceConfigPage = await CreateAeonLabsSwitchDeviceConfigPage();
            deviceConfigPage.OnDeviceConfigChange(PageFactory.CreateGenericPage("id", "name").Page);
        }

        [DataTestMethod]
        [DynamicData(nameof(GetSupportsDeviceConfigPageData), DynamicDataSourceType.Method)]
        public async Task SupportsDeviceConfigPage(ZWaveData zwaveData, Mock<HttpMessageHandler> httpHandler)
        {
            int deviceRef = 34;
            var mock = SetupZWaveConnection(deviceRef, zwaveData);

            var deviceConfigPage = new DeviceConfigPage(mock.Object, deviceRef, httpHandler.CreateClient());
            await deviceConfigPage.BuildConfigPage(CancellationToken.None);
            var page = deviceConfigPage.GetPage();

            Assert.IsNotNull(page);

            Assert.AreEqual(page.Views.Count, 4);

            // verify header link
            VerifyHeader(deviceConfigPage, page.Views[0]);

            // verify refresh button
            VeryHtmlValid(page.Views[1].ToHtml());

            // verify parameters
            VerifyParametersView(deviceConfigPage, (ViewGroup)page.Views[2]);

            // verify script
            VerifyScript((LabelView)page.Views[3], zwaveData.Listening);

            Mock.VerifyAll(mock, httpHandler);
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

            var zwaveData = AeonLabsZWaveData;
            var mock = SetupZWaveConnection(deviceRef, zwaveData);

            var deviceConfigPage = new DeviceConfigPage(mock.Object, deviceRef, httpclient);
            await deviceConfigPage.BuildConfigPage(CancellationToken.None);
            var page = deviceConfigPage.GetPage();

            Assert.AreEqual(page.Views.Count, 1);

            // verify header link
            VerifyHeader(deviceConfigPage, page.Views[0]);

            Mock.VerifyAll(mock, handler);
        }

        private static async Task<DeviceConfigPage> CreateAeonLabsSwitchDeviceConfigPage()
        {
            int deviceRef = 3746;
            ZWaveData zwaveData = AeonLabsZWaveData;
            var httpHandler = CreateAeonLabsSwitchHttpHandler();

            var mock = SetupZWaveConnection(deviceRef, zwaveData);
            var deviceConfigPage = new DeviceConfigPage(mock.Object, deviceRef, httpHandler.CreateClient());
            await deviceConfigPage.BuildConfigPage(CancellationToken.None);
            return deviceConfigPage;
        }

        private static Mock<HttpMessageHandler> CreateAeonLabsSwitchHttpHandler()
        {
            return CreateMockHttpHandler("https://www.opensmarthouse.org/dmxConnect/api/zwavedatabase/device/list.php?filter=manufacturer:0x0086%200003:0006",
                                                                Resource.AeonLabsOpenZWaveDBDeviceListJson,
                                                                "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=75",
                                                                Resource.AeonLabsOpenZWaveDBDeviceJson);
        }

        private static Mock<HttpMessageHandler> CreateMockHttpHandler(string deviceListUrl, string deviceListJson, string deviceUrl, string deviceJson)
        {
            var handler = new Mock<HttpMessageHandler>();

            handler.SetupRequest(HttpMethod.Get, deviceListUrl)
                               .ReturnsResponse(deviceListJson, "application/json");

            handler.SetupRequest(HttpMethod.Get, deviceUrl)
                               .ReturnsResponse(deviceJson, "application/json");
            return handler;
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

        private static void VerifyParametersView(DeviceConfigPage deviceConfigPage, ViewGroup view)
        {
            HtmlAgilityPack.HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(view.ToHtml());
            Assert.AreEqual(htmlDocument.ParseErrors.Count(), 0, "Parameters HTML is ill formed");

            // check each parameter is present
            foreach (var parameter in deviceConfigPage.Data.Parameters)
            {
                //label
                var labelNodes = htmlDocument.DocumentNode.SelectNodes(Invariant($"//*/*[.=\"{deviceConfigPage.Data.LabelForParameter(parameter.ParameterId)}\"]"));

                Assert.IsNotNull(labelNodes);
                Assert.AreEqual(labelNodes.Count, 1);

                // input
                var dropDownNodes = htmlDocument.DocumentNode.SelectNodes(Invariant($"//*/select[@id=\"{ZWaveParameterPrefix}{parameter.Id}\"]"));

                if (parameter.HasOptions && !parameter.HasSubParameters)
                {
                    Assert.IsNotNull(dropDownNodes);
                    Assert.AreEqual(dropDownNodes.Count, 1);
                }
                else
                {
                    var inputNodes = htmlDocument.DocumentNode.SelectNodes(Invariant($"//*/input[@id=\"{ZWaveParameterPrefix}{parameter.Id}\"]"));

                    Assert.IsNotNull(inputNodes);
                    Assert.AreEqual(inputNodes.Count, 1);
                }
            }

            // not write only should have refresh buttons
            var refreshButtonNodes = htmlDocument.DocumentNode.SelectNodes("//*/button");
            Assert.AreEqual(refreshButtonNodes.Count, deviceConfigPage.Data.Parameters.Count(x => !x.WriteOnly));
        }

        private static void VeryHtmlValid(string html)
        {
            HtmlAgilityPack.HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(html);
            Assert.AreEqual(htmlDocument.ParseErrors.Count(), 0);
        }

        private void VerifyScript(LabelView view, bool listening)
        {
            HtmlAgilityPack.HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(view.ToHtml());
            Assert.AreEqual(htmlDocument.ParseErrors.Count(), 0, "Script HTML is ill formed");

            var scriptNodes = htmlDocument.DocumentNode.SelectNodes(Invariant($"//*/script"));
            Assert.IsNotNull(scriptNodes);

            string last = scriptNodes.Last().OuterHtml;
            Assert.AreEqual(last.Contains(".ready(function() {"), listening);
        }
        private const string ZWaveParameterPrefix = "zw_parameter_";
    }
}