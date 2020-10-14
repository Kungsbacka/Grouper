using System;

namespace GrouperLib.Store
{
    public class GroupNotFoundException : Exception
    {
        public GroupNotFoundException(string message) : base(message)
        {
        }

        public GroupNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public static GroupNotFoundException Create(Guid GroupId, Exception innerException)
        {
            return new GroupNotFoundException($"Group with id {GroupId} not found in store.", innerException);
        }

        public static GroupNotFoundException Create(Guid id) => Create(id, null);

    }

    public class MemberNotFoundException : Exception
    {
        public MemberNotFoundException(string message) : base(message)
        {
        }

        public MemberNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public static MemberNotFoundException Create(Guid memberId, Exception innerException)
        {
            return new MemberNotFoundException($"Member with id {memberId} not found in store.", innerException);
        }

        public static MemberNotFoundException Create(Guid memberId) => Create(memberId, null);
    }
}
