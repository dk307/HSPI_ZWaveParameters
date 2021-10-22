using static System.FormattableString;

#nullable enable

namespace Hspi
{
    internal static class BootstrapHtmlHelper
    {
        public static string MakeBolder(string data)
        {
            return Invariant($"<p class=\"font-weight-bolder\">{data}</p>");
        }

        public static string MakeBold(string data)
        {
            return Invariant($"<p class=\"font-weight-bold\">{data}</p>");
        }

        public static string MakeNormal(string data)
        {
            return Invariant($"<p class=\"font-weight-normal\">{data}</p>");
        }

        public static string MakeCollapsibleCard(string id, string header, string body)
        {
            return string.Format(Resource.CollapseHeader, id, header, body);
        }
    }
}