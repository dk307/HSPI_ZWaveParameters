using Hspi.OpenZWaveDB;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

#nullable enable

namespace Hspi
{
    internal static class ZWaveInformationDBDisplayExtensions
    {
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
                    list.Add(value.SanitizeHtml());
                    list.Add(sizeString);
                }
            }
            else
            {
                var descList = new[] { parameter.Description, parameter.Overview };
                string? longerOne = descList.OrderByDescending(x => x?.Length ?? 0).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(longerOne) && longerOne != parameter.Label)
                {
                    list.Add(longerOne!.SanitizeHtml());
                }

                if (!parameter.HasOptions)
                {
                    list.Add(sizeString);
                    list.Add(Invariant($"Range: {parameter.Minimum} - {parameter.Maximum} {parameter.Units?.StripHtml()}"));
                }

                list.Add(Invariant($"Default: {parameter.DefaultValueDescription.StripHtml()}"));
            }

            return list;
        }

        public static string DisplayFullName(this ZWaveInformation data)
        {
            var listName = new List<string?>
                {
                    data.Manufacturer?.Label,
                    data.Description,
                    "(" + data.Label + ")"
                };

            return string.Join(" ", listName.Where(s => !string.IsNullOrEmpty(s))).StripHtml();
        }

        public static string LabelForParameter(this ZWaveInformation data, int parameterId)
        {
            var parameter = data.Parameters.First(x => x.ParameterId == parameterId);

            if (parameter.HasSubParameters)
            {
                var channel = data.GetCommandClassChannelForParameter(parameterId);
                var value = channel?.Label?.StripHtml();
                if (value != null)
                {
                    return value;
                }
            }

            return parameter.Label?.StripHtml() ?? string.Empty;
        }

        public static string SanitizeHtml(this string html)
        {
            return sanitizer.Sanitize(html);
        }

        public static string StripHtml(this string html)
        {
            return sanitizer.Strip(html);
        }

        public static readonly SanitizeHtml sanitizer = new();
    };
}