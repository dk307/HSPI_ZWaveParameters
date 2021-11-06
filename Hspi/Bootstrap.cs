﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.FormattableString;

#nullable enable

namespace Hspi
{
    internal static class Bootstrap
    {
        public enum Style
        {
            TextBold,
            TextBolder,
            TextItalic,
            TextNormal,
            TextLight,
            TextWrap,
            TextNoWrap,
            AlignMiddle,
            HideOnXS,
        }

        public static string ApplyStyle(string data, params Style[] styles)
        {
            var classes = styles.Select(x => styleClass[x]);
            return Invariant($"<span class=\"{string.Join(" ", classes)}\">{data}</span>");
        }

        public static string MakeInfoHyperlinkInAnotherTab(string data, Uri link)
        {
            return Invariant($"<a href=\"{link}\" class=\"link-info\" target=\"_blank\">{data}</a>");
        }

        public static string MakeMultipleRows(IEnumerable<string> values)
        {
            return string.Join("<BR>", values);
        }

        private static readonly Dictionary<Style, string> styleClass = new()
        {
            { Style.TextBold, "font-weight-bold" },
            { Style.TextBolder, "font-weight-bolder" },
            { Style.TextItalic, "font-italic" },
            { Style.TextNormal, "font-weight-normal" },
            { Style.TextLight, "font-weight-light" },
            { Style.TextWrap, "text-wrap" },
            { Style.TextNoWrap, "text-nowrap" },
            { Style.AlignMiddle, "align-middle" },
            { Style.HideOnXS, "d-none d-sm-block" },
        };
    }
}