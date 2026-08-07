using GrouperLib.Core;
using GrouperLib.Database;

namespace GrouperLib.Test;

/// <summary>
/// Covers <see cref="GrouperDocumentEntry"/>, the row shape returned by every DocumentDb query:
/// a document plus its revision metadata and tags.
/// </summary>
public class GrouperDocumentEntryTest
{
    private static GrouperDocumentEntry MakeEntry(int revision = 1, string[]? tags = null) =>
        new(TestHelpers.MakeDocument(), revision, new DateTime(2026, 8, 7, 10, 0, 0), true, false, tags);

    [Fact]
    public void TestConstructionExposesSuppliedValues()
    {
        DateTime created = new(2026, 8, 7, 10, 0, 0);
        GrouperDocumentEntry entry = new(TestHelpers.MakeDocument(), 3, created, isPublished: true, isDeleted: false, ["HR"]);

        Assert.Equal(3, entry.Revision);
        Assert.Equal(created, entry.RevisionCreated);
        Assert.True(entry.IsPublished);
        Assert.False(entry.IsDeleted);
        Assert.Equal(["HR"], entry.Tags);
    }

    /// <summary>Revisions are one-based in the database, so anything lower is a bug upstream.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void TestRevisionBelowOneThrows(int revision)
    {
        Assert.Throws<ArgumentException>(() => MakeEntry(revision));
    }

    [Fact]
    public void TestRevisionOneIsAccepted()
    {
        Assert.Equal(1, MakeEntry(1).Revision);
    }

    [Fact]
    public void TestNullDocumentThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GrouperDocumentEntry(null!, 1, DateTime.Now, true, false, null));
    }

    [Fact]
    public void TestNullTagsBecomeAnEmptyCollection()
    {
        GrouperDocumentEntry entry = MakeEntry(tags: null);

        Assert.NotNull(entry.Tags);
        Assert.Empty(entry.Tags);
    }

    [Fact]
    public void TestEmptyTagsStayEmpty()
    {
        Assert.Empty(MakeEntry(tags: []).Tags);
    }

    [Fact]
    public void TestTagsPreserveOrderAndDuplicates()
    {
        GrouperDocumentEntry entry = MakeEntry(tags: ["B", "A", "B"]);

        Assert.Equal(["B", "A", "B"], entry.Tags);
    }

    /// <summary>Tags are exposed read-only so a caller cannot mutate the entry.</summary>
    [Fact]
    public void TestTagsAreReadOnly()
    {
        GrouperDocumentEntry entry = MakeEntry(tags: ["HR"]);

        Assert.True(entry.Tags.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => entry.Tags.Add("Injected"));
    }

    /// <summary>
    /// The tag array is copied on construction, so mutating the caller's array afterwards does not
    /// reach into the entry.
    /// </summary>
    [Fact]
    public void TestTagArrayIsCopiedNotAliased()
    {
        string[] tags = ["HR"];
        GrouperDocumentEntry entry = MakeEntry(tags: tags);

        tags[0] = "Mutated";

        Assert.Equal(["HR"], entry.Tags);
    }

    /// <summary>GroupId and GroupName are projections of the document, not stored separately.</summary>
    [Fact]
    public void TestGroupIdentityIsDelegatedToTheDocument()
    {
        GrouperDocument document = TestHelpers.MakeDocument();
        GrouperDocumentEntry entry = new(document, 1, DateTime.Now, true, false, null);

        Assert.Equal(document.GroupId, entry.GroupId);
        Assert.Equal(document.GroupName, entry.GroupName);
        Assert.Same(document, entry.Document);
    }

    [Fact]
    public void TestDeletedAndUnpublishedFlagsRoundTrip()
    {
        GrouperDocumentEntry entry =
            new(TestHelpers.MakeDocument(), 1, DateTime.Now, isPublished: false, isDeleted: true, null);

        Assert.False(entry.IsPublished);
        Assert.True(entry.IsDeleted);
    }
}
