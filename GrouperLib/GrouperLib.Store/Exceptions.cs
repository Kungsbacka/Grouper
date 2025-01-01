namespace GrouperLib.Store;

public class GroupNotFoundException : Exception
{
    public GroupNotFoundException(string message) : base(message)
    {
    }

    public GroupNotFoundException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    public static GroupNotFoundException Create(Guid groupId, Exception? innerException)
    {
        return new GroupNotFoundException($"Group '{groupId}' is not found in store.", innerException);
    }

    public static GroupNotFoundException Create(Guid groupId) => Create(groupId, null);

}

public class MemberNotFoundException : Exception
{
    public MemberNotFoundException(string message) : base(message)
    {
    }

    public MemberNotFoundException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    public static MemberNotFoundException Create(Guid memberId, Exception? innerException)
    {
        return new MemberNotFoundException($"Member '{memberId}' is not found in store.", innerException);
    }

    public static MemberNotFoundException Create(Guid memberId) => Create(memberId, null);
}

public class ObjectAlreadyMemberException : Exception
{
    public ObjectAlreadyMemberException(string message) : base(message)
    {
    }

    public ObjectAlreadyMemberException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    public static ObjectAlreadyMemberException Create(Guid memberId, Guid groupId, Exception? innerException)
    {
        return new ObjectAlreadyMemberException($"Object '{memberId}' is already a member of group '{groupId}'.", innerException);
    }

    public static ObjectAlreadyMemberException Create(Guid memberId, Guid groupId) => Create(memberId, groupId, null);
}
public class ObjectNotMemberException : Exception
{
    public ObjectNotMemberException(string message) : base(message)
    {
    }

    public ObjectNotMemberException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    public static ObjectNotMemberException Create(Guid memberId, Guid groupId, Exception? innerException)
    {
        return new ObjectNotMemberException($"Object '{memberId}' is not a member of group '{groupId}'.", innerException);
    }

    public static ObjectNotMemberException Create(Guid memberId, Guid groupId) => Create(memberId, groupId, null);
}