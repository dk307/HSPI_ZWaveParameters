using Hspi.Exceptions;
using Hspi.OpenZWaveDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace HSPI_ZWaveParametersTest
{
    [TestClass]
    public class OpenZWaveDBJsonParseTest
    {
        [TestMethod]
        public void EmptyThrowsException()
        {
            Assert.ThrowsException<JsonException>(() => OpenZWaveDatabase.ParseJson(string.Empty));
        }

        [TestMethod]
        public void EmptyObjectThrowsException()
        {
            Assert.ThrowsException<ShowErrorMessageException>(() => OpenZWaveDatabase.ParseJson("{}"));
        }

        [TestMethod]
        public void BareMinimumJsonWorks()
        {
            var obj = OpenZWaveDatabase.ParseJson("{\"database_id\": 113, \"approved\":1, \"deleted\":0}");

            Assert.AreEqual(113, obj.Id);

            Assert.IsNull(obj.Description);
            Assert.IsNull(obj.Label);
            Assert.IsNull(obj.Overview);
            Assert.IsNull(obj.Manufacturer);
            Assert.IsNull(obj.EndPoints);
            Assert.IsNull(obj.Parameters);
        }

        [TestMethod]
        public void BasicJsonWorks()
        {
            var obj = OpenZWaveDatabase.ParseJson(Resource.BasicTopLevelOpenZwaveDBJson);

            Assert.AreEqual(1113, obj.Id);
            Assert.AreEqual("description", obj.Description);
            Assert.AreEqual("LZW30-SN", obj.Label);
            Assert.AreEqual("overview", obj.Overview);
            Assert.AreEqual("Inovelli", obj.Manufacturer.Label);

            Assert.IsNull(obj.EndPoints);
            Assert.IsNull(obj.Parameters);
        }

        [TestMethod]
        public void ParameterwithOption()
        {
            var obj = OpenZWaveDatabase.ParseJson(Resource.ParameterWithOptionOpenZWaveDBJson);

            Assert.IsNotNull(obj.Parameters);
            Assert.AreEqual(1, obj.Parameters.Count);

            Assert.AreEqual(7228, obj.Parameters[0].Id);
            Assert.AreEqual(1, obj.Parameters[0].ParameterId);
            Assert.AreEqual(@"Power On State", obj.Parameters[0].Label);
            Assert.AreEqual(@"Power On State", obj.Parameters[0].Description);
            Assert.AreEqual(@"<p>When power is restored, the switch reverts to either On, Off or last level</p> <p>0 = Returns to State before Power Outage</p> <p>1 = On</p> <p>2 = Off</p>", obj.Parameters[0].Overview);
            Assert.AreEqual(1, obj.Parameters[0].Size);
            Assert.AreEqual(false, obj.Parameters[0].WriteOnly);
            Assert.AreEqual(false, obj.Parameters[0].ReadOnly);
            Assert.AreEqual(0, obj.Parameters[0].Bitmask);
            Assert.AreEqual(0, obj.Parameters[0].Minimum);
            Assert.AreEqual(2, obj.Parameters[0].Maximum);
            Assert.AreEqual(false, obj.Parameters[0].LimitOptions);

            //Options
            Assert.AreEqual(true, obj.Parameters[0].HasOptions);
            Assert.AreEqual(3, obj.Parameters[0].Options.Count);
            Assert.AreEqual(0, obj.Parameters[0].Options[0].Value);
            Assert.AreEqual(1, obj.Parameters[0].Options[1].Value);
            Assert.AreEqual(2, obj.Parameters[0].Options[2].Value);

            Assert.AreEqual("Prior State", obj.Parameters[0].Options[0].Label);
            Assert.AreEqual("On", obj.Parameters[0].Options[1].Label);
            Assert.AreEqual("Off", obj.Parameters[0].Options[2].Label);

            Assert.IsNull(obj.Parameters[0].SubParameters);
        }

        [TestMethod]
        public void ParameterwithNoOption()
        {
            var obj = OpenZWaveDatabase.ParseJson(Resource.ParameterWithNoOptionOpenZWaveDBJson);

            Assert.IsNotNull(obj.Parameters);
            Assert.AreEqual(1, obj.Parameters.Count);

            Assert.AreEqual(7247, obj.Parameters[0].Id);
            Assert.AreEqual(3, obj.Parameters[0].ParameterId);
            Assert.AreEqual(@"Auto Off Timer", obj.Parameters[0].Label);
            Assert.AreEqual(@"Auto Off Timer", obj.Parameters[0].Description);
            Assert.AreEqual(@"<p>Automatically turns the switch off after x amount of seconds</p> <p>0 = Disabled</p> <p>1= 1 second</p> <p>32767 = 32767 seconds</p>", obj.Parameters[0].Overview);
            Assert.AreEqual(2, obj.Parameters[0].Size);
            Assert.AreEqual(false, obj.Parameters[0].WriteOnly);
            Assert.AreEqual(false, obj.Parameters[0].ReadOnly);
            Assert.AreEqual(0, obj.Parameters[0].Bitmask);
            Assert.AreEqual(0, obj.Parameters[0].Minimum);
            Assert.AreEqual(32767, obj.Parameters[0].Maximum);
            Assert.AreEqual(true, obj.Parameters[0].LimitOptions);

            //Options
            Assert.AreEqual(false, obj.Parameters[0].HasOptions);
            Assert.AreEqual(0, obj.Parameters[0].Options.Count);
            Assert.IsNull(obj.Parameters[0].SubParameters);
        }

        [TestMethod]
        public void ParameterWriteOnly()
        {
            var obj = OpenZWaveDatabase.ParseJson(Resource.ParameterWriteOnlyOpenZWaveDBJson);

            Assert.IsNotNull(obj.Parameters);
            Assert.AreEqual(1, obj.Parameters.Count);

            Assert.AreEqual(true, obj.Parameters[0].WriteOnly);
        }

        [TestMethod]
        public void ParameterReadOnly()
        {
            var obj = OpenZWaveDatabase.ParseJson(Resource.ParameterReadOnlyOpenZWaveDBJson);

            Assert.IsNotNull(obj.Parameters);
            Assert.AreEqual(1, obj.Parameters.Count);
            Assert.AreEqual(true, obj.Parameters[0].ReadOnly);
        }

        [TestMethod]
        public void ParameterNeedingLong()
        {
            var obj = OpenZWaveDatabase.ParseJson(Resource.ParameterWithLongValues);

            Assert.IsNotNull(obj.Parameters);
            Assert.AreEqual(1, obj.Parameters.Count);
            Assert.AreEqual(4294967103, obj.Parameters[0].Default);
            Assert.AreEqual(4294967103, obj.Parameters[0].Maximum);
            Assert.AreEqual(4294967103, obj.Parameters[0].Default);
        }

        [TestMethod]
        public void EndPointsAreParsed()
        {
            var obj = OpenZWaveDatabase.ParseJson(Resource.HomeseerDimmerOpenZWaveDBFullJson);

            Assert.IsNotNull(obj.EndPoints);
            Assert.AreEqual(1, obj.EndPoints.Count);

            Assert.AreEqual(16, obj.EndPoints[0].CommandClass.Count);

            // check set command class
            Assert.AreEqual(true, obj.EndPoints[0].CommandClass[10].IsSetCommand);
            Assert.AreEqual(13, obj.EndPoints[0].CommandClass[10].Channels.Count);

            Assert.AreEqual(31, obj.EndPoints[0].CommandClass[10].Channels[12].ParameterId);
            Assert.AreEqual("Status mode LEDs Blink status (Bitmask)", obj.EndPoints[0].CommandClass[10].Channels[12].Label);
        }
    }
}