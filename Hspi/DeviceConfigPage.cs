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
        public DeviceConfigPage(IHsController hsController, int deviceOrFeatureRef)
        {
            this.HomeSeerSystem = hsController;
            this.deviceOrFeatureRef = deviceOrFeatureRef;
        }

        private IHsController HomeSeerSystem { get; }

        public static bool IsZwaveDevice(IHsController hsController, int devOrFeatRef)
        {
            return ((string)hsController.GetPropertyByRef(devOrFeatRef, EProperty.Interface) == ZWaveInterface);
        }

        public async Task<string> BuildConfigPage(CancellationToken cancellationToken)
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
            page = page.WithLabel(NewId(),
                    BootstrapHtmlHelper.MakeInfoHyperlinkInAnotherTab(BootstrapHtmlHelper.MakeBolder(data.FullName),
                                                                      data.WebUrl));

            //Parameters
            page = AddParameters(page, openZWaveData, homeId, nodeId.Value);

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

        private PageFactory AddParameters(PageFactory page, OpenZWaveDBInformation openZWaveData,
                                                            string homeId, byte nodeId)
        {
            if (openZWaveData.Data?.Parameters != null && openZWaveData.Data.Parameters.Count > 0)
            {
                page = page.WithLabel(NewId(), string.Empty, HtmlSnippets.PostForRefreshScript);

                var parametersView = new GridView(NewId(), string.Empty);
                var allButtonId = NewId();
                string allButton =
                    string.Format("<button id=\"{1}\" type=\"button\" class=\"btn btn-secondary\" onclick=\"refreshAllZWaveParameters('{0}')\"> Refresh all parameters</button>",
                                   parametersView.Id, allButtonId);

                page = page.WithLabel(NewId(), string.Empty, (HtmlSnippets.AllParametersScript + allButton));

                foreach (var parameter in openZWaveData.Data.Parameters)
                {
                    if (parameter.ReadOnly != "0")
                    {
                        continue;
                    }
                    var elementId = ZWaveParameterId(parameter.Id);
                    string currentMessageValueId = elementId + "_message";
                    string currentWrapperControlValueId = elementId + "_wrapper";
                    string currentControlValueId = elementId;

                    string button =
                      string.Format("<button type=\"button\" class=\"btn btn-secondary refresh-z-wave\" onclick=\"refreshZWaveParameter('{0}',{1},{2},'{3}','{4}','{5}')\"> Refresh</button>",
                              homeId, nodeId, parameter.Id, currentMessageValueId, currentWrapperControlValueId, currentControlValueId);

                    string label = Invariant($"{BootstrapHtmlHelper.MakeBold(parameter.Label ?? string.Empty)}(#{parameter.Id})");

                    var row1 = new GridRow();

                    string notRetrievedMessage = BootstrapHtmlHelper.MakeItalic(Invariant($"<span id=\"{currentMessageValueId}\">Value not retrieved</span>"));
                    string currentControlValue = CreateParameterValueControl(parameter, currentControlValueId);
                    string currentControlValueWrapper = Invariant($"<span id=\"{currentWrapperControlValueId}\" hidden>{currentControlValue}</span>");

                    string current = BootstrapHtmlHelper.MakeMultipleRows(label,
                                                                          Invariant($"Default: {parameter.DefaultValueDescription}"),
                                                                          notRetrievedMessage,
                                                                          currentControlValueWrapper,
                                                                          button);
                    row1.AddItem(AddRawHtml(current));

                    var options = CreateOptionsDescription(parameter);
                    var detailsLabel = AddRawHtml(BootstrapHtmlHelper.MakeMultipleRows(parameter.LongerDescription,
                                                                                           Invariant($"Size:{parameter.Size} Byte(s)"),
                                                                                           options ?? Invariant($"Range: {parameter.Minimum} - {parameter.Maximum} {parameter.Units}")));
                    row1.AddItem(detailsLabel);
                    parametersView.AddRow(row1);
                }

                page = page.WithView(parametersView);
                string clickRefreshButtonScript = HtmlSnippets.ClickRefreshButtonScript;
                page = page.WithLabel(NewId(), string.Empty, string.Format(clickRefreshButtonScript, parametersView.Id, allButtonId));
            }

            return page;
        }

        private static string CreateParameterValueControl(ZWaveDeviceParameter parameter, string currentControlValueId)
        {
            if (parameter.HasOptions)
            {
                var options = parameter.Options.Select(x => x.Description).ToList();
                var optionKeys = parameter.Options.Select(x => x.Value.ToString(CultureInfo.InvariantCulture)).ToList();

                string script =
                    Invariant($"<script> const {currentControlValueId}_option = [{string.Join(",", optionKeys)}];</script>");
                var selectListView = new SelectListView(currentControlValueId,
                                                           string.Empty,
                                                           options,
                                                           optionKeys,
                                                           ESelectListType.DropDown);
                return script + selectListView.ToHtml();
            }
            else
            {
                var stb = new StringBuilder();
                stb.Append("Value");
                stb.Append(Invariant($" ({parameter.Minimum}-{parameter.Maximum}"));

                if (!string.IsNullOrWhiteSpace(parameter.Units))
                {
                    stb.Append(' ');
                    stb.Append(parameter.Units);
                }
                stb.Append(')');

                return (new InputView(currentControlValueId, stb.ToString(),
                                                             HomeSeer.Jui.Types.EInputType.Number)).ToHtml();
            }
        }

        private LabelView AddRawHtml(string value, string? id = null)
        {
            var label = new LabelView(id ?? NewId(), string.Empty, value)
            {
                LabelType = ELabelType.Default
            };
            return label;
        }

        private string NewId()
        {
            return Invariant($"z_wave{id++}");
        }

        private string ZWaveParameterId(int parameter)
        {
            return Invariant($"zw_parameter{parameter}");
        }

        private const string ZWaveInterface = "Z-Wave";
        private readonly static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly int deviceOrFeatureRef;
        private int id = 0;
    }
}