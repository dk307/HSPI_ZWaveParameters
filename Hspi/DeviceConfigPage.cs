using HomeSeer.Jui.Types;
using HomeSeer.Jui.Views;
using HomeSeer.PluginSdk;
using Hspi.OpenZWaveDB;
using System;
using System.Collections.Generic;
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
            logger.Debug(Invariant($"Creating Page for {deviceOrFeatureRef}"));
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
                    BootstrapHtmlHelper.MakeInfoHyperlinkInAnotherTab(BootstrapHtmlHelper.MakeBolder(data.DisplayFullName()),
                                                                      data.WebUrl));

            //Parameters
            page = AddParameters(page, openZWaveData, zwaveData.HomeId, zwaveData.NodeId);

            return page.Page.ToJsonString();
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
                byte id = checked((byte)ZWaveParameterFromId(view.Id));

                var parameterInfo = data.Parameters.FirstOrDefault(x => x.Id == id);
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
                                                          parameterInfo.ParameterId,
                                                          parameterInfo.Size,
                                                          value.Value);
                }
                else
                {
                    throw new InvalidValueForTypeException("View not valid");
                }
            }
        }

        private static string CreateParameterValueControl(ZWaveDeviceParameter parameter, string id)
        {
            var stb = new StringBuilder();

            stb.Append(Invariant($"<script> const {id}_mask = 0x{parameter.Bitmask:x};</script>"));

            if (parameter.HasOptions && !parameter.HasSubParameters)
            {
                var options = parameter.Options.Select(x => x.Description).ToList();
                var optionKeys = parameter.Options.Select(x => x.Value.ToString(CultureInfo.InvariantCulture)).ToList();

                stb.Append(Invariant($"<script> const {id}_optionkeys = [{string.Join(",", optionKeys)}];</script>"));

                var selectListView = new SelectListView(id,
                                                        string.Empty,
                                                        options,
                                                        ESelectListType.DropDown);
                stb.Append(selectListView.ToHtml());

                // Have not found a away to make it readonly on UI
            }
            else
            {
                var stb2 = new StringBuilder();

                stb2.Append("Value");
                if (!parameter.HasSubParameters)
                {
                    stb2.Append('(');
                    stb2.Append(Invariant($" {parameter.Minimum}-{parameter.Maximum} "));

                    if (!string.IsNullOrWhiteSpace(parameter.Units))
                    {
                        stb2.Append(parameter.Units);
                    }
                    stb2.Append(')');
                }

                var inputView = new InputView(id, stb2.ToString(), EInputType.Number);
                stb.Append(inputView.ToHtml());

                if (parameter.ReadOnly)
                {
                    stb.Append(Invariant($"<script>$(\"#{id}\").attr('readonly', 'readonly');</script>"));
                }
            }

            return stb.ToString();
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
                    var row = new GridRow();
                    var current = CreateGetSetViewForParameter(openZWaveData.Data, parameter, homeId, nodeId);
                    row.AddItem(current);

                    var detailsLabel = CreateDescriptionViewForParameter(openZWaveData.Data, parameter.ParameterId);
                    row.AddItem(detailsLabel);

                    parametersView.AddRow(row);
                }

                page = page.WithView(parametersView);
                string clickRefreshButtonScript = HtmlSnippets.ClickRefreshButtonScript;
                page = page.WithLabel(NewId(), string.Empty, string.Format(clickRefreshButtonScript, parametersView.Id, allButtonId));
            }

            return page;
        }

        private LabelView CreateGetSetViewForParameter(ZWaveInformation data,
                                                       ZWaveDeviceParameter parameter, string homeId, byte nodeId)
        {
            var elementId = CreateZWaveParameterId(parameter.Id);
            string currentMessageValueId = elementId + "_message";
            string currentWrapperControlValueId = elementId + "_wrapper";

            string refreshButton =
              string.Format("<button type=\"button\" class=\"btn btn-secondary refresh-z-wave\" onclick=\"refreshZWaveParameter('{0}',{1},{2},'{3}','{4}','{5}')\">Refresh</button>",
                      homeId, nodeId, parameter.ParameterId, currentMessageValueId, currentWrapperControlValueId, elementId);

            var list = new List<string>
            {
                Invariant($"{BootstrapHtmlHelper.MakeBold(data.LabelForParameter(parameter.ParameterId))}(#{parameter.ParameterId})")
            };

            var topMessage = parameter.WriteOnly ? "Write Only parameter" : "Value not retrieved";
            string notRetrievedMessage = Invariant($"<span id=\"{currentMessageValueId}\">{topMessage}</span>");
            list.Add(BootstrapHtmlHelper.MakeItalic(notRetrievedMessage));

            string currentControlValue = CreateParameterValueControl(parameter, elementId);
            if (parameter.ReadOnly)
            {
                string readonlyMessage = BootstrapHtmlHelper.MakeItalic("Read only parameter");
                currentControlValue = BootstrapHtmlHelper.MakeMultipleRows(readonlyMessage, currentControlValue);
            }

            string currentControlValueWrapper = Invariant($"<span id=\"{currentWrapperControlValueId}\" {(!parameter.WriteOnly ? "hidden" : string.Empty)}>{currentControlValue}</span>");
            list.Add(currentControlValueWrapper);

            if (!parameter.WriteOnly)
            {
                list.Add(refreshButton);
            }

            var current = BootstrapHtmlHelper.MakeMultipleRows(list.ToArray());
            return AddRawHtml(current);
        }

        private LabelView CreateDescriptionViewForParameter(ZWaveInformation data, int parameterId)
        {
            var list = data.DescriptionForParameter(parameterId);
            var detailsLabel = AddRawHtml(BootstrapHtmlHelper.MakeMultipleRows(list.ToArray()));
            return detailsLabel;
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
            return new LabelView(id ?? NewId(), string.Empty, value);
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