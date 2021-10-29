﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal record ZWaveInformation
    {
        [JsonProperty("database_id")]
        public string? Id { get; init; }
        public string? Overview { get; init; }
        public string? Description { get; init; }
        public string? Label { get; init; }
        public ZWaveDeviceManufacturer? Manufacturer { get; init; }
        public IReadOnlyList<ZWaveDeviceParameter>? Parameters { get; init; }


        [JsonIgnore]
        public string FullName
        {
            get
            {
                var listName = new List<string?>
                {
                    Manufacturer?.Label,
                    Description,
                    "(" + Label + ")"
                };

                return string.Join(" ", listName.Where(s => !string.IsNullOrEmpty(s)));
            }
        }

        [JsonIgnore]
        public Uri WebUrl => new(string.Format(webUrlFormat, Id), UriKind.Absolute);

        private const string webUrlFormat = "https://www.opensmarthouse.org/zwavedatabase/{0}";
    }
}