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

    /// <summary>
    /// What the current validation rules make of the stored document. Documents are read without
    /// being gated on validation, so that a revision written under an earlier version of the rules
    /// can still be fetched and corrected. An entry always says which it is, and anything that acts
    /// on the document is expected to look first.
    /// </summary>
    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    public bool IsValid => ValidationErrors.Count == 0;

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
        // Validating here rather than taking the result as a parameter is what keeps an entry from
        // claiming to be valid when nobody checked.
        ValidationErrors = document.Validate();
    }
}