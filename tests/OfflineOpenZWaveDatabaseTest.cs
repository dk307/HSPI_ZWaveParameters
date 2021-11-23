using Hspi.OpenZWaveDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class OfflineOpenZWaveDatabaseTest
    {
        [DataTestMethod]
        [DataRow(12, 0x4447, 0x3036, "5.7", "806.json", DisplayName = "Older Homeseer Dimmer")]
        [DataRow(12, 0x4447, 0x3036, "5.10", "806.json", DisplayName = "Default picks first")]
        [DataRow(12, 0x4447, 0x3036, "5.13", "1040.json", DisplayName = "Correct firmware is selected")]
        public async Task Create(int manufacturerId, int productType, int productId,
                             string firmware, string fromFile)
        {
            string fromFilePath = Path.Combine(TestHelper.GetOfflineDatabasePath(), fromFile);
            var zWaveInformation = OpenZWaveDatabase.ParseJson(File.ReadAllText(fromFilePath));

            OfflineOpenZWaveDatabase offlineOpenZWaveDatabase = new(TestHelper.GetOfflineDatabasePath());
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
        public async Task CreateThrowsOnFound()
        {
            var mock = new Mock<IHttpQueryMaker>();
            OfflineOpenZWaveDatabase offlineOpenZWaveDatabase = new(TestHelper.GetOfflineDatabasePath());
            var _ = offlineOpenZWaveDatabase.StartLoadAsync(CancellationToken.None);

            await Assert.ThrowsExceptionAsync<Exception>(() => offlineOpenZWaveDatabase.Create(0x783, 243,
                                                                   234, Version.Parse("4.3"),
                                                                   CancellationToken.None));
        }

        [TestMethod]
        public void CreateWithoutLoad()
        {
            var mock = new Mock<IHttpQueryMaker>(MockBehavior.Strict);
            OfflineOpenZWaveDatabase offlineOpenZWaveDatabase = new(TestHelper.GetOfflineDatabasePath());

            Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                     offlineOpenZWaveDatabase.Create(1, 1, 1, new Version(), CancellationToken.None));
        }

        [TestMethod]
        public async Task Download()
        {
            var dbPath = TestHelper.GetOfflineDatabasePath();

            int maxCount = 5;
            var queryMaker = new Mock<IHttpQueryMaker>(MockBehavior.Strict);

            for (int i = 1; i <= maxCount; i++)
            {
                string iStr = i.ToString(CultureInfo.InvariantCulture);
                string jsonPath = Path.Combine(dbPath, iStr + ".json");
                TestHelper.SetupRequest(queryMaker,
                            "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=" + iStr,
                            File.Exists(jsonPath) ? File.ReadAllText(jsonPath) : "{ \"database_id\":1034, \"approved\":1, \"deleted\":0}");
            }

            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            await OfflineOpenZWaveDatabase.Download(queryMaker.Object, databasePath: path, maxCount: maxCount);

            for (int i = 1; i <= maxCount; i++)
            {
                string path2 = Path.Combine(path, i.ToString(CultureInfo.InvariantCulture) + ".json");
                Assert.IsTrue(File.Exists(path2));
                OpenZWaveDatabase.ParseJson(File.ReadAllText(path2));
            }
        }

        [DataTestMethod]
        [DataRow("{ \"database_id\":1034, \"approved\":0, \"deleted\":0}", DisplayName = "Not Approved")]
        [DataRow("{ \"database_id\":1034, \"approved\":1, \"deleted\":1}", DisplayName = "Deleted")]
        public async Task DownloadIgnoreSomeFiles(string json)
        {
            var queryMaker = new Mock<IHttpQueryMaker>(MockBehavior.Strict);

            TestHelper.SetupRequest(queryMaker,
                        "https://opensmarthouse.org/dmxConnect/api/zwavedatabase/device/read.php?device_id=1",
                       json);

            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            await OfflineOpenZWaveDatabase.Download(queryMaker.Object, databasePath: path, maxCount: 1);

            string path2 = Path.Combine(path, "1.json");
            Assert.IsFalse(File.Exists(path2));
        }

        [TestMethod]
        public async Task Load()
        {
            OfflineOpenZWaveDatabase offlineOpenZWaveDatabase = new(TestHelper.GetOfflineDatabasePath());
            await offlineOpenZWaveDatabase.StartLoadAsync(CancellationToken.None);

            Assert.AreEqual(offlineOpenZWaveDatabase.EntriesCount, 1665);
        }
    }
}