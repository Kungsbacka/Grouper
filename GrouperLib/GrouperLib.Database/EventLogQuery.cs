using GrouperLib.Core;

namespace GrouperLib.Database;

public sealed class EventLogQuery
{
    public int Count { get; set; } = int.MaxValue;
    public LogLevel? LogLevel { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid? GroupId { get; set; }
    public string? MessageContains { get; set; }
    public string? GroupDisplayNameContains { get; set; }
}