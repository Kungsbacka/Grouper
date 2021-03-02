using GrouperLib.Core;
using System;
using System.Collections.Generic;

namespace GrouperLib.Database
{
    public sealed class GrouperDocumentEntry
    {
        public GrouperDocument Document { get; }
        public Guid GroupId { get => Document.GroupId; }
        public string GroupName { get => Document.GroupName; }
        public int Revision { get; }
        public DateTime RevisionCreated { get; }
        public bool IsPublished { get; }
        public bool IsDeleted { get; }
        public IList<string> Tags
        {
            get
            {
                return _tags.AsReadOnly();
            }
        }
        private readonly List<string> _tags;

        public GrouperDocumentEntry(GrouperDocument document, int revision, DateTime revisionCreated, bool isPublished, bool isDeleted, string[] tags)
        {
            if (revision < 1)
            {
                throw new ArgumentException(nameof(revision), "Revision cannot be less than one");
            }
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Revision = revision;
            RevisionCreated = revisionCreated;
            IsPublished = isPublished;
            IsDeleted = isDeleted;
            if (tags == null)
            {
                _tags = new List<string>();
            }
            else
            {
                _tags = new List<string>(tags);
            }
        }
    }
}
