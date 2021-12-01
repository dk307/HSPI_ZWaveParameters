using Ganss.XSS;
using System;
using System.Collections.Generic;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal sealed class SanitizeHtml
    {
        public SanitizeHtml()
        {
            htmlSanitizer = new(allowedTags: AllowedTags,
                                allowedCssProperties: Array.Empty<string>())
            {
                KeepChildNodes = true,
            };

            htmlStriper = new(allowedTags: Array.Empty<string>(),
                              allowedCssProperties: Array.Empty<string>())
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

        private readonly HtmlSanitizer htmlSanitizer;
        private readonly HtmlSanitizer htmlStriper;
    }
}