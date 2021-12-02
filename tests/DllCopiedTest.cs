using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class DllCopiedTest
    {
        [TestMethod]
        public void InstallFileHasAllDlls()
        {
            string path = GetInstallFilePath();
            var dllFilesPaths = Directory.GetFiles(path, "*.dll");
            Assert.AreNotEqual(0, dllFilesPaths.Length);

            var dllFiles = dllFilesPaths.Select(x => Path.GetExtension(x)).ToList();

            // Parse install
            var lines = File.ReadLines(Path.Combine(path, "install.txt"));

            var installDlls = lines.SelectMany(x =>
            {
                var filename = x.Split(new[] { ',' }).FirstOrDefault();
                if (Path.GetExtension(filename) == ".dll")
                {
                    return new string[] { filename };
                }
                return new string[] { };
            }).ToList();

            CollectionAssert.AreEquivalent(installDlls, dllFiles);
        }

        private static string GetInstallFilePath()
        {
            string dllPath = Assembly.GetExecutingAssembly().Location;
            var parentDirectory = new DirectoryInfo(Path.GetDirectoryName(dllPath));
            return Path.Combine(parentDirectory.Parent.Parent.Parent.FullName, "plugin", "bin", "debug");
        }
    }
}