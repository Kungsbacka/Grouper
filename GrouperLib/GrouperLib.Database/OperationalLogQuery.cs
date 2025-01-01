using GrouperLib.Core;

namespace GrouperLib.Database;

public sealed class OperationalLogQuery
{
    public int Count { get; set; } = int.MaxValue;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? TargetId { get; set; }
    private GroupMemberOperation? _operation;
    public GroupMemberOperation? Operation
    {
        get => _operation;

        set
        {
            if (value == GroupMemberOperation.None)
            {
                throw new ArgumentException("Log items with operation 'None' are not stored in the database and cannot be used as a search filter.", nameof(value));
            }
            _operation = value;
        }
    }
    public string? TargetDisplayNameContains { get; set; }
    public string? GroupDisplayNameContains { get; set; }
}