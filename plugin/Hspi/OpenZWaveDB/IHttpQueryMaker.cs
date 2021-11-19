﻿using System.Threading;
using System.Threading.Tasks;

namespace Hspi.OpenZWaveDB
{
    public interface IHttpQueryMaker
    {
#pragma warning disable CA1054 // URI-like parameters should not be strings

        Task<string> GetResponseAsString(string url, CancellationToken cancellationToken);

#pragma warning restore CA1054 // URI-like parameters should not be strings
    }
}