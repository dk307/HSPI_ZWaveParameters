using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

#nullable enable

namespace Hspi.Utils
{
    internal static class EnumHelper
    {
        /// <summary>
        /// Returns the value of the DescriptionAttribute if the specified Enum value has one.
        /// If not, returns the ToString() representation of the Enum value.
        /// </summary>
        /// <param name="value">The Enum to get the description for</param>
        /// <returns></returns>
        public static string GetDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }

        public static T? GetAttribute<T>(Enum value) where T : Attribute
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            var attributes = (T[])fi.GetCustomAttributes(typeof(T), false);
            if (attributes.Length > 0)
                return attributes[0];
            else
                return null;
        }

        public static IEnumerable<T> GetValues<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }
    }
}