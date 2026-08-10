using GrouperLib.Core;
using GrouperLib.Language;

namespace GrouperLib.Test;

/// <summary>
/// The self-reference check: a document may not draw members from its own target group, which would
/// make the group depend on itself.
///
/// One validator now serves all three group-member sources. It was previously two byte-identical
/// classes covering <see cref="GroupMemberSource.AzureAdGroup"/> and
/// <see cref="GroupMemberSource.OnPremAdGroup"/>, with <see cref="GroupMemberSource.ExoGroup"/>
/// left uncovered -- an oversight, closed after confirming no stored document relied on it.
///
/// These call the validator directly. <c>DocumentValidatorTest.TestGroupCannotBeItsOwnMemberSource</c>
/// covers the same three sources through the engine, which is what proves each spec actually
/// attaches it.
/// </summary>
public class SelfReferenceValidatorTest
{
    private static readonly Guid groupId = Guid.Parse("baefe5f4-d404-491d-89d0-fb192afa3c1d");
    private static readonly Guid otherGroupId = Guid.Parse("2422bdbd-8a4d-4996-99db-d9ed29294779");

    private static List<ValidationError> Validate(GroupMemberSource source, GroupStore store, Guid ruleValue)
    {
        GrouperDocument document = TestHelpers.MakeDocument(new
        {
            GroupId = groupId,
            Store = store,
            Members = new[]
            {
                new
                {
                    Source = source,
                    Rules = new[]
                    {
                        new { Name = "Group", Value = ruleValue }
                    }
                }
            }
        });
        List<ValidationError> errors = [];
        new SelfReferenceValidator().Validate(document, document.Members.First(), errors);
        return errors;
    }

    [Theory]
    [InlineData(GroupMemberSource.OnPremAdGroup, GroupStore.OnPremAd)]
    [InlineData(GroupMemberSource.AzureAdGroup, GroupStore.AzureAd)]
    [InlineData(GroupMemberSource.ExoGroup, GroupStore.Exo)]
    public void TestAnotherGroupAsMemberSourceIsAccepted(GroupMemberSource source, GroupStore store)
    {
        Assert.Empty(Validate(source, store, otherGroupId));
    }

    [Theory]
    [InlineData(GroupMemberSource.OnPremAdGroup, GroupStore.OnPremAd)]
    [InlineData(GroupMemberSource.AzureAdGroup, GroupStore.AzureAd)]
    [InlineData(GroupMemberSource.ExoGroup, GroupStore.Exo)]
    public void TestOwnGroupAsMemberSourceIsRejected(GroupMemberSource source, GroupStore store)
    {
        Assert.Contains(ResourceString.ValidationErrorSourceGroupSameAsTarget,
            Validate(source, store, groupId).Select(e => e.ErrorId));
    }
}
