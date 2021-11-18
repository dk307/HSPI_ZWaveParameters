using System.Collections.Generic;

#nullable enable

namespace Hspi.OpenZWaveDB
{
    internal record ZWaveEndPoints
    {
        public IReadOnlyList<ZWaveCommandClass>? CommandClass { get; init; }
    }
}