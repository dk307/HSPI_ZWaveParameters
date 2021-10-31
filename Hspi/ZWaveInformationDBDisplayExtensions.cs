using Hspi.OpenZWaveDB;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.FormattableString;

#nullable enable

namespace Hspi
{
    internal static class ZWaveInformationDBDisplayExtensions
    {
        public static string LabelForParameter(this ZWaveInformation data, int parameterId)
        {
            var parameter = data.Parameters.First(x => x.ParameterId == parameterId);

            if (parameter.HasSubParameters)
            {
                var channel = data.GetCommandClassChannelForParameter(parameterId);
                var value = channel?.Label;
                if (value != null)
                {
                    return value;
                }
            }

            return parameter.Label ?? string.Empty;
        }

        public static string DisplayFullName(this ZWaveInformation data)
        {
            var listName = new List<string?>
                {
                    data.Manufacturer?.Label,
                    data.Description,
                    "(" + data.Label + ")"
                };

            return string.Join(" ", listName.Where(s => !string.IsNullOrEmpty(s)));
        }

        public static List<string> DescriptionForParameter(this ZWaveInformation data, int parameterId)
        {
            var list = new List<string>();
            var parameter = data.Parameters.First(x => x.ParameterId == parameterId);

            string sizeString = Invariant($"Size: {parameter.Size} Byte(s)");
            if (parameter.HasSubParameters)
            {
                // try description classes for bitmask
                var overallParameter = data.GetCommandClassChannelForParameter(parameter.ParameterId);

                var value = overallParameter?.Overview ?? overallParameter?.Label;
                if (value != null)
                {
                    list.Add(value);
                    list.Add(sizeString);
                }
            }
            else
            {
                var descList = new[] { parameter.Description, parameter.Overview, parameter.Label };
                list.Add(descList.OrderByDescending(x => x?.Length ?? 0).First() ?? string.Empty);

                if (parameter.HasOptions)
                {
                    var stb = new StringBuilder();
                    stb.Append("Options:<BR>");
                    foreach (var option in parameter.Options!)
                    {
                        stb.Append(Invariant($"{option.Value} - {option.Label}<BR>"));
                    }
                    list.Add(stb.ToString());
                }
                else if (!parameter.HasSubParameters)
                {
                    list.Add(Invariant($"Range: {parameter.Minimum} - {parameter.Maximum} {parameter.Units}"));
                }
            }

            return list;
        }
    };
}