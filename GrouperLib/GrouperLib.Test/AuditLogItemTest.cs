using System;
using Xunit;
using GrouperLib.Core;
using Newtonsoft.Json;

namespace GrouperLib.Test
{
    public class AuditLogItemTest
    {
        private static readonly Guid documentId = Guid.Parse("3cbc0481-23b0-4860-a58a-7a723ee250c5");
        private static readonly string actor = "Actor";
        private static readonly string action = "Action";
        private static readonly string info = "Additional information";

        [Fact]
        public void TestAuditLogItemConstruction()
        {
            DateTime now = DateTime.Now;
            AuditLogItem logItem = new AuditLogItem(now, documentId, actor, action, info);
            Assert.Equal(now, logItem.LogTime);
            Assert.Equal(documentId, logItem.DocumentId);
            Assert.Equal(actor, logItem.Actor);
            Assert.Equal(action, logItem.Action);
            Assert.Equal(info, logItem.AdditionalInformation);
        }

        [Fact]
        public void TestAuditLogItemConstructionWithoutActor()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AuditLogItem(DateTime.Now, documentId, actor: null, action, null)
            );
        }

        [Fact]
        public void TestAuditLogItemConstructionWithoutAction()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AuditLogItem(DateTime.Now, documentId, actor, action: null, null)
            );
        }

        [Fact]
        public void TestAuditLogItemConstructionWithoutTime()
        {
            DateTime time = DateTime.Now;
            AuditLogItem logItem = new AuditLogItem(documentId, actor, action, info);
            Assert.True(logItem.LogTime >= time);
        }

        [Fact]
        public void TestAuditLogItemSerialization()
        {
            string validJson = @"{
  ""logTime"": ""2020-11-19T21:28:18.3926113+01:00"",
  ""documentId"": ""00000000-0000-0000-0000-000000000000"",
  ""actor"": ""Actor"",
  ""action"": ""Action"",
  ""additionalInformation"": ""Additional information""
}";
            DateTime time = DateTime.Parse("2020-11-19T21:28:18.3926113+01:00");
            AuditLogItem logItem = new AuditLogItem(time, Guid.Empty, actor, action, info);
            string json = JsonConvert.SerializeObject(logItem, Formatting.Indented);
            Assert.Equal(validJson, json);
        }
    }
}
