﻿using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Hspi
{
    public record ZWaveData(int ManufactureId, ushort ProductId, ushort ProductType, byte NodeId, string HomeId, Version Firmware, bool Listening);

    internal interface IZWaveConnection
    {
        Task<int> GetConfiguration(string homeId, byte nodeId, byte param,
                                   CancellationToken cancellationtoken);

        ZWaveData GetDeviceZWaveData(int deviceRef);

        bool IsZwaveDevice(int devOrFeatRef);

        void SetConfiguration(string homeId, byte nodeId, byte param, byte size, int value);
    }
}