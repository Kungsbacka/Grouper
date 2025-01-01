using GrouperLib.Core;

namespace GrouperLib.Database;

public sealed class GrouperDocumentEntry
{
    public GrouperDocument Document { get; }
    public Guid GroupId => Document.GroupId;
    public string GroupName => Document.GroupName;
    public int Revision { get; }
    public DateTime RevisionCreated { get; }
    public bool IsPublished { get; }
    public bool IsDeleted { get; }
    public IList<string> Tags => _tags.AsReadOnly();
    private readonly List<string> _tags;

    public GrouperDocumentEntry(GrouperDocument document, int revision, DateTime revisionCreated, bool isPublished, bool isDeleted, string[]? tags)
    {
        if (revision < 1)
        {
            throw new ArgumentException("Revision cannot be less than one", nameof(revision));
        }
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Revision = revision;
        RevisionCreated = revisionCreated;
        IsPublished = isPublished;
        IsDeleted = isDeleted;
        _tags = tags == null ? [] : [..tags];
    }
}