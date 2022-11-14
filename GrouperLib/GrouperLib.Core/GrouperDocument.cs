using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
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
        public GroupStore Store { get; }

        [DefaultValue("KeepExisting")]
        [JsonProperty(PropertyName = "owner", Order = 6, DefaultValueHandling = DefaultValueHandling.Populate)]
        [JsonConverter(typeof(StringEnumConverter))]
        public GroupOwnerAction Owner { get; }

        [JsonProperty(PropertyName = "members", Order = 7)]
        public IReadOnlyCollection<GrouperDocumentMember> Members => _members.AsReadOnly();
        private readonly List<GrouperDocumentMember> _members;

        public GroupMemberType MemberType
        {
            get
            {
                switch (Store)
                {
                    case GroupStore.OnPremAd:
                    case GroupStore.OpenE:
                        return GroupMemberType.OnPremAd;
                    default:
                        return GroupMemberType.AzureAd;
                }
            }
        }

        [JsonConstructor]
        private GrouperDocument(Guid id, int interval, Guid groupId, string groupName, GroupStore store, GroupOwnerAction owner, IReadOnlyCollection<GrouperDocumentMember> members)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException("Parameter cannot be null or empty", nameof(groupName));
            }
            if (members == null)
            {
                throw new ArgumentNullException(nameof(members));
            }
            Id = id;
            ProcessingInterval = interval;
            GroupId = groupId;
            GroupName = groupName;
            Store = store;
            Owner = owner;
            _members = members.Select(m => new GrouperDocumentMember(m)).ToList();
        }

        public bool ShouldSerializeOwner() => Store == GroupStore.AzureAd;

        public bool ShouldSerializeProcessingInterval() => ProcessingInterval > 0;

        public bool ShouldSerializeMemberType() => false;


        public static GrouperDocument Create(Guid id, int interval, Guid groupId, string groupName, GroupStore store, GroupOwnerAction owner, IReadOnlyCollection<GrouperDocumentMember> members, List<ValidationError> validationErrors)
        {
            if (validationErrors == null)
            {
                throw new ArgumentNullException(nameof(validationErrors));
            }
            GrouperDocument document = new(id, interval, groupId, groupName, store, owner, members);
            DocumentValidator.Validate(document, validationErrors);
            if (validationErrors.Count > 0)
            {
                return null;
            }
            return document;
        }

        public static GrouperDocument Create(Guid id, int interval, Guid groupId, string groupName, GroupStore store, GroupOwnerAction owner, IReadOnlyCollection<GrouperDocumentMember> members)
        {
            List<ValidationError> validationErrors = new List<ValidationError>();
            GrouperDocument document = Create(id, interval, groupId, groupName, store, owner, members, validationErrors);
            if (document == null)
            {
                throw new InvalidGrouperDocumentException();
            }
            return document;
        }


        public GrouperDocument CloneWithNewGroupName(string groupName)
        {
            return new GrouperDocument(Id, ProcessingInterval, GroupId, groupName, Store, Owner, Members);
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
                int i = 0;
                foreach (var member in Members)
                {
                    int indent = maxIndent + 4;
                    if (i >= 9)
                    {
                        indent -= (int)Math.Floor(Math.Log10(i + 1));
                    }
                    sb.Append(new string(' ', indent));
                    sb.Append('(');
                    sb.Append(i + 1);
                    sb.Append(") ");
                    sb.Append(member.Action);
                    sb.Append(": ");
                    sb.AppendLine(member.Source.ToString());
                    i++;
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