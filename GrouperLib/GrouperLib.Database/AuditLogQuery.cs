using System;

namespace GrouperLib.Database
{
    public sealed class AuditLogQuery
    {
        public int Count { get; set; } = int.MaxValue;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? DocumentId { get; set; }
        public string? ActorContains { get; set; }
        public string? ActionContains { get; set; }
    }
}
