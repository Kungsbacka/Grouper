using GrouperLib.Core;

namespace GrouperLib.Test;

/// <summary>
/// Covers the <c>ToString</c> overrides in Core. These are not decoration: the log-item ones are
/// what GrouperService prints when run interactively, and <c>ValidationError.ToString</c> is how a
/// validation failure renders when written straight to a console or a log.
/// </summary>
public class CoreToStringTest
{
    private static readonly GroupMember member =
        new("9e9abe09-5e43-4f61-a09a-f1aa370be6c1", "member@example.com", GroupMemberType.AzureAd);

    // ---- log items ----------------------------------------------------------------------

    [Fact]
    public void TestAuditLogItemToStringIncludesActorAndAction()
    {
        AuditLogItem item = new(new DateTime(2026, 8, 7, 14, 30, 0),
            Guid.Parse("aa11bb22-cc33-dd44-ee55-ff6677889900"), "DOMAIN\\user", "Published document", null);

        string text = item.ToString();

        Assert.Contains("2026-08-07 14:30:00", text);
        Assert.Contains("DOMAIN\\user", text);
        Assert.Contains("Published document", text);
    }

    [Fact]
    public void TestOperationalLogItemToStringReadsAsASentenceForAdd()
    {
        OperationalLogItem added = new(TestHelpers.MakeDocument(), GroupMemberOperation.Add, member);

        Assert.Contains("Added member@example.com to Test Group", added.ToString());
    }

    /// <summary>
    /// The past tense used to be built as <c>Operation.ToString() + "ed "</c>, which gave "Added"
    /// for Add but "Removeed" for Remove, because the enum name already ends in "e". Both words are
    /// now written out per operation, so this asserts the corrected spelling.
    /// </summary>
    [Fact]
    public void TestOperationalLogItemToStringReadsAsASentenceForRemove()
    {
        OperationalLogItem removed = new(TestHelpers.MakeDocument(), GroupMemberOperation.Remove, member);

        Assert.Contains("Removed member@example.com from Test Group", removed.ToString());
    }

    /// <summary>
    /// The None operation cannot be persisted, but ToString still has to render it.
    /// </summary>
    [Fact]
    public void TestOperationalLogItemToStringHandlesTheNoneOperation()
    {
        OperationalLogItem item = new(TestHelpers.MakeDocument(), GroupMemberOperation.None, member);

        Assert.Contains("No change for member@example.com in Test Group", item.ToString());
    }

    /// <summary>
    /// ToString is called while logging, including from the code that reports why something else
    /// failed, so an operation it does not recognise must not throw. The value is rendered instead:
    /// its name when the enum has one, its number when it does not.
    /// </summary>
    [Fact]
    public void TestOperationalLogItemToStringRendersAnUnrecognisedOperation()
    {
        OperationalLogItem item = new(TestHelpers.MakeDocument(), (GroupMemberOperation)99, member);

        string text = item.ToString();

        Assert.Contains("Unrecognised operation 99", text);
        Assert.Contains("member@example.com", text);
        Assert.Contains("Test Group", text);
    }

    [Fact]
    public void TestEventLogItemToStringIncludesLevelAndGroup()
    {
        EventLogItem item = new(TestHelpers.MakeDocument(), "Something went wrong", LogLevel.Error);

        string text = item.ToString();

        Assert.Contains("Error", text);
        Assert.Contains("Something went wrong", text);
        Assert.Contains("(Group: Test Group)", text);
    }

    /// <summary>A multi-line message is truncated to its first line with an ellipsis.</summary>
    [Fact]
    public void TestEventLogItemToStringTruncatesMultiLineMessages()
    {
        EventLogItem item = new(TestHelpers.MakeDocument(), "First line\nSecond line", LogLevel.Warning);

        string text = item.ToString();

        Assert.Contains("...", text);
        Assert.DoesNotContain("Second line", text);
    }

    [Fact]
    public void TestEventLogItemToStringOmitsGroupWhenAbsent()
    {
        EventLogItem item = new(DateTime.Now, null, null, null, null, "No group here", LogLevel.Information);

        Assert.DoesNotContain("(Group:", item.ToString());
    }

    // ---- documents and rules -------------------------------------------------------------

    [Fact]
    public void TestDocumentToStringIsTheDocumentIdByDefault()
    {
        GrouperDocument document = TestHelpers.MakeDocument();

        Assert.Equal(document.Id.ToString(), document.ToString());
        Assert.Equal(document.Id.ToString(), document.ToString(logFormat: false));
    }

    [Fact]
    public void TestDocumentLogFormatListsTheHeaderFields()
    {
        GrouperDocument document = TestHelpers.MakeDocument();

        string text = document.ToString(logFormat: true);

        Assert.Contains("Group Name: Test Group", text);
        Assert.Contains("Group Store: OnPremAd", text);
        Assert.Contains("Owner Action: KeepExisting", text);
        Assert.Contains($"Group ID: {document.GroupId}", text);
        Assert.Contains($"Document ID: {document.Id}", text);
        Assert.Contains("Member Rules: 1", text);
    }

    [Fact]
    public void TestDocumentLogFormatNumbersEachMemberObject()
    {
        GrouperDocument document = TestHelpers.MakeDocument(new
        {
            Members = new[]
            {
                new { Source = GroupMemberSource.Static, Action = GroupMemberAction.Include },
                new { Source = GroupMemberSource.OnPremAdGroup, Action = GroupMemberAction.Exclude }
            }
        });

        string text = document.ToString(logFormat: true);

        Assert.Contains("Member Rules: 2", text);
        Assert.Contains("(1) Include: Static", text);
        Assert.Contains("(2) Exclude: OnPremAdGroup", text);
    }

    [Fact]
    public void TestDocumentLogFormatIncludesIntervalWhenNonZero()
    {
        GrouperDocument document = TestHelpers.MakeDocument(new { Interval = 30 });

        string text = document.ToString(logFormat: true);

        Assert.Contains("Processing Interval: 30", text);
    }

    [Fact]
    public void TestDocumentLogFormatOmitsIntervalWhenZero()
    {
        string text = TestHelpers.MakeDocument(new { Interval = 0 }).ToString(logFormat: true);

        Assert.DoesNotContain("Processing Interval", text);
    }

    [Fact]
    public void TestRuleToStringShowsNameAndValue()
    {
        Assert.Equal("Upn => member@example.com", new GrouperDocumentRule("Upn", "member@example.com").ToString());
    }

    [Fact]
    public void TestGroupMemberToStringIsTheDisplayName()
    {
        Assert.Equal("member@example.com", member.ToString());
    }

    // ---- validation errors ---------------------------------------------------------------

    [Fact]
    public void TestValidationErrorToStringIsTheMessage()
    {
        ValidationError error = new("GroupName", Language.ResourceString.ValidationErrorGroupNameIsNullOrEmpty);

        Assert.Equal(error.ErrorMessage, error.ToString());
        Assert.False(string.IsNullOrWhiteSpace(error.ToString()));
    }
}
