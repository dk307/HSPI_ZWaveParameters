using HomeSeer.Jui.Types;
using HomeSeer.Jui.Views;
using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Devices;
using System;
using System.Globalization;
using System.Linq;
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

            if (!manufacturerId.HasValue || !productType.HasValue || 
                    !productId.HasValue || !nodeId.HasValue || homeId == null)
            {
                throw new Exception("Device Z-Wave plugin data is not valid");
            }

            var openZWaveData = new OpenZWaveDBInformation(manufacturerId.Value, productType.Value, productId.Value);

            await openZWaveData.Update(cancellationToken);

            if (openZWaveData.Data == null)
            {
                throw new Exception("Failed to get data from website");
            }

            var data = openZWaveData.Data;

            // Label
            page = page.WithLabel(NewId(), BootstrapHtmlHelper.MakeBolder(data.FullName));

            // Overview
            page = AddCollapsibleCardIfNotEmpty(data.Overview, page, "Overview");

            //Parameters
            page = AddParameters(page, openZWaveData, homeId, nodeId.Value);

            page = AddInclusionSections(page, data);

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

        public bool IsZwaveDevice(int devOrFeatRef)
        {
            return ((string)HomeSeerSystem.GetPropertyByRef(devOrFeatRef, EProperty.Interface) == ZWaveInterface);
        }

        private static string? CreateOptionsDescription(ZWaveDeviceParameter parameter)
        {
            if (parameter.HasOptions)
            {
                StringBuilder stb = new StringBuilder();
                stb.Append("Options:<BR>");
                foreach (var option in parameter.Options!)
                {
                    stb.Append(Invariant($"{option.Value} - {option.Label}<BR>"));
                }
                return stb.ToString();
            }
            return null; ;
        }

        private PageFactory AddCollapsibleCardIfNotEmpty(string? data, PageFactory page, string name)
        {
            if (!string.IsNullOrWhiteSpace(data))
            {
                string html = BootstrapHtmlHelper.MakeCollapsibleCard(NewId(), name, data!);
                return page.WithView(AddRawHtml(html));
            }
            return page;
        }

        private PageFactory AddInclusionSections(PageFactory page, ZWaveInformation data)
        {
            // some devices has same value for these fields
            if (data.Inclusion == data.Exclusion)
            {
                page = AddCollapsibleCardIfNotEmpty(data.Inclusion, page, "Inclusion/Exclusion");
            }
            else
            {
                page = AddCollapsibleCardIfNotEmpty(data.Inclusion, page, "Inclusion");
                page = AddCollapsibleCardIfNotEmpty(data.Exclusion, page, "Exclusion");
            }

            return page;
        }

        private PageFactory AddParameters(PageFactory page, OpenZWaveDBInformation openZWaveData,
                                                            string homeId, byte nodeId)
        {
            if (openZWaveData.Data?.Parameters != null && openZWaveData.Data.Parameters.Count > 0)
            {
                page = page.WithLabel(NewId(), string.Empty, Resource.PostForRefreshScript);

                var parametersView = new GridView(NewId(), string.Empty);

                foreach (var parameter in openZWaveData.Data.Parameters)
                {
                    string currentMessageValueId = NewId();
                    string currentWrapperControlValueId = NewId();
                    string currentControlValueId = NewId();

                    string button =
                      string.Format("<button type=\"button\" class=\"btn btn-secondary\" onclick=\"refreshZWaveParameter('{0}',{1},{2},'{3}','{4}','{5}')\"> Refresh</button>",
                              homeId, nodeId, parameter.Id, currentMessageValueId, currentWrapperControlValueId, currentControlValueId);

                    string label = Invariant($"{BootstrapHtmlHelper.MakeNormal(parameter.Label ?? string.Empty)}(#{parameter.Id})");

                    var row1 = new GridRow();
                    // row1.AddItem(AddRawHtml(BootstrapHtmlHelper.MakeMultipleRows(label, button)));

                    string currentMessageValue = BootstrapHtmlHelper.MakeNormal(Invariant($"<span id=\"{currentMessageValueId}\">Value not retrieved</span>"));
                    string currentControlValue = CreateParameterValueControl(parameter, currentControlValueId);
                    string currentControlValueWrapper = Invariant($"<span id=\"{currentWrapperControlValueId}\" hidden>{currentControlValue}</span>");

                    string current = BootstrapHtmlHelper.MakeMultipleRows(label,
                                                                          Invariant($"Default: {parameter.DefaultValueDescription}"),
                                                                          currentMessageValue, 
                                                                          currentControlValueWrapper,
                                                                          button);
                    row1.AddItem(AddRawHtml(current));

                    var options = CreateOptionsDescription(parameter);

                    LabelView detailsLabel = AddRawHtml(BootstrapHtmlHelper.MakeMultipleRows(parameter.LongerDescription,
                                                                                           Invariant($"Size:{parameter.Size} Byte(s)"),
                                                                                           options ?? Invariant($"Range: {parameter.Minimum} - {parameter.Maximum} {parameter.Units}")));
                    row1.AddItem(detailsLabel);

                    parametersView.AddRow(row1);
                }

                page = page.WithView(AddRawHtml(
                                        BootstrapHtmlHelper.MakeCollapsibleCard(NewId(), "Parameters", parametersView.ToHtml())));
            }

            return page;
        }

        private static string CreateParameterValueControl(ZWaveDeviceParameter parameter, string currentControlValueId)
        {
            if (parameter.HasOptions)
            {
                return (new SelectListView(currentControlValueId,
                                           string.Empty,
                                           parameter.Options.Select(x => x.Description).ToList(),
                                           parameter.Options.Select(x => x.Value.ToString(CultureInfo.InvariantCulture)).ToList(),
                                           ESelectListType.DropDown)).ToHtml();
            }
            else
            {
                var stb = new StringBuilder();
                stb.Append("Value");
                stb.Append(Invariant($" ({parameter.Minimum}-{parameter.Maximum}"));

                if (!string.IsNullOrWhiteSpace(parameter.Units))
                {
                    stb.Append(parameter.Units);
                }
                stb.Append(')');
                
                return (new InputView(currentControlValueId, stb.ToString(),
                                                             HomeSeer.Jui.Types.EInputType.Number)).ToHtml();
            }
        }

        private LabelView AddRawHtml(string value)
        {
            var label = new LabelView(NewId(), string.Empty, value)
            {
                LabelType = HomeSeer.Jui.Types.ELabelType.Default
            };
            return label;
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