﻿using System;
using System.Text.Json.Serialization;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal record ZWaveCommandClassChannel
    {
        [JsonPropertyName("config")]
        public string? Config { get; init; }

        [JsonPropertyName("label")]
        public string? Label { get; init; }

        [JsonPropertyName("overview")]
        public string? Overview { get; init; }

        [JsonIgnore]
        public int? ParameterId
        {
            get
            {
                var list = Config?.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (list != null && list.Length == 2)
                {
                    if (int.TryParse(list[1], out var value))
                    {
                        return value;
                    }
                }
                return null;
            }
        }
    }
}