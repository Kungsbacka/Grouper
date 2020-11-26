using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GrouperLib.Core
{
    public sealed class GrouperDocument
    {
        [JsonProperty(PropertyName = "id", Order = 1)]
        public Guid Id { get; }

        [JsonProperty(PropertyName = "interval", Order = 2)]
        public int ProcessingInterval { get; }

        [JsonProperty(PropertyName = "groupId", Order = 3)]
        public Guid GroupId { get; }

        [JsonProperty(PropertyName = "groupName", Order = 4)]
        public string GroupName { get; }

        [JsonProperty(PropertyName = "store", Order = 5)]
        [JsonConverter(typeof(StringEnumConverter))]
        public GroupStores Store { get; }

        [JsonProperty(PropertyName = "owner", Order = 6)]
        [JsonConverter(typeof(StringEnumConverter))]
        public GroupOwnerActions Owner { get; }

        [JsonProperty(PropertyName = "members", Order = 7)]
        public IList<GrouperDocumentMember> Members
        {
            get
            {
                return _members.AsReadOnly();
            }
        }
        private readonly List<GrouperDocumentMember> _members;

        internal GrouperDocument(DeserializedDocument deserializedDocument)
        {
            Id = Guid.Parse(deserializedDocument.Id);
            ProcessingInterval = deserializedDocument.Interval;
            GroupName = deserializedDocument.GroupName;
            GroupId = Guid.Parse(deserializedDocument.GroupId);
            Store = (GroupStores)Enum.Parse(typeof(GroupStores), deserializedDocument.Store, true);
            Owner = (GroupOwnerActions)Enum.Parse(typeof(GroupOwnerActions), deserializedDocument.Owner, true);
            GroupMemberTypes memberType = Store == GroupStores.OnPremAd ? GroupMemberTypes.OnPremAd : GroupMemberTypes.AzureAd;
            _members = deserializedDocument.Members.Select(m => new GrouperDocumentMember(m, memberType)).ToList();
        }

        private GrouperDocument(GrouperDocument document, string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException("Parameter cannot be null or empty", nameof(groupName));
            }
            GroupName = groupName;
            // Copy all other properties
            Id = document.Id;
            GroupId = document.GroupId;
            Store = document.Store;
            Owner = document.Owner;
            _members = document.Members.Select(m => new GrouperDocumentMember(m)).ToList();
        }

        public bool ShouldSerializeOwner() => Store == GroupStores.AzureAd;

        public bool ShouldSerializeProcessingInterval() =>  ProcessingInterval > 0;

        public GrouperDocument CloneWithNewGroupName(string groupName)
        {
            return new GrouperDocument(this, groupName);
        }

        public string ToJson(Formatting formatting = Formatting.Indented)
        {
            return JsonConvert.SerializeObject(this, formatting);
        }

        public static GrouperDocument FromJson(string json, List<ValidationError> validationErrors)
        {
            if (validationErrors == null)
            {
                throw new ArgumentNullException(nameof(validationErrors));
            }
            DeserializedDocument deserializedDocument = DocumentValidator.DeserializeAndValidate(json, validationErrors);
            if (validationErrors.Count > 0)
            {
                return null;
            }
            return new GrouperDocument(deserializedDocument);
        }

        public static GrouperDocument FromJson(string json)
        {
            List<ValidationError> validationErrors = new List<ValidationError>();
            GrouperDocument document = FromJson(json, validationErrors);
            if (document == null)
            {
                throw new InvalidGrouperDocumentException();
            }
            return document;
        }

        public string ToString(bool logFormat)
        {
            if (logFormat)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("         Group Name: ");
                sb.AppendLine(GroupName);
                sb.Append("        Group Store: ");
                sb.AppendLine(Store.ToString());
                sb.Append("       Owner Action: ");
                sb.AppendLine(Owner.ToString());
                sb.Append("           Group ID: ");
                sb.AppendLine(GroupId.ToString());
                sb.Append("        Document ID: ");
                if (ProcessingInterval > 0)
                {
                    sb.Append("Processing Interval: ");
                    sb.AppendLine(ProcessingInterval.ToString());
                }
                sb.AppendLine(Id.ToString());
                sb.Append("       Member Rules: ");
                sb.AppendLine(Members.Count.ToString());
                int maxIndent = (int)Math.Floor(Math.Log10(Members.Count));
                for (int i = 0; i < Members.Count; i++)
                {
                    int indent = maxIndent + 4;
                    if (i >= 9)
                    {
                        indent -= (int)Math.Floor(Math.Log10(i + 1));
                    }
                    GrouperDocumentMember member = Members[i];
                    sb.Append(new string(' ', indent));
                    sb.Append('(');
                    sb.Append(i + 1);
                    sb.Append(") ");
                    sb.Append(member.Action);
                    sb.Append(": ");
                    sb.AppendLine(member.Source.ToString());
                }
                return sb.ToString();
            }
            return Id.ToString();
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GrouperDocument document))
            {
                return false;
            }
            return Id == document.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}