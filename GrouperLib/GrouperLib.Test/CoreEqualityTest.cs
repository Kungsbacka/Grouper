using GrouperLib.Core;

namespace GrouperLib.Test;

/// <summary>
/// Covers the "not my type" branch of each <c>Equals</c> override in Core, plus the identity rules
/// those overrides encode. Membership arithmetic runs on hash sets, so equality is what makes
/// <c>ExceptWith</c> and <c>IntersectWith</c> mean the right thing.
/// </summary>
public class CoreEqualityTest
{
    [Fact]
    public void TestGroupMemberIsNotEqualToAnotherType()
    {
        GroupMember member = new(Guid.NewGuid(), "M", GroupMemberType.AzureAd);

        Assert.False(member.Equals("not a member"));
        Assert.False(member.Equals(null));
    }

    /// <summary>Identity is (id, memberType) -- the same GUID in a different directory is not the
    /// same member, which is what stops on-prem and cloud members being conflated.</summary>
    [Fact]
    public void TestGroupMemberIdentityIncludesMemberType()
    {
        Guid id = Guid.NewGuid();
        GroupMember onPrem = new(id, "M", GroupMemberType.OnPremAd);
        GroupMember azure = new(id, "M", GroupMemberType.AzureAd);

        Assert.NotEqual(onPrem, azure);
    }

    /// <summary>Display name is not part of identity: a renamed member is still the same member.</summary>
    [Fact]
    public void TestGroupMemberIdentityIgnoresDisplayName()
    {
        Guid id = Guid.NewGuid();

        Assert.Equal(
            new GroupMember(id, "Old Name", GroupMemberType.AzureAd),
            new GroupMember(id, "New Name", GroupMemberType.AzureAd));
    }

    [Fact]
    public void TestDocumentIsNotEqualToAnotherType()
    {
        GrouperDocument document = TestHelpers.MakeDocument();

        Assert.False(document.Equals("not a document"));
        Assert.False(document.Equals(null));
    }

    /// <summary>Document identity is the id alone, so content differences do not affect it.</summary>
    [Fact]
    public void TestDocumentIdentityIsTheIdAlone()
    {
        GrouperDocument document = TestHelpers.MakeDocument();
        GrouperDocument renamed = document.CloneWithNewGroupName("Different Name");

        Assert.Equal(document, renamed);
        Assert.Equal(document.GetHashCode(), renamed.GetHashCode());
    }

    [Fact]
    public void TestDocumentMemberIsNotEqualToAnotherType()
    {
        GrouperDocumentMember member = TestHelpers.MakeMember();

        Assert.False(member.Equals("not a member object"));
        Assert.False(member.Equals(null));
    }

    [Fact]
    public void TestDocumentMemberWithDifferentRuleCountIsNotEqual()
    {
        GrouperDocumentMember one = new(GroupMemberSource.Static, GroupMemberAction.Include,
            [new GrouperDocumentRule("Upn", "a@example.com")]);
        GrouperDocumentMember two = new(GroupMemberSource.Static, GroupMemberAction.Include,
            [new GrouperDocumentRule("Upn", "a@example.com"), new GrouperDocumentRule("Upn", "b@example.com")]);

        Assert.NotEqual(one, two);
    }

    /// <summary>Rule order within a member object does not affect equality.</summary>
    [Fact]
    public void TestDocumentMemberRuleOrderDoesNotMatter()
    {
        GrouperDocumentMember one = new(GroupMemberSource.Static, GroupMemberAction.Include,
            [new GrouperDocumentRule("Upn", "a@example.com"), new GrouperDocumentRule("Upn", "b@example.com")]);
        GrouperDocumentMember two = new(GroupMemberSource.Static, GroupMemberAction.Include,
            [new GrouperDocumentRule("Upn", "b@example.com"), new GrouperDocumentRule("Upn", "a@example.com")]);

        Assert.Equal(one, two);
        Assert.Equal(one.GetHashCode(), two.GetHashCode());
    }

    [Fact]
    public void TestDocumentRuleIsNotEqualToAnotherType()
    {
        GrouperDocumentRule rule = new("Upn", "a@example.com");

        Assert.False(rule.Equals("not a rule"));
        Assert.False(rule.Equals(null));
    }

    /// <summary>
    /// Rule names compare case-sensitively -- the document is the contract and the name has to be
    /// spelled the way the validator declares it. Values stay case-insensitive.
    /// </summary>
    [Fact]
    public void TestDocumentRuleComparisonIsNameSensitiveAndValueInsensitive()
    {
        Assert.NotEqual(new GrouperDocumentRule("Upn", "a@example.com"), new GrouperDocumentRule("UPN", "a@example.com"));
        Assert.Equal(new GrouperDocumentRule("Upn", "A@Example.com"), new GrouperDocumentRule("Upn", "a@example.com"));
        Assert.Equal(
            new GrouperDocumentRule("Upn", "A@Example.com").GetHashCode(),
            new GrouperDocumentRule("Upn", "a@example.com").GetHashCode());
    }

    /// <summary>Null name or value is coerced to empty rather than throwing.</summary>
    [Fact]
    public void TestDocumentRuleCoercesNullsToEmpty()
    {
        GrouperDocumentRule rule = new(null!, null!);

        Assert.Equal("", rule.Name);
        Assert.Equal("", rule.Value);
    }

    /// <summary>The non-generic enumerator is what foreach over IEnumerable uses.</summary>
    [Fact]
    public void TestGroupMemberCollectionSupportsNonGenericEnumeration()
    {
        GroupMemberCollection collection = [new GroupMember(Guid.NewGuid(), "M", GroupMemberType.AzureAd)];

        int count = 0;
        foreach (object _ in (System.Collections.IEnumerable)collection)
        {
            count++;
        }

        Assert.Equal(1, count);
    }
}
