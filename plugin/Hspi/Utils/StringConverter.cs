using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Hspi.Utils
{
    internal static class StringConverter
    {
        public static T? TryGetFromString<T>(string value) where T : struct
        {
            var converter = TypeDescriptor.GetConverter(typeof(T));

            if (converter.IsValid(value))
            {
                return (T)(converter.ConvertFromInvariantString(value));
            }
            return null;
        }
    }
}
