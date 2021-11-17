using HomeSeer.Jui.Views;
using Hspi.OpenZWaveDB;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Hspi
{
    internal interface IDeviceConfigPage
    {
        ZWaveInformation? Data { get; }

        Page? GetPage();

        Task BuildConfigPage(CancellationToken cancellationToken);

        void OnDeviceConfigChange(Page changes);
    }
}