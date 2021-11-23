using HomeSeer.Jui.Types;
using HomeSeer.Jui.Views;
using Hspi.OpenZWaveDB.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.FormattableString;

#nullable enable

namespace Hspi
{
    internal class DeviceConfigPage : IDeviceConfigPage
    {
        public DeviceConfigPage(int deviceOrFeatureRef, IZWaveConnection zwaveConnection,
                                Func<ZWaveData, Task<ZWaveInformation>> factoryForZWaveInformation)
        {
            Log.Debug("Creating Page for deviceRef:{deviceRef}", deviceOrFeatureRef);
            this.zwaveConnection = zwaveConnection;
            this.factoryForZWaveInformation = factoryForZWaveInformation;
            this.deviceOrFeatureRef = deviceOrFeatureRef;
        }

        public ZWaveInformation? Data { get; private set; }

        public virtual async Task BuildConfigPage(CancellationToken cancellationToken)
        {
            var pageFactory = PageFactory.CreateDeviceConfigPage(PlugInData.PlugInId, "Z-Wave Information");
            var zwaveData = zwaveConnection.GetDeviceZWaveData(this.deviceOrFeatureRef);

            Data = await factoryForZWaveInformation(zwaveData).ConfigureAwait(false);

            if (Data == null)
            {
                throw new Exception("Failed to get data from website");
            }

            var scripts = new List<string>();

            // Label
            string labelText0 = Bootstrap.ApplyStyle(Data.DisplayFullName(), Bootstrap.Style.TextBolder, Bootstrap.Style.TextWrap);
            string labelText = Bootstrap.MakeInfoHyperlinkInAnotherTab(labelText0, Data.WebUrl);
            pageFactory = pageFactory.WithView(AddRawHtml(Invariant($"<h6>{labelText}</h6>"), true));

            //Parameters
            pageFactory = AddParameters(pageFactory, scripts, zwaveData.HomeId, zwaveData.NodeId, zwaveData.Listening);

            if (scripts.Count > 0)
            {
                pageFactory = pageFactory.WithLabel(NewId(), string.Empty, string.Join(Environment.NewLine, scripts));
            }
            SetPage(pageFactory.Page);
        }

        public Page? GetPage()
        {
            return page;
        }

        public void OnDeviceConfigChange(Page changes)
        {
            CheckInitialized();

            var zwaveData = zwaveConnection.GetDeviceZWaveData(this.deviceOrFeatureRef);

            foreach (var view in changes.Views)
            {
                var id = ZWaveParameterFromId(view.Id);

                var parameterInfo = Data.Parameters.FirstOrDefault(x => x.Id == id);
                if ((parameterInfo == null) || (parameterInfo.Size == 0))
                {
                    throw new Exception("Z-wave parameter information not found");
                }

                long? value = null;

                if (view is InputView inputView)
                {
                    var temp = Hspi.Utils.StringConverter.TryGetFromString<long>(view.GetStringValue());

                    if (temp.HasValue)
                    {
                        value = temp;
                    }
                    else
                    {
                        throw new InvalidValueForTypeException(Invariant($"Value not a integer for {parameterInfo.Label}"));
                    }
                }
                else if (view is SelectListView selectListView)
                {
                    string selection = selectListView.GetStringValue();
                    if (int.TryParse(selection, NumberStyles.AllowTrailingWhite |
                                                NumberStyles.AllowTrailingWhite,
                                     CultureInfo.InvariantCulture, out var temp))
                    {
                        value = parameterInfo?.Options?[temp].Value;
                    }
                    else
                    {
                        throw new InvalidValueForTypeException(Invariant($"Value not a integer for {parameterInfo.Label}"));
                    }
                }

                if (value.HasValue)
                {
                    if (parameterInfo.Bitmask != 0)
                    {
                        value = value.Value & parameterInfo.Bitmask;
                    }

                    zwaveConnection.SetConfiguration(zwaveData.HomeId,
                                                     zwaveData.NodeId,
                                                     parameterInfo.ParameterId,
                                                     parameterInfo.Size,
                                                     (int)value.Value); //truncate value if too long
                }
                else
                {
                    throw new InvalidValueForTypeException("Selection/Input not valid");
                }
            }
        }

        private static List<AbstractView> CreateParameterValueControl(ZWaveDeviceParameter parameter, List<string> scripts, string id)
        {
            var views = new List<AbstractView>();

            string label = "Value";

            scripts.Add(Invariant($"<script> const {id}_mask = 0x{parameter.Bitmask:x};</script>"));

            if (parameter.HasOptions && !parameter.HasSubParameters)
            {
                var options = parameter.Options.Select(x => x.Description.StripHtml()).ToList();
                var optionKeys = parameter.Options.Select(x => x.Value.ToString(CultureInfo.InvariantCulture)).ToList();

                scripts.Add(Invariant($"<script> const {id}_optionkeys = [{string.Join(",", optionKeys)}];</script>"));

                var selectListView = new SelectListView(id,
                                                        label,
                                                        options,
                                                        ESelectListType.DropDown);
                views.Add(selectListView);

                // Have not found a away to make it readonly on UI
            }
            else
            {
                var stb2 = new StringBuilder();

                stb2.Append(label);
                if (!parameter.HasSubParameters)
                {
                    stb2.Append('(');
                    stb2.Append(Invariant($" {parameter.Minimum}-{parameter.Maximum} "));

                    if (!string.IsNullOrWhiteSpace(parameter.Units))
                    {
                        stb2.Append(parameter.Units!.StripHtml());
                    }
                    stb2.Append(')');
                }

                var inputView = new InputView(id, stb2.ToString(), EInputType.Text);
                views.Add(inputView);

                if (parameter.ReadOnly)
                {
                    scripts.Add(Invariant($"<script>$(\"#{id}\").attr('readonly', 'readonly');</script>"));
                }
            }

            return views;
        }

        private static string CreateZWaveParameterId(int parameter)
        {
            return Invariant($"{ZWaveParameterPrefix}{parameter}");
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

        private PageFactory AddParameters(PageFactory page, List<string> scripts,
                                          string homeId, byte nodeId, bool listening)
        {
            if (Data?.Parameters != null && Data?.Parameters.Count > 0)
            {
                var parametersView = new ViewGroup(NewId(), string.Empty);

                string? allButtonId = null;
                if (Data.HasRefreshableParameters)
                {
                    page = CreateAllParameterRefreshButton(page, scripts, parametersView.Id, out allButtonId);
                }

                if (!listening)
                {
                    page = page.WithLabel(NewId(), "Device is non-listening one. Please wake the device to refresh or uppdate the parameters.");
                }

                foreach (var parameter in Data.Parameters)
                {
                    string parameterLabel = Invariant($"{Bootstrap.ApplyStyle(Data.LabelForParameter(parameter.ParameterId), Bootstrap.Style.TextBold)}(#{parameter.ParameterId})");

                    var currentViews = CreateGetSetViewsForParameter(scripts, parameter, homeId, nodeId);
                    var detailsLabel = CreateDescriptionViewForParameter(parameter.ParameterId);

                    parametersView.AddView(AddRawHtml(parameterLabel, false));
                    parametersView.AddViews(currentViews);
                    if (detailsLabel != null)
                    {
                        parametersView.AddView(detailsLabel);
                    }
                }

                page = page.WithView(parametersView);

                if (listening && allButtonId!= null)
                {
                    scripts.Add(string.Format(CultureInfo.InvariantCulture, HtmlSnippets.ClickRefreshButtonScript, parametersView.Id, allButtonId));
                }
            }

            return page;
        }

        private LabelView AddRawHtml(string value, bool asTitle, string? id = null)
        {
            var html = Invariant($"<span style=\"font-size:small;\">{value}</span>");
            return new LabelView(id ?? NewId(),
                                 asTitle ? html : string.Empty,
                                 asTitle ? string.Empty : value);
        }

        [MemberNotNull(nameof(Data))]
        private void CheckInitialized()
        {
            if (Data == null)
            {
                throw new InvalidOperationException("Existing ZWave data is null");
            }
        }

        private PageFactory CreateAllParameterRefreshButton(PageFactory page,
                                                            List<string> scripts,
                                                            string containerToClickButtonId,
                                                            out string allButtonId)
        {
            scripts.Add(HtmlSnippets.PostForRefreshScript);

            allButtonId = NewId();
            string allButton =
                string.Format(CultureInfo.InvariantCulture, "<button id=\"{1}\" type=\"button\" class=\"btn btn-secondary\" onclick=\"refreshAllZWaveParameters('{0}')\"> Refresh all parameters</button>",
                                containerToClickButtonId, allButtonId);

            page = page.WithLabel(NewId(), string.Empty, allButton);
            scripts.Add(HtmlSnippets.AllParametersScript);
            return page;
        }

        private LabelView? CreateDescriptionViewForParameter(int parameterId)
        {
            CheckInitialized();

            var list = Data.DescriptionForParameter(parameterId);
            if (list.Count > 0)
            {
                string rows = Bootstrap.MakeMultipleRows(list);
                string description = Bootstrap.ApplyStyle(rows,
                                                          Bootstrap.Style.TextLight,
                                                          Bootstrap.Style.TextWrap);
                var detailsLabel = AddRawHtml(description, true);
                return detailsLabel;
            }
            else
            {
                return null;
            }
        }

        private List<AbstractView> CreateGetSetViewsForParameter(List<string> scripts,
                                                                 ZWaveDeviceParameter parameter,
                                                                 string homeId, byte nodeId)
        {
            var views = new List<AbstractView>();

            var elementId = CreateZWaveParameterId(parameter.Id);
            string currentMessageValueId = elementId + "_message";
            string currentWrapperControlValueId = elementId + "-par";

            var controlViews = CreateParameterValueControl(parameter, scripts, elementId);
            if (parameter.ReadOnly)
            {
                string readonlyMessage = Bootstrap.ApplyStyle("Read only parameter", Bootstrap.Style.TextItalic);
                controlViews.Add(AddRawHtml(readonlyMessage, false));
            }

            views.AddRange(controlViews);

            var topMessage = parameter.WriteOnly ? "Write Only parameter" : "Value not retrieved";
            string notRetrievedMessage = Invariant($"<span id=\"{currentMessageValueId}\">{topMessage}{NewLine}</span>");
            views.Add(AddRawHtml(Bootstrap.ApplyStyle(notRetrievedMessage, Bootstrap.Style.TextItalic), false));

            if (!parameter.WriteOnly)
            {
                scripts.Add(Invariant($"<script>$('#{currentWrapperControlValueId}').hide()</script>"));
                string refreshButton =
                        string.Format(CultureInfo.InvariantCulture, "<button type =\"button\" class=\"btn btn-secondary refresh-z-wave waves-effect waves-light\" onclick=\"refreshZWaveParameter('{0}',{1},{2},'{3}','{4}','{5}')\">Refresh</button>",
                                      homeId, nodeId, parameter.ParameterId, currentMessageValueId, currentWrapperControlValueId, elementId);

                views.Add(AddRawHtml(refreshButton, false));
            }

            return views;
        }

        private string NewId()
        {
            return Invariant($"z_wave_parameter_{id++}");
        }

        private void SetPage(Page? value)
        {
            page = value;
        }

        private const string NewLine = "<BR>";
        private const string ZWaveParameterPrefix = "zw_parameter_";
        private readonly int deviceOrFeatureRef;
        private readonly Func<ZWaveData, Task<ZWaveInformation>> factoryForZWaveInformation;
        private readonly IZWaveConnection zwaveConnection;
        private int id = 0;
        private Page? page;
    }
}