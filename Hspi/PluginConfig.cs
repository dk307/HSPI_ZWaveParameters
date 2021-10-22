using HomeSeer.PluginSdk;
using Nito.AsyncEx;
using System.Diagnostics.CodeAnalysis;
using System.Net;

#nullable enable

namespace Hspi
{
    internal sealed class PluginConfig : PluginConfigBase
    {
        public PluginConfig(IHsController HS) : base(HS)
        {         
        }

        
        private readonly AsyncReaderWriterLock configLock = new AsyncReaderWriterLock();
        
    }
}