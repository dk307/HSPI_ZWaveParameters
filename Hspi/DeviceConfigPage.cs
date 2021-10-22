using HomeSeer.Jui.Views;
using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Devices;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.FormattableString;

#nullable enable

namespace Hspi
{
    internal class DeviceConfigPage
    {
        public DeviceConfigPage(IHsController hsController)
        {
            this.HomeSeerSystem = hsController;
        }

        private IHsController HomeSeerSystem { get; }

        public static bool IsZwaveDevice(IHsController hsController, int devOrFeatRef)
        {
            return ((string)hsController.GetPropertyByRef(devOrFeatRef, EProperty.Interface) == ZWaveInterface);
        }

        public async Task<string> BuildConfigPage(int deviceOrFeatureRef, CancellationToken cancellationToken)
        {
            var page = PageFactory.CreateDeviceConfigPage(PlugInData.PlugInId, "Z-Wave Information");
            if (!IsZwaveDevice(deviceOrFeatureRef))
            {
                throw new Exception("Device is not a Z-Wave device");
            }

            var plugInData = (PlugExtraData)HomeSeerSystem.GetPropertyByRef(deviceOrFeatureRef, EProperty.PlugExtraData);

            if (plugInData == null)
            {
                throw new Exception("Device Plugin extra data is not valid");
            }

            var manufacturerId = GetValueFromExtraData<Int32>(plugInData, "manufacturer_id");
            var productId = GetValueFromExtraData<UInt16>(plugInData, "manufacturer_prod_id");
            var productType = GetValueFromExtraData<UInt16>(plugInData, "manufacturer_prod_type");

            var nodeId = GetValueFromExtraData<Byte>(plugInData, "node_id");
            var homeId = plugInData["homeid"];

            if (!manufacturerId.HasValue || !productType.HasValue || !productId.HasValue)
            {
                throw new Exception("Device Z-Wave plugin data is not valid");
            }

            var openZWaveData = new OpenZWaveDBInformation(manufacturerId.Value, productType.Value, productId.Value);

            await openZWaveData.Update(cancellationToken);

            page = AddTopLevelStuff(page, openZWaveData);

            page = AddParameters(page, openZWaveData);

            return page.Page.ToJsonString();

            static T? GetValueFromExtraData<T>(PlugExtraData plugInData, string name) where T : struct
            {
                if (plugInData.ContainsNamed(name))
                {
                    return (T)Convert.ChangeType(plugInData[name].Trim('"', '\\'), typeof(T));
                }
                else
                {
                    return null;
                }
            }
        }

        private PageFactory AddParameters(PageFactory page, OpenZWaveDBInformation openZWaveData)
        {
            if (openZWaveData.Data.Parameters.Count > 0)
            {
                GridView parametersView = new GridView(NewId(), string.Empty);

                //Stopwatch stopwatch = new Stopwatch();

                //stopwatch.Start();

                //logger.Info(Invariant($"Getting {openZWaveData.Data.Parameters.Count}"));
                foreach (var parameter in openZWaveData.Data.Parameters)
                {
                    string label = Invariant($"{parameter.Label}({parameter.Id})");

                    var row1 = new GridRow();
                    row1.AddItem(new LabelView(NewId(), string.Empty, BootstrapHtmlHelper.MakeNormal(label)));
                    string range = Invariant($"Size:{parameter.Size} Byte(s)<BR>Default:{ parameter.Default}</BR>{parameter.Units} Range {parameter.Minimum} - {parameter.Maximum} {parameter.Units}");
                    row1.AddItem(new LabelView(NewId(), string.Empty, range));

                    // int value = GetConfiguration(homeId, nodeId.Value, (byte)parameter.Id);
                    // row1.AddItem(new LabelView(NewId(), "Val", Invariant($"{value}")));

                    string options = CreateOptionsDescription(parameter);

                    row1.AddItem(new LabelView(NewId(), string.Empty, parameter.FinalDescription + "<BR>" + options));

                    parametersView.AddRow(row1);
                }

                //stopwatch.Stop();

                //logger.Info(Invariant($"Took {stopwatch.Elapsed.TotalMilliseconds} for {openZWaveData.Data.Parameters.Count} Parameters"));

                page = page.WithView(new LabelView(NewId(), string.Empty,
                                        BootstrapHtmlHelper.MakeCollapsibleCard(NewId(), "Parameters", parametersView.ToHtml())));
            }

            return page;
        }

        private PageFactory AddTopLevelStuff(PageFactory page, OpenZWaveDBInformation openZWaveData)
        {
            page = page.WithLabel(NewId(), BootstrapHtmlHelper.MakeBolder(openZWaveData.FullName));
            page = AddNodeIfNotEmpty(openZWaveData.Data.Overview, page, "Overview");

            // some devices has same value for these fields
            if (openZWaveData.Data.Inclusion == openZWaveData.Data.Exclusion)
            {
                page = AddNodeIfNotEmpty(openZWaveData.Data.Inclusion, page, "Inclusion/Exclusion");
            }
            else
            {
                page = AddNodeIfNotEmpty(openZWaveData.Data.Inclusion, page, "Inclusion");
                page = AddNodeIfNotEmpty(openZWaveData.Data.Exclusion, page, "Exclusion");
            }

            return page;

            PageFactory AddNodeIfNotEmpty(string data, PageFactory page, string name)
            {
                if (!string.IsNullOrWhiteSpace(data))
                {
                    string html = BootstrapHtmlHelper.MakeCollapsibleCard(NewId(), name, data);
                    return page.WithView(new LabelView(NewId(), string.Empty, html));
                }
                return page;
            }
        }

        private static string CreateOptionsDescription(ZWaveDeviceParameter parameter)
        {
            StringBuilder stb = new StringBuilder();
            foreach (var option in parameter.Options)
            {
                stb.Append(Invariant($"{option.Value} - {option.Label}<BR>"));
            }
            string options = stb.ToString();
            return options;
        }

        public bool IsZwaveDevice(int devOrFeatRef)
        {
            return ((string)HomeSeerSystem.GetPropertyByRef(devOrFeatRef, EProperty.Interface) == ZWaveInterface);
        }

        private int GetConfiguration(string homeId, byte nodeId, byte param)
        {
            return (int)HomeSeerSystem.LegacyPluginFunction("Z-Wave", string.Empty, "Configuration_Get", new object[3] { homeId, nodeId, param });
        }

        private string NewId()
        {
            return Invariant($"z-wave{id++}");
        }

        private const string ZWaveInterface = "Z-Wave";
        private readonly static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private int id = 0;
    }
}