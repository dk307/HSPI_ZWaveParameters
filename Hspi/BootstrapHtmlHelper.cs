using System.Text;
using static System.FormattableString;

#nullable enable

namespace Hspi
{
    internal static class BootstrapHtmlHelper
    {
        public static string MakeBolder(string data)
        {
            return Invariant($"<span class=\"font-weight-bolder\">{data}</span>");
        }

        public static string MakeBold(string data)
        {
            return Invariant($"<span class=\"font-weight-bold\">{data}</span>");
        }

        public static string MakeNormal(string data)
        {
            return Invariant($"<span class=\"font-weight-normal\">{data}</span>");
        }

        public static string MakeMultipleRows(params string[] values)
        {
            StringBuilder stb = new StringBuilder();
            stb.Append("<div class=\"container pt-0\">");
            foreach (var value in values)
            {
                stb.Append("<div class=\"row\">");
                stb.Append("<div class=\"col\">");
                stb.Append(value);
                stb.Append("</div>");
                stb.Append("</div>");
            }
            stb.Append("</div>");
            return stb.ToString();
        }

        public static string MakeCollapsibleCard(string id, string header, string body)
        {
            return string.Format(Resource.CollapseHeader, id, header, body);
        }
    }
}