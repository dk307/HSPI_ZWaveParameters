using HomeSeer.Jui.Views;
using Hspi;
using Hspi.OpenZWaveDB;
using Hspi.OpenZWaveDB.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.FormattableString;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class DeviceConfigPageTest
    {
        public static IEnumerable<object[]> BuildConfigPageForAllPagesData()
        {
            foreach (var item in Directory.EnumerateFiles(TestHelper.GetOfflineDatabasePath(), "*.json", SearchOption.AllDirectories))
            {
                yield return new object[] { item };
            }
        }

        public static IEnumerable<object[]> GetSupportsDeviceConfigPageData()
        {
            yield return new object[] { TestHelper.AeonLabsZWaveData, GetFromJsonString(Resource.AeonLabsOpenZWaveDBDeviceJson) };
            yield return new object[] { TestHelper.AeonLabsZWaveData with { Listening = false }, GetFromJsonString(Resource.AeonLabsOpenZWaveDBDeviceJson) };
            yield return new object[] { TestHelper.HomeseerDimmerZWaveData, GetFromJsonString(Resource.HomeseerDimmerOpenZWaveDBFullJson) };
            yield return new object[] { TestHelper.AeonLabsZWaveData, GetFromJsonString(Resource.AeonLabsOpenZWaveDBDeviceJsonWithInvalidHtml) };
        }

        [DataTestMethod]
        [DynamicData(nameof(GetSupportsDeviceConfigPageData), DynamicDataSourceType.Method)]
        public async Task BuildConfigPage(ZWaveData zwaveData, Task<ZWaveInformation> zwaveInformationTask)
        {
            await BuildConfigPageImpl(true, zwaveData, zwaveInformationTask).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DynamicData(nameof(BuildConfigPageForAllPagesData), DynamicDataSourceType.Method)]
        public async Task BuildConfigPageForAllPages(string filePath)
        {
            ZWaveData zwaveData = TestHelper.AeonLabsZWaveData;
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var zWaveInformation = await OpenZWaveDatabase.ParseJson(fileStream, CancellationToken.None)
                                   .ConfigureAwait(false);

            await BuildConfigPageImpl((zWaveInformation?.Parameters.Count ?? 0) > 0, zwaveData, Task.FromResult(zWaveInformation));
        }

        [TestMethod]
        public async Task OnDeviceConfigChangeWithInputAsHex()
        {
            await TestOnDeviceConfigChange((view, parameter) =>
            {
                if (view is InputView)
                {
                    return (true, Invariant($"0x{parameter.Default:x}"), parameter.Default);
                }
                return (false, null, null);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OnDeviceConfigChangeWithOutofRangeValue()
        {
            var task = TestOnDeviceConfigChange((view, parameter) =>
            {
                if (view is InputView)
                {
                    return (true, (((long)int.MaxValue) + 1).ToString(), parameter.Default);
                }
                return (false, null, null);
            });

            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => task);
        }

        [TestMethod]
        public async Task OnDeviceConfigChangeWithNonIntegerValue()
        {
            var task = TestOnDeviceConfigChange((view, parameter) =>
            {
                if (view is InputView)
                {
                    return (true, "abcd", parameter.Default);
                }
                return (false, null, null);
            });

            await Assert.ThrowsExceptionAsync<InvalidValueForTypeException>(() => task);
        }

        [TestMethod]
        public async Task OnDeviceConfigChangeWithNoChange()
        {
            await TestOnDeviceConfigChange((_, _) => (false, null, null)).ConfigureAwait(false);
        }

        [TestMethod]
        public void OnDeviceConfigChangeWithoutBuildingPage()
        {
            int deviceRef = 3746;
            var zwaveData = TestHelper.HomeseerDimmerZWaveData;

            var mock = SetupZWaveConnection(deviceRef, zwaveData);
            var deviceConfigPage = new DeviceConfigPage(deviceRef, mock.Object,
                x => GetFromJsonString(Resource.HomeseerDimmerOpenZWaveDBFullJson));

            var changes = PageFactory.CreateGenericPage("id", "name");

            Assert.ThrowsException<InvalidOperationException>(() => deviceConfigPage.OnDeviceConfigChange(changes.Page));
        }

        [TestMethod]
        public async Task OnDeviceConfigChangeWithSetForBitmask()
        {
            await TestOnDeviceConfigChange((_, parameter) =>
            {
                if (parameter.Bitmask != 0)
                {
                    return (true, int.MaxValue.ToString(CultureInfo.InvariantCulture), parameter.Bitmask);
                }
                return (false, null, null);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OnDeviceConfigChangeWithSetToDefault()
        {
            await TestOnDeviceConfigChange((view, parameter) =>
            {
                return (true, parameter.Default.ToString(CultureInfo.InvariantCulture), parameter.Default);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SupportsDeviceConfigPageForMinPage()
        {
            var zwaveData = TestHelper.AeonLabsZWaveData;
            await BuildConfigPageImpl(false, zwaveData,
                                      GetFromJsonString("{ \"database_id\":1034, \"approved\":1, \"deleted\":0}")).ConfigureAwait(false);
        }

        private static async Task<(Mock<IZWaveConnection>, DeviceConfigPage)> CreateHomeseerDimmerDeviceConfigPage()
        {
            int deviceRef = 3746;
            var zwaveData = TestHelper.HomeseerDimmerZWaveData;

            var mock = SetupZWaveConnection(deviceRef, zwaveData);
            var deviceConfigPage = new DeviceConfigPage(deviceRef, mock.Object,
                  x => GetFromJsonString(Resource.HomeseerDimmerOpenZWaveDBFullJson));

            await deviceConfigPage.BuildConfigPage(CancellationToken.None).ConfigureAwait(false);
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
            Assert.AreEqual(0, htmlDocument.ParseErrors.Count());

            var node = htmlDocument.DocumentNode.SelectSingleNode("//*/a");

            Assert.IsNotNull(node);
            Assert.IsTrue(node.InnerHtml.Contains(deviceConfigPage.Data.DisplayFullName()));
            Assert.AreEqual(node.Attributes["href"].Value, deviceConfigPage.Data.WebUrl.ToString());
        }

        private static void VerifyParametersView(DeviceConfigPage deviceConfigPage, ViewGroup view)
        {
            HtmlAgilityPack.HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(view.ToHtml());
            Assert.AreEqual(0, htmlDocument.ParseErrors.Count(), "Parameters HTML is ill formed");

            // check each parameter is present
            foreach (var parameter in deviceConfigPage.Data.Parameters)
            {
                //label
                string label = parameter.LabelForParameter();
                Assert.IsTrue(view.Views.Any(x => x is LabelView labelView && labelView.Value.Contains(label)));

                // input
                var dropDownNodes = htmlDocument.DocumentNode.SelectNodes(Invariant($"//*/select[@id=\"{ZWaveParameterPrefix}{parameter.Id}\"]"));

                if (parameter.HasOptions && !parameter.HasSubParameters)
                {
                    Assert.IsNotNull(dropDownNodes);
                    Assert.AreEqual(1, dropDownNodes.Count);
                }
                else
                {
                    var inputNodes = htmlDocument.DocumentNode.SelectNodes(Invariant($"//*/input[@id=\"{ZWaveParameterPrefix}{parameter.Id}\"]"));

                    Assert.IsNotNull(inputNodes);
                    Assert.AreEqual(1, inputNodes.Count);
                }
            }

            // not write only should have refresh buttons
            int refreshButtons = deviceConfigPage.Data.Parameters.Count(x => !x.WriteOnly);

            var refreshButtonNodes = htmlDocument.DocumentNode.SelectNodes("//*/button");
            if (refreshButtons != 0)
            {
                Assert.IsNotNull(refreshButtonNodes);
                Assert.AreEqual(refreshButtonNodes.Count, refreshButtons);
            }
            else
            {
                Assert.IsNull(refreshButtonNodes);
            }
        }

        private async Task BuildConfigPageImpl(bool hasParamaters,
                                               ZWaveData zwaveData,
                                               Task<ZWaveInformation> zwaveInformationTask)
        {
            int deviceRef = 34;
            var mock = SetupZWaveConnection(deviceRef, zwaveData);

            var deviceConfigPage = new DeviceConfigPage(deviceRef, mock.Object,
                                                        x => zwaveInformationTask);
            await deviceConfigPage.BuildConfigPage(CancellationToken.None).ConfigureAwait(false);
            var page = deviceConfigPage.GetPage();

            Assert.IsNotNull(page);

            int index = 0;

            // verify header link
            VerifyHeader(deviceConfigPage, page.Views[index++]);

            if (hasParamaters)
            {
                if (!zwaveData.Listening)
                {
                    // verify valid non-listening message
                    TestHelper.VerifyHtmlValid(page.Views[index++].ToHtml());
                }

                // verify refresh button
                if (deviceConfigPage.Data.HasRefreshableParameters)
                {
                    TestHelper.VerifyHtmlValid(page.Views[index++].ToHtml());
                }

                // verify parameters
                VerifyParametersView(deviceConfigPage, (ViewGroup)page.Views[index++]);

                // verify script
                bool autoRefresh = deviceConfigPage.Data.HasRefreshableParameters && zwaveData.Listening;
                VerifyScript((LabelView)page.Views[index++], autoRefresh);
            }

            // no more views
            Assert.AreEqual(page.Views.Count, index);

            Mock.VerifyAll(mock);
        }

        private async Task TestOnDeviceConfigChange(Func<AbstractView, ZWaveDeviceParameter, (bool, string, long?)> changedData)
        {
            var (zwaveMock, deviceConfigPage) = await CreateHomeseerDimmerDeviceConfigPage()
                                                      .ConfigureAwait(false);

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
                                                            (int)expectedValue.Value));
                }
            }

            deviceConfigPage.OnDeviceConfigChange(changes.Page);
            zwaveMock.Verify();
        }

        private void VerifyScript(LabelView view, bool hasRefreshAllButton)
        {
            HtmlAgilityPack.HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(view.ToHtml());
            Assert.AreEqual(0, htmlDocument.ParseErrors.Count(), "Script HTML is ill formed");

            var scriptNodes = htmlDocument.DocumentNode.SelectNodes(Invariant($"//*/script"));
            Assert.IsNotNull(scriptNodes);

            var last = scriptNodes.LastOrDefault()?.OuterHtml;
            Assert.AreEqual(last.Contains(".ready(function() {"), hasRefreshAllButton);
        }

        private const string ZWaveParameterPrefix = "zw_parameter_";
    }
}