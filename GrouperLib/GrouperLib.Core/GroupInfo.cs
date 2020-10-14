using System;

namespace GrouperLib.Core
{
    public sealed class GroupInfo
    {
        public Guid Id { get; }
        public string DisplayName { get; }
        public GroupStores Store { get;  }

        public GroupInfo(Guid id, string displayName, GroupStores store)
        {
            Id = id;
            DisplayName = displayName;
            Store = store;
        }

        public GroupInfo(string id, string displayName, GroupStores store)
        {
            if (Guid.TryParse(id, out Guid guid))
            {
                Id = guid;
            }
            else
            {
                throw new ArgumentException(nameof(id), "Argument is not a valid GUID");
            }
            DisplayName = displayName;
            Store = store;
        }
    }
}
