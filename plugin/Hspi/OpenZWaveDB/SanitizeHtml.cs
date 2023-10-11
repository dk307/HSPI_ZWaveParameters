using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Ganss.Xss;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal sealed class SanitizeHtml
    {
        public SanitizeHtml()
        {
            HtmlSanitizerOptions htmlSanitizerOptions = new()
            {
                AllowedTags = SanitizeHtml.AllowedTags.ToImmutableSortedSet(),
                AllowedCssProperties = { },
            };

            htmlSanitizer = new(htmlSanitizerOptions)
            {
                KeepChildNodes = true,
            };

            HtmlSanitizerOptions htmlSanitizerOptions2 = new()
            {
                AllowedTags = { },
            };

            htmlStriper = new(htmlSanitizerOptions2)
            {
                KeepChildNodes = true,
            };
        }

        public string Sanitize(string html)
        {
            return htmlSanitizer.Sanitize(html);
        }

        public string Strip(string html)
        {
            return htmlStriper.Sanitize(html);
        }

        private static readonly IEnumerable<string> AllowedTags = new[] { "b","br","em","h1","h2","h3","h4","h5",
                                     "h6","i","p","small","strong","sub","sup","ul","ol",
                                     "li","table","tr","td" };

        private readonly Ganss.Xss.HtmlSanitizer htmlSanitizer;
        private readonly Ganss.Xss.HtmlSanitizer htmlStriper;
    }
}