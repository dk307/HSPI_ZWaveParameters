using HomeSeer.Jui.Views;
using Hspi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class SettingsPagesTest
    {
        [TestMethod]
        public void CreateDefault()
        {
            var page = SettingsPages.CreateDefault();

            Assert.IsNotNull(page);

            foreach (var view in page.Views)
            {
                TestHelper.VerifyHtmlValid(view.ToHtml());
            }

            TestHelper.VerifyHtmlValid(page.ToHtml());

            Assert.IsTrue(page.ContainsViewWithId(SettingsPages.PreferOnlineDatabaseId));
            Assert.IsTrue(page.ContainsViewWithId(SettingsPages.LogToFileId));
            Assert.IsTrue(page.ContainsViewWithId(SettingsPages.LoggingDebugId));
            Assert.IsTrue(page.ContainsViewWithId(SettingsPages.ShowSubParameteredValuesAsHexId));
        }

        [DataTestMethod]
        [DataRow(false, false, false, false)]
        [DataRow(false, false, false, true)]
        [DataRow(true, false, false, false)]
        [DataRow(false, true, false, false)]
        [DataRow(true, false, true, false)]
        [DataRow(true, true, true, true)]
        public void DefaultValues(bool preferOnline,
                                  bool enableDefaultLogging,
                                  bool logToFileEnable,
                                  bool showSubParameteredValuesAsHex)
        {
            var settingsCollection = new SettingsCollection
            {
                SettingsPages.CreateDefault(preferOnline, enableDefaultLogging, logToFileEnable, showSubParameteredValuesAsHex)
            };

            var settingPages = new SettingsPages(settingsCollection);

            Assert.AreEqual(settingPages.PreferOnlineDatabase, preferOnline);
            Assert.AreEqual(settingPages.DebugLoggingEnabled, enableDefaultLogging);
            Assert.AreEqual(settingPages.LogtoFileEnabled, logToFileEnable);
            Assert.AreEqual(settingPages.ShowSubParameteredValuesAsHex, showSubParameteredValuesAsHex);
        }

        [TestMethod]
        public void OnSettingChangeWithNoChange()
        {
            var settingsCollection = new SettingsCollection
            {
                SettingsPages.CreateDefault()
            };
            var settingPages = new SettingsPages(settingsCollection);

            Assert.IsFalse(settingPages.OnSettingChange(new ToggleView("id", "name")));
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void OnSettingChangeWithDebugLoggingChange(bool initialValue)
        {
            var settingsCollection = new SettingsCollection
            {
                SettingsPages.CreateDefault(enableDebugLoggingDefault: initialValue)
            };
            var settingPages = new SettingsPages(settingsCollection);

            ToggleView changedView = new(SettingsPages.LoggingDebugId, "name", !initialValue);
            Assert.IsTrue(settingPages.OnSettingChange(changedView));
            Assert.AreEqual(settingPages.DebugLoggingEnabled, !initialValue);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void OnSettingChangeWithLogtoFileChange(bool initialValue)
        {
            var settingsCollection = new SettingsCollection
            {
                SettingsPages.CreateDefault(logToFileDefault: initialValue)
            };
            var settingPages = new SettingsPages(settingsCollection);

            ToggleView changedView = new(SettingsPages.LogToFileId, "name", !initialValue);
            Assert.IsTrue(settingPages.OnSettingChange(changedView));
            Assert.AreEqual(settingPages.LogtoFileEnabled, !initialValue);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void OnSettingChangeWithPreferOnlineChange(bool initialValue)
        {
            var settingsCollection = new SettingsCollection
            {
                SettingsPages.CreateDefault(preferOnlineDatabaseDefault: initialValue)
            };
            var settingPages = new SettingsPages(settingsCollection);

            ToggleView changedView = new(SettingsPages.PreferOnlineDatabaseId, "name", !initialValue);
            Assert.IsTrue(settingPages.OnSettingChange(changedView));
            Assert.AreEqual(settingPages.PreferOnlineDatabase, !initialValue);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void OnSettingShowSubParameteredValuesAsHexChange(bool initialValue)
        {
            var settingsCollection = new SettingsCollection
            {
                SettingsPages.CreateDefault(showSubParameteredValuesAsHexDefault: initialValue)
            };
            var settingPages = new SettingsPages(settingsCollection);

            ToggleView changedView = new(SettingsPages.ShowSubParameteredValuesAsHexId, "name", !initialValue);
            Assert.IsTrue(settingPages.OnSettingChange(changedView));
            Assert.AreEqual(settingPages.ShowSubParameteredValuesAsHex, !initialValue);
        }
    }
}