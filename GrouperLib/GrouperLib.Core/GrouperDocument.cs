using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace GrouperLib.Core
{
    public sealed class GrouperDocument
    {
        [JsonProperty(PropertyName = "id", Order = 1)]
        public Guid Id { get; }

        [DefaultValue(0)]
        [JsonProperty(PropertyName = "interval", Order = 2, DefaultValueHandling = DefaultValueHandling.Populate)]
        public int ProcessingInterval { get; }

        [JsonProperty(PropertyName = "groupId", Order = 3)]
        public Guid GroupId { get; }

        [JsonProperty(PropertyName = "groupName", Order = 4)]
        public string GroupName { get; }

        [JsonProperty(PropertyName = "store", Order = 5)]
        [JsonConverter(typeof(StringEnumConverter))]
        public GroupStores Store { get; }

        [DefaultValue("KeepExisting")]
        [JsonProperty(PropertyName = "owner", Order = 6, DefaultValueHandling = DefaultValueHandling.Populate)]
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

        public GroupMemberTypes MemberType
        {
            get
            {
                switch (Store)
                {
                    case GroupStores.OnPremAd:
                    case GroupStores.OpenE:
                        return GroupMemberTypes.OnPremAd;
                    default:
                        return GroupMemberTypes.AzureAd;
                }
            }
        }

        [JsonConstructor]
#pragma warning disable IDE0051 // "Remove unused private members" - Used when deserializing from JSON
        private GrouperDocument(Guid id, int interval, Guid groupId, string groupName, GroupStores store, GroupOwnerActions owner, List<GrouperDocumentMember> members)
#pragma warning restore IDE0051 // "Remove unused private members"
        {
            Id = id;
            ProcessingInterval = interval;
            GroupId = groupId;
            GroupName = groupName;
            Store = store;
            Owner = owner;
            _members = members;
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

        public bool ShouldSerializeProcessingInterval() => ProcessingInterval > 0;

        public bool ShouldSerializeMemberType() => false;

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
            GrouperDocument document = DocumentValidator.DeserializeAndValidate(json, validationErrors);
            if (validationErrors.Count > 0)
            {
                return null;
            }
            return document;
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
            if (obj is not GrouperDocument document)
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