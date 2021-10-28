using HomeSeer.Jui.Types;
using HomeSeer.Jui.Views;
using HomeSeer.PluginSdk;
using Hspi.OpenZWaveDB;
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

            var openZWaveData = new OpenZWaveDBInformation(zwaveData.ManufactureId, zwaveData.ProductType,
                                                           zwaveData.ProductId, zwaveData.Firmware);

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

            var createdPage = page.Page;
            return createdPage.ToJsonString();
        }

        public void OnDeviceConfigChange(Page changes)
        {
            if (data == null)
            {
                throw new Exception("Existing ZWave data is null");
            }

            var zwaveData = zwaveConnection.GetDeviceZWaveData(this.deviceOrFeatureRef);

            foreach (var view in changes.Views)
            {
                byte parameter = checked((byte)ZWaveParameterFromId(view.Id));

                var parameterInfo = data.Parameters.FirstOrDefault(x => x.ParameterId == parameter);
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

        private static string CreateParameterValueControl(ZWaveDeviceParameter parameter, string id)
        {
            string scriptBitmask =
                Invariant($"<script> const {id}_mask = {parameter.Bitmask};</script>");
            if (parameter.HasOptions)
            {
                var options = parameter.Options.Select(x => x.Description).ToList();
                var optionKeys = parameter.Options.Select(x => x.Value.ToString(CultureInfo.InvariantCulture)).ToList();

                string script =
                    Invariant($"<script> const {id}_option = [{string.Join(",", optionKeys)}];</script>");
                var selectListView = new SelectListView(id,
                                                           string.Empty,
                                                           options,
                                                           optionKeys,
                                                           ESelectListType.DropDown);
                return scriptBitmask + script + selectListView.ToHtml();
            }
            else
            {
                var stb = new StringBuilder();
                stb.Append("Value");

                stb.Append('(');
                stb.Append(Invariant($" {parameter.Minimum}-{parameter.Maximum} "));

                if (!string.IsNullOrWhiteSpace(parameter.Units))
                {
                    stb.Append(parameter.Units);
                }
                stb.Append(')');

                InputView inputView = new InputView(id, stb.ToString(), EInputType.Number);
                return scriptBitmask + inputView.ToHtml();
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
            throw new ArgumentException("Not a ZWave Parameter", nameof(idParameter));
        }

        private static string CreateZWaveParameterId(int parameter)
        {
            return Invariant($"{ZWaveParameterPrefix}{parameter}");
        }

        private PageFactory AddParameters(PageFactory page, OpenZWaveDBInformation openZWaveData,
                                             string homeId, byte nodeId)
        {
            if (openZWaveData.Data?.Parameters != null && openZWaveData.Data.Parameters.Count > 0)
            {
                var parametersView = new GridView(NewId(), string.Empty);
                page = CreateAllParameterRefreshButton(page, parametersView.Id, out var allButtonId);

                foreach (var parameter in openZWaveData.Data.Parameters)
                {
                    var elementId = CreateZWaveParameterId(parameter.Id);
                    string currentMessageValueId = elementId + "_message";
                    string currentWrapperControlValueId = elementId + "_wrapper";

                    string refreshButton =
                      string.Format("<button type=\"button\" class=\"btn btn-secondary refresh-z-wave\" onclick=\"refreshZWaveParameter('{0}',{1},{2},'{3}','{4}','{5}')\"> Refresh</button>",
                              homeId, nodeId, parameter.ParameterId, currentMessageValueId, currentWrapperControlValueId, elementId);

                    string label = Invariant($"{BootstrapHtmlHelper.MakeBold(parameter.Label ?? string.Empty)}(#{parameter.ParameterId})");

                    var row1 = new GridRow();
                    string current;
                    if (parameter.WriteOnly == "1")
                    {
                        string writeOnlyMessage = BootstrapHtmlHelper.MakeItalic(Invariant($"<span id=\"{currentMessageValueId}\">Write Only property</span>"));
                        string currentControlValue = CreateParameterValueControl(parameter, elementId);
                        string currentControlValueWrapper = Invariant($"<span id=\"{currentWrapperControlValueId}\">{currentControlValue}</span>");

                        current = BootstrapHtmlHelper.MakeMultipleRows(label,
                                                                       writeOnlyMessage,
                                                                       currentControlValueWrapper);
                    }
                    else
                    {
                        string notRetrievedMessage = BootstrapHtmlHelper.MakeItalic(Invariant($"<span id=\"{currentMessageValueId}\">Value not retrieved</span>"));
                        string currentControlValue = CreateParameterValueControl(parameter, elementId);
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

        private PageFactory CreateAllParameterRefreshButton(PageFactory page, string containerToClickButtonId, out string allButtonId)
        {
            page = page.WithLabel(NewId(), string.Empty, HtmlSnippets.PostForRefreshScript);

            allButtonId = NewId();
            string allButton =
                string.Format("<button id=\"{1}\" type=\"button\" class=\"btn btn-secondary\" onclick=\"refreshAllZWaveParameters('{0}')\"> Refresh all parameters</button>",
                                containerToClickButtonId, allButtonId);

            page = page.WithLabel(NewId(), string.Empty, (HtmlSnippets.AllParametersScript + allButton));
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
            return Invariant($"z_wave_paramater_{id++}");
        }

        private const string ZWaveParameterPrefix = "zw_parameter_";
        private readonly static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly int deviceOrFeatureRef;
        private ZWaveInformation? data;
        private int id = 0;
        private readonly ZWaveConnection zwaveConnection;
    }
}