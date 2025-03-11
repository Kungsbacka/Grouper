using GrouperLib.Core;

namespace GrouperLib.Test;

public class AzureAdValidatorTest
{
    private static readonly Guid groupId = Guid.Parse("baefe5f4-d404-491d-89d0-fb192afa3c1d");
    private static readonly Guid targetGroupId = Guid.Parse("2422bdbd-8a4d-4996-99db-d9ed29294779");

    [Fact]
    public void TestValidateWithLegalDocument()
    {
        GrouperDocument document = TestHelpers.MakeDocument(new
        {
            GroupId = groupId,
            Store = GroupStore.AzureAd,
            Members = new[]
            {
                new
                {
                    Source = GroupMemberSource.AzureAdGroup,
                    Rules = new []
                    {
                        new { Name = "Group", Value = targetGroupId }
                    }
                }
            }
        });
        List<ValidationError> errors = [];
        new AzureAdValidator().Validate(document, document.Members.First(), errors);
        Assert.Empty(errors);
    }

    [Fact]
    public void TestValidateWithBrokenDocument()
    {
        GrouperDocument document = TestHelpers.MakeDocument(new
        {
            GroupId = groupId,
            Store = GroupStore.AzureAd,
            Members = new[]
            {
                new
                {
                    Source = GroupMemberSource.AzureAdGroup,
                    Rules = new []
                    {
                        new { Name = "Group", Value = groupId }
                    }
                }
            }
        });
        List<ValidationError> errors = [];
        new AzureAdValidator().Validate(document, document.Members.First(), errors);
        Assert.True(errors.Count > 0);
    }
}