using System.Collections.Generic;
using System.Collections.Immutable;

#nullable enable

namespace Hspi.Utils
{
    internal static class MiscHelper
    {
        public static TValue GetValueOrDefault<TKey, TValue> (
                this IDictionary<TKey, TValue> dictionary,
                TKey key,
                TValue defaultValue)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }

        public static TValue GetValueOrDefault<TKey, TValue> (
                this ImmutableDictionary<TKey, TValue> dictionary,
                TKey key,
                TValue defaultValue) where TKey: notnull
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
    }
}