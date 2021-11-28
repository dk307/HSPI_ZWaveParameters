﻿using Hspi.Exceptions;
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

            Assert.AreEqual(obj.Id, 113);

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

            Assert.AreEqual(obj.Id, 1113);
            Assert.AreEqual(obj.Description, "description");
            Assert.AreEqual(obj.Label, "LZW30-SN");
            Assert.AreEqual(obj.Overview, "overview");
            Assert.AreEqual(obj.Manufacturer.Label, "Inovelli");

            Assert.IsNull(obj.EndPoints);
            Assert.IsNull(obj.Parameters);
        }

        [TestMethod]
        public void ParameterwithOption()
        {
            var obj = OpenZWaveDatabase.ParseJson(Resource.ParameterWithOptionOpenZWaveDBJson);

            Assert.IsNotNull(obj.Parameters);
            Assert.AreEqual(1, obj.Parameters.Count);

            Assert.AreEqual(obj.Parameters[0].Id, 7228);
            Assert.AreEqual(obj.Parameters[0].ParameterId, 1);
            Assert.AreEqual(obj.Parameters[0].Label, @"Power On State");
            Assert.AreEqual(obj.Parameters[0].Description, @"Power On State");
            Assert.AreEqual(obj.Parameters[0].Overview, @"<p>When power is restored, the switch reverts to either On, Off or last level</p> <p>0 = Returns to State before Power Outage</p> <p>1 = On</p> <p>2 = Off</p>");
            Assert.AreEqual(obj.Parameters[0].Size, 1);
            Assert.AreEqual(obj.Parameters[0].WriteOnly, false);
            Assert.AreEqual(obj.Parameters[0].ReadOnly, false);
            Assert.AreEqual(obj.Parameters[0].Bitmask, 0);
            Assert.AreEqual(obj.Parameters[0].Minimum, 0);
            Assert.AreEqual(obj.Parameters[0].Maximum, 2);
            Assert.AreEqual(obj.Parameters[0].LimitOptions, false);

            //Options
            Assert.AreEqual(obj.Parameters[0].HasOptions, true);
            Assert.AreEqual(obj.Parameters[0].Options.Count, 3);
            Assert.AreEqual(obj.Parameters[0].Options[0].Value, 0);
            Assert.AreEqual(obj.Parameters[0].Options[1].Value, 1);
            Assert.AreEqual(obj.Parameters[0].Options[2].Value, 2);

            Assert.AreEqual(obj.Parameters[0].Options[0].Label, "Prior State");
            Assert.AreEqual(obj.Parameters[0].Options[1].Label, "On");
            Assert.AreEqual(obj.Parameters[0].Options[2].Label, "Off");

            Assert.IsNull(obj.Parameters[0].SubParameters);
        }

        [TestMethod]
        public void ParameterwithNoOption()
        {
            var obj = OpenZWaveDatabase.ParseJson(Resource.ParameterWithNoOptionOpenZWaveDBJson);

            Assert.IsNotNull(obj.Parameters);
            Assert.AreEqual(obj.Parameters.Count, 1);

            Assert.AreEqual(obj.Parameters[0].Id, 7247);
            Assert.AreEqual(obj.Parameters[0].ParameterId, 3);
            Assert.AreEqual(obj.Parameters[0].Label, @"Auto Off Timer");
            Assert.AreEqual(obj.Parameters[0].Description, @"Auto Off Timer");
            Assert.AreEqual(obj.Parameters[0].Overview, @"<p>Automatically turns the switch off after x amount of seconds</p> <p>0 = Disabled</p> <p>1= 1 second</p> <p>32767 = 32767 seconds</p>");
            Assert.AreEqual(obj.Parameters[0].Size, 2);
            Assert.AreEqual(obj.Parameters[0].WriteOnly, false);
            Assert.AreEqual(obj.Parameters[0].ReadOnly, false);
            Assert.AreEqual(obj.Parameters[0].Bitmask, 0);
            Assert.AreEqual(obj.Parameters[0].Minimum, 0);
            Assert.AreEqual(obj.Parameters[0].Maximum, 32767);
            Assert.AreEqual(obj.Parameters[0].LimitOptions, true);

            //Options
            Assert.AreEqual(obj.Parameters[0].HasOptions, false);
            Assert.AreEqual(obj.Parameters[0].Options.Count, 0);
            Assert.IsNull(obj.Parameters[0].SubParameters);
        }

        [TestMethod]
        public void ParameterWriteOnly()
        {
            var obj = OpenZWaveDatabase.ParseJson(Resource.ParameterWriteOnlyOpenZWaveDBJson);

            Assert.IsNotNull(obj.Parameters);
            Assert.AreEqual(obj.Parameters.Count, 1);

            Assert.AreEqual(obj.Parameters[0].WriteOnly, true);
        }

        [TestMethod]
        public void ParameterReadOnly()
        {
            var obj = OpenZWaveDatabase.ParseJson(Resource.ParameterReadOnlyOpenZWaveDBJson);

            Assert.IsNotNull(obj.Parameters);
            Assert.AreEqual(obj.Parameters.Count, 1);
            Assert.AreEqual(obj.Parameters[0].ReadOnly, true);
        }


        [TestMethod]
        public void ParameterNeedingLong()
        {
            var obj = OpenZWaveDatabase.ParseJson(Resource.ParameterWithLongValues);

            Assert.IsNotNull(obj.Parameters);
            Assert.AreEqual(obj.Parameters.Count, 1);
            Assert.AreEqual(obj.Parameters[0].Default, 4294967103);
            Assert.AreEqual(obj.Parameters[0].Maximum, 4294967103);
            Assert.AreEqual(obj.Parameters[0].Default, 4294967103);
        }

        [TestMethod]
        public void EndPointsAreParsed()
        {
            var obj = OpenZWaveDatabase.ParseJson(Resource.HomeseerDimmerOpenZWaveDBFullJson);

            Assert.IsNotNull(obj.EndPoints);
            Assert.AreEqual(obj.EndPoints.Count, 1);

            Assert.AreEqual(obj.EndPoints[0].CommandClass.Count, 16);

            // check set command class
            Assert.AreEqual(obj.EndPoints[0].CommandClass[10].IsSetCommand, true);
            Assert.AreEqual(obj.EndPoints[0].CommandClass[10].Channels.Count, 13);

            Assert.AreEqual(obj.EndPoints[0].CommandClass[10].Channels[12].ParameterId, 31);
            Assert.AreEqual(obj.EndPoints[0].CommandClass[10].Channels[12].Label, "Status mode LEDs Blink status (Bitmask)");
        }
    }
}