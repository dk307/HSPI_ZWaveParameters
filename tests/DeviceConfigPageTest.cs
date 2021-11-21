using HomeSeer.Jui.Views;
using Hspi;
using Hspi.OpenZWaveDB;
using Hspi.OpenZWaveDB.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.FormattableString;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class DeviceConfigPageTest
    {
        public static IEnumerable<object[]> GetSupportsDeviceConfigPageData()
        {
            yield return new object[] { TestHelper.AeonLabsZWaveData, GetFromJsonString(Resource.AeonLabsOpenZWaveDBDeviceJson) };
            yield return new object[] { TestHelper.AeonLabsZWaveData with { Listening = false }, GetFromJsonString(Resource.AeonLabsOpenZWaveDBDeviceJson) };
            yield return new object[] { TestHelper.HomeseerDimmerZWaveData, GetFromJsonString(Resource.HomeseerDimmerOpenZWaveDBFullJson) };
            yield return new object[] { TestHelper.AeonLabsZWaveData, GetFromJsonString(Resource.AeonLabsOpenZWaveDBDeviceJsonWithInvalidHtml) };
        }

        [TestMethod]
        public async Task OnDeviceConfigChangeWithInputAsHex()
        {
            await TestOnDeviceConfigChange((view, parameter) =>
            {
                if (view is InputView inputView)
                {
                    return (true, Invariant($"0x{parameter.Default:x}"), parameter.Default);
                }
                return (false, null, null);
            });
        }

        [TestMethod]
        public async Task OnDeviceConfigChangeWithNoChange()
        {
            await TestOnDeviceConfigChange((view, parameter) => (false, null, null));
        }

        [TestMethod]
        public void OnDeviceConfigChangeWithoutBuildingPage()
        {
            int deviceRef = 3746;
            var zwaveData = TestHelper.HomeseerDimmerZWaveData;
            var httpQueryMock = TestHelper.CreateHomeseerDimmerHttpHandler();

            var mock = SetupZWaveConnection(deviceRef, zwaveData);
            var deviceConfigPage = new DeviceConfigPage(deviceRef, mock.Object,
                x => GetFromJsonString(Resource.HomeseerDimmerOpenZWaveDBFullJson));

            var changes = PageFactory.CreateGenericPage("id", "name");

            Assert.ThrowsException<InvalidOperationException>(() => deviceConfigPage.OnDeviceConfigChange(changes.Page));
        }
        [TestMethod]
        public async Task OnDeviceConfigChangeWithSetForBitmask()
        {
            await TestOnDeviceConfigChange((view, parameter) =>
            {
                if (parameter.Bitmask != 0)
                {
                    return (true, int.MaxValue.ToString(CultureInfo.InvariantCulture), parameter.Bitmask);
                }
                return (false, null, null);
            });
        }

        [TestMethod]
        public async Task OnDeviceConfigChangeWithSetToDefault()
        {
            await TestOnDeviceConfigChange((view, parameter) =>
            {
                return (true, parameter.Default.ToString(CultureInfo.InvariantCulture), parameter.Default);
            });
        }
        [DataTestMethod]
        [DynamicData(nameof(GetSupportsDeviceConfigPageData), DynamicDataSourceType.Method)]
        public async Task SupportsDeviceConfigPage(ZWaveData zwaveData, Task<ZWaveInformation> zwaveInformationTask)
        {
            int deviceRef = 34;
            var mock = SetupZWaveConnection(deviceRef, zwaveData);

            var deviceConfigPage = new DeviceConfigPage(deviceRef, mock.Object,
                                                        x => zwaveInformationTask);
            await deviceConfigPage.BuildConfigPage(CancellationToken.None);
            var page = deviceConfigPage.GetPage();

            Assert.IsNotNull(page);

            Assert.AreEqual(page.Views.Count, !zwaveData.Listening ? 5 : 4);

            // verify header link
            VerifyHeader(deviceConfigPage, page.Views[0]);

            if (!zwaveData.Listening)
            {
                // verify valid non-listening message
                TestHelper.VeryHtmlValid(page.Views[1].ToHtml());
            }

            // verify refresh button
            TestHelper.VeryHtmlValid(page.Views[!zwaveData.Listening ? 2 : 1].ToHtml());

            // verify parameters
            VerifyParametersView(deviceConfigPage, (ViewGroup)page.Views[!zwaveData.Listening ? 3 : 2]);

            // verify script
            VerifyScript((LabelView)page.Views[!zwaveData.Listening ? 4 : 3], zwaveData.Listening);

            Mock.VerifyAll(mock);
        }

        [TestMethod]
        public async Task SupportsDeviceConfigPageForMinPage()
        {
            int deviceRef = 334;

            var zwaveData = TestHelper.AeonLabsZWaveData;
            var mock = SetupZWaveConnection(deviceRef, zwaveData);

            var deviceConfigPage = new DeviceConfigPage(deviceRef, mock.Object,
                    x => GetFromJsonString("{ \"database_id\":1034, \"approved\":1, \"deleted\":0}"));
            await deviceConfigPage.BuildConfigPage(CancellationToken.None);
            var page = deviceConfigPage.GetPage();

            Assert.AreEqual(page.Views.Count, 1);

            // verify header link
            VerifyHeader(deviceConfigPage, page.Views[0]);

            Mock.VerifyAll(mock);
        }

        private static async Task<(Mock<IZWaveConnection>, DeviceConfigPage)> CreateAeonLabsSwitchDeviceConfigPage()
        {
            int deviceRef = 3746;
            ZWaveData zwaveData = TestHelper.AeonLabsZWaveData;
 
            var mock = SetupZWaveConnection(deviceRef, zwaveData);
            var deviceConfigPage = new DeviceConfigPage(deviceRef, mock.Object, 
                x => Task.FromResult(OpenZWaveDatabase.ParseJson(Resource.AeonLabsOpenZWaveDBDeviceJson)));
            await deviceConfigPage.BuildConfigPage(CancellationToken.None);
            return (mock, deviceConfigPage);
        }

        private static async Task<(Mock<IZWaveConnection>, DeviceConfigPage)> CreateHomeseerDimmerDeviceConfigPage()
        {
            int deviceRef = 3746;
            var zwaveData = TestHelper.HomeseerDimmerZWaveData;
            var httpQueryMock = TestHelper.CreateHomeseerDimmerHttpHandler();

            var mock = SetupZWaveConnection(deviceRef, zwaveData);
            var deviceConfigPage = new DeviceConfigPage(deviceRef, mock.Object,
                  x => GetFromJsonString(Resource.AeonLabsOpenZWaveDBDeviceJson));

            await deviceConfigPage.BuildConfigPage(CancellationToken.None);
            return (mock, deviceConfigPage);
        }

        private static Task<ZWaveInformation> GetFromJsonString(string json)
        {
            return Task.FromResult(OpenZWaveDatabase.ParseJson(json));
        }

        private static Mock<IZWaveConnection> SetupZWaveConnection(int deviceRef, ZWaveData zwaveData)
        {
            var mock = new Mock<IZWaveConnection>(MockBehavior.Strict);
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

        private async Task TestOnDeviceConfigChange(Func<AbstractView, ZWaveDeviceParameter, (bool, string, int?)> changedData)
        {
            var (zwaveMock, deviceConfigPage) = await CreateHomeseerDimmerDeviceConfigPage();

            Page page = deviceConfigPage.GetPage();
            var viewGroup = (ViewGroup)page.Views[2];

            var changes = PageFactory.CreateGenericPage(page.Id, page.Name);
            foreach (var view in viewGroup.Views)
            {
                if (view is not InputView && view is not SelectListView)
                {
                    continue;
                }

                var idString = view.Id.Substring(view.Id.LastIndexOf('_') + 1);
                var id = int.Parse(idString);
                var parameter = deviceConfigPage.Data.Parameters.First(x => x.Id == id);
                var (changed, newValue, expectedValue) = changedData(view, parameter);

                if (changed)
                {
                    if (view is SelectListView selectionView)
                    {
                        int newValueInt = int.Parse(newValue, NumberStyles.Any, CultureInfo.InvariantCulture);
                        int selection = parameter.Options.TakeWhile(x => x.Value != newValueInt).Count();
                        selectionView.Selection = selection;
                    }
                    else
                    {
                        view.UpdateValue(newValue);
                    }

                    changes = changes.WithView(view);
                    zwaveMock.Setup(x => x.SetConfiguration(TestHelper.HomeseerDimmerZWaveData.HomeId,
                                                            TestHelper.HomeseerDimmerZWaveData.NodeId,
                                                            parameter.ParameterId,
                                                            parameter.Size,
                                                            expectedValue.Value));
                }
            }

            deviceConfigPage.OnDeviceConfigChange(changes.Page);
            zwaveMock.Verify();
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