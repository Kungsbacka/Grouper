using Newtonsoft.Json;
using System;

namespace GrouperLib.Core
{
    public sealed class GroupInfo
    {
        [JsonProperty(PropertyName = "id", Order = 1)]
        public Guid Id { get; }

        [JsonProperty(PropertyName = "displayName", Order = 2)]
        public string DisplayName { get; }

        [JsonProperty(PropertyName = "store", Order = 3)]
        public GroupStore Store { get; }

        public GroupInfo(Guid id, string displayName, GroupStore store)
        {
            Id = id;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Store = store;
        }

        public GroupInfo(string id, string displayName, GroupStore store)
        {
            if (Guid.TryParse(id, out Guid guid))
            {
                Id = guid;
            }
            else
            {
                throw new ArgumentException("Argument is not a valid GUID", nameof(id));
            }
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Store = store;
        }
    }
}
