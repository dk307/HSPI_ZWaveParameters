using Hspi.OpenZWaveDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class OfflineOpenZWaveDatabaseTest
    {
        [TestMethod]
        public async Task CreateThrowsOnFound()
        {
            var mock = new Mock<IHttpQueryMaker>();
            OfflineOpenZWaveDatabase offlineOpenZWaveDatabase = new(mock.Object, GetDatabasePath());
            var _ = offlineOpenZWaveDatabase.StartLoadAsync(CancellationToken.None);

           await Assert.ThrowsExceptionAsync<Exception>(() => offlineOpenZWaveDatabase.Create(0x783, 243,
                                                                  234, Version.Parse("4.3"),
                                                                  CancellationToken.None));

        }

        [DataTestMethod]
        [DataRow(12, 0x4447, 0x3036, "5.7", "806.json", DisplayName = "Older Homeseer Dimmer")]
        [DataRow(12, 0x4447, 0x3036, "5.10", "806.json", DisplayName = "Default picks first")]
        [DataRow(12, 0x4447, 0x3036, "5.13", "1040.json", DisplayName = "Correct firmware is selected")]
        public async Task Create(int manufacturerId, int productType, int productId,
                             string firmware, string fromFile)
        {
            string fromFilePath = Path.Combine(GetDatabasePath(), fromFile);
            var zWaveInformation = OpenZWaveDatabase.ParseJson(File.ReadAllText(fromFilePath));

            var mock = new Mock<IHttpQueryMaker>();
            OfflineOpenZWaveDatabase offlineOpenZWaveDatabase = new(mock.Object, GetDatabasePath());
            var _ = offlineOpenZWaveDatabase.StartLoadAsync(CancellationToken.None);

            var foundInfo = await offlineOpenZWaveDatabase.Create(manufacturerId, productType,
                                                                  productId, Version.Parse(firmware),
                                                                  CancellationToken.None);

            Assert.AreEqual(zWaveInformation.Id, foundInfo.Id);

            var json1 = JsonSerializer.Serialize(foundInfo);
            var json2 = JsonSerializer.Serialize(zWaveInformation);

            Assert.AreEqual(json1, json2);
        }

        [TestMethod]
        public void CreateWithoutLoad()
        {
            var mock = new Mock<IHttpQueryMaker>();
            OfflineOpenZWaveDatabase offlineOpenZWaveDatabase = new(mock.Object);

            Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                     offlineOpenZWaveDatabase.Create(1, 1, 1, new Version(), CancellationToken.None));
        }

        [TestMethod]
        public async Task Load()
        {
            var mock = new Mock<IHttpQueryMaker>();
            OfflineOpenZWaveDatabase offlineOpenZWaveDatabase = new(mock.Object, GetDatabasePath());
            await offlineOpenZWaveDatabase.StartLoadAsync(CancellationToken.None);

            Assert.AreEqual(offlineOpenZWaveDatabase.EntriesCount, 1665);
        }

        private string GetDatabasePath([CallerFilePath] string path = "")
        {
            return Path.Combine(Path.GetDirectoryName(path), "..", "plugin", "db");
        }
    }
}