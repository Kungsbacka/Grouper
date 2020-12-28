using System;
using Xunit;
using GrouperLib.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GrouperLib.Test
{
    public class AuditLogItemTest
    {
        private static readonly Guid documentId = Guid.Parse("3cbc0481-23b0-4860-a58a-7a723ee250c5");
        private static readonly string actor = "Actor";
        private static readonly string action = "Action";
        private static readonly string info = "Additional information";
        private static readonly DateTime time = DateTime.Parse("2020-11-19T21:28:18.3926113+01:00");

        [Fact]
        public void TestConstruction()
        {
            AuditLogItem logItem = new AuditLogItem(time, documentId, actor, action, info);
            Assert.Equal(time, logItem.LogTime);
            Assert.Equal(documentId, logItem.DocumentId);
            Assert.Equal(actor, logItem.Actor);
            Assert.Equal(action, logItem.Action);
            Assert.Equal(info, logItem.AdditionalInformation);
        }

        [Fact]
        public void TestConstructionWithoutActor()
        {
            Assert.Throws<ArgumentNullException>(() => new AuditLogItem(time, documentId, actor: null, action, null));
        }

        [Fact]
        public void TestConstructionWithoutAction()
        {
            Assert.Throws<ArgumentNullException>(() => new AuditLogItem(time, documentId, actor, action: null, null));
        }

        [Fact]
        public void TestConstructionWithoutTime()
        {
            DateTime now = DateTime.Now;
            AuditLogItem logItem = new AuditLogItem(documentId, actor, action, info);
            Assert.True(logItem.LogTime >= now);
        }

        [Fact]
        public void TestSerializedNames()
        {
            AuditLogItem logItem = new AuditLogItem(time, documentId, actor, action, info);
            string json = JsonConvert.SerializeObject(logItem);
            JObject obj = JObject.Parse(json);
            Assert.True(obj.ContainsKey("logTime"));
            Assert.True(obj.ContainsKey("documentId"));
            Assert.True(obj.ContainsKey("actor"));
            Assert.True(obj.ContainsKey("action"));
            Assert.True(obj.ContainsKey("additionalInformation"));
        }
    }
}
