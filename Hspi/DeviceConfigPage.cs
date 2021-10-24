using HomeSeer.Jui.Types;
using HomeSeer.Jui.Views;
using HomeSeer.PluginSdk;
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
            this.zwaveConnection = new ZWaveConnection(hsController);
            this.deviceOrFeatureRef = deviceOrFeatureRef;
        }

        public async Task<string> BuildConfigPage(CancellationToken cancellationToken)
        {
            var page = PageFactory.CreateDeviceConfigPage(PlugInData.PlugInId, "Z-Wave Information");
            var zwaveData = zwaveConnection.GetDeviceZWaveData(this.deviceOrFeatureRef);

            var openZWaveData = new OpenZWaveDBInformation(zwaveData.ManufactureId, zwaveData.ProductType, zwaveData.productId);

            await openZWaveData.Update(cancellationToken);

            if (openZWaveData.Data == null)
            {
                throw new Exception("Failed to get data from website");
            }

            data = openZWaveData.Data;

            // Label
            page = page.WithLabel(NewId(),
                    BootstrapHtmlHelper.MakeInfoHyperlinkInAnotherTab(BootstrapHtmlHelper.MakeBolder(data.FullName),
                                                                      data.WebUrl));

            //Parameters
            page = AddParameters(page, openZWaveData, zwaveData.HomeId, zwaveData.NodeId);

            createdPage = page.Page;
            return createdPage.ToJsonString();
        }

        public void OnDeviceConfigChange(Page changes)
        {
            if (createdPage == null)
            {
                throw new Exception("Existing Page is null");
            }

            if (data == null)
            {
                throw new Exception("Existing ZWave data is null");
            }

            var zwaveData = zwaveConnection.GetDeviceZWaveData(this.deviceOrFeatureRef);

            foreach (var view in changes.Views)
            {
                byte parameter = checked((byte)ZWaveParameterFromId(view.Id));

                var parameterInfo = data.Parameters.FirstOrDefault(x => x.Id == parameter);

                if ((parameterInfo == null) || (parameterInfo.Size == 0))
                {
                    throw new Exception("Z-wave paramater information not found");
                }

                int? value = null;

                if (view is InputView inputView)
                {
                    if (int.TryParse(inputView.Value, out var temp))
                    {
                        value = temp;
                    }
                    else
                    {
                        throw new InvalidValueForTypeException("Value not integer");
                    }
                }
                else if (view is SelectListView selectListView)
                {
                    if (int.TryParse(selectListView.GetSelectedOption(), out var temp))
                    {
                        value = parameterInfo?.Options?[temp].Value;
                    }
                    else
                    {
                        throw new InvalidValueForTypeException("Value not integer");
                    }
                }

                if (value.HasValue)
                {
                    zwaveConnection.UpdateDeviceParameter(zwaveData.HomeId,
                                                          zwaveData.NodeId,
                                                          parameter,
                                                          parameterInfo.Size,
                                                          value.Value);
                }
                else
                {
                    throw new InvalidValueForTypeException("View not valid");
                }
            }
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

        private static int ZWaveParameterFromId(string idParameter)
        {
            if (idParameter.StartsWith(ZWaveParameterPrefix))
            {
                if (int.TryParse(idParameter.Substring(ZWaveParameterPrefix.Length), out int id))
                {
                    return id;
                }
            }
            throw new ArgumentException(nameof(idParameter), "Not a ZWave Parameter");
        }

        private static string ZWaveParameterId(int parameter)
        {
            return Invariant($"{ZWaveParameterPrefix}{parameter}");
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
                    var elementId = ZWaveParameterId(parameter.Id);
                    string currentMessageValueId = elementId + "_message";
                    string currentWrapperControlValueId = elementId + "_wrapper";
                    string currentControlValueId = elementId;

                    string refreshButton =
                      string.Format("<button type=\"button\" class=\"btn btn-secondary refresh-z-wave\" onclick=\"refreshZWaveParameter('{0}',{1},{2},'{3}','{4}','{5}')\"> Refresh</button>",
                              homeId, nodeId, parameter.Id, currentMessageValueId, currentWrapperControlValueId, currentControlValueId);

                    string label = Invariant($"{BootstrapHtmlHelper.MakeBold(parameter.Label ?? string.Empty)}(#{parameter.Id})");

                    var row1 = new GridRow();
                    string current;
                    if (parameter.WriteOnly == "1")
                    {
                        string writeOnlyMessage = BootstrapHtmlHelper.MakeItalic(Invariant($"<span id=\"{currentMessageValueId}\">Write Only property</span>"));
                        string currentControlValue = CreateParameterValueControl(parameter, currentControlValueId);
                        string currentControlValueWrapper = Invariant($"<span id=\"{currentWrapperControlValueId}\">{currentControlValue}</span>");

                        current = BootstrapHtmlHelper.MakeMultipleRows(label,
                                                                       writeOnlyMessage,
                                                                       currentControlValueWrapper);
                    }
                    else
                    {
                        string notRetrievedMessage = BootstrapHtmlHelper.MakeItalic(Invariant($"<span id=\"{currentMessageValueId}\">Value not retrieved</span>"));
                        string currentControlValue = CreateParameterValueControl(parameter, currentControlValueId);
                        string currentControlValueWrapper = Invariant($"<span id=\"{currentWrapperControlValueId}\" hidden>{currentControlValue}</span>");

                        current = BootstrapHtmlHelper.MakeMultipleRows(label,
                                                                        Invariant($"Default: {parameter.DefaultValueDescription}"),
                                                                        notRetrievedMessage,
                                                                        currentControlValueWrapper,
                                                                        refreshButton);
                    }
                    row1.AddItem(AddRawHtml(current));

                    var options = CreateOptionsDescription(parameter);
                    var detailsLabel = AddRawHtml(BootstrapHtmlHelper.MakeMultipleRows(parameter.LongerDescription,
                                                                                           Invariant($"Size: {parameter.Size} Byte(s)"),
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
            return Invariant($"z_wave_{id++}");
        }

        private const string ZWaveParameterPrefix = "zw_parameter_";
        private readonly static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly int deviceOrFeatureRef;
        private Page? createdPage;
        private ZWaveInformation? data;
        private int id = 0;
        private ZWaveConnection zwaveConnection;
    }
}