using GrouperLib.Core;

namespace GrouperLib.Test;

public class GroupMemberTest
{
    private static readonly string validGuidString = "16551e3f-6ce9-4630-b873-e50321bf97a7";
    private static readonly string anotherValidGuidString = "d808de38-0fa4-4e81-b054-3ed738370266";
    private static readonly Guid validGuid = Guid.Parse(validGuidString);
    private static readonly Guid anotherValidGuid = Guid.Parse(anotherValidGuidString);
    private static readonly string invalidGuid = "not-a-valid-guid";

    [Fact]
    public void TestConstruction1()
    {
        GroupMember member = new(validGuid, "Name", GroupMemberType.OnPremAd);
        Assert.Equal(validGuid, member.Id);
        Assert.Equal("Name", member.DisplayName);
        Assert.Equal(GroupMemberType.OnPremAd, member.MemberType);
    }

    [Fact]
    public void TestConstruction2()
    {
        GroupMember member = new(validGuidString, "Name", GroupMemberType.OnPremAd);
        Assert.Equal(validGuid, member.Id);
        Assert.Equal("Name", member.DisplayName);
        Assert.Equal(GroupMemberType.OnPremAd, member.MemberType);
    }

    [Fact]
    public void TestConstructionInvalidGuid()
    {
        Assert.Throws<ArgumentException>(() => { new GroupMember(invalidGuid, "Name", GroupMemberType.OnPremAd); });
    }

    [Fact]
    public void TestEquals()
    {
        GroupMember member1 = new(validGuid, "Name", GroupMemberType.OnPremAd);
        GroupMember member2 = new(validGuid, "Different name", GroupMemberType.OnPremAd);
        Assert.True(member1.Equals(member2));
    }

    [Fact]
    public void TestNotEquals1()
    {
        GroupMember member1 = new(validGuid, "Name", GroupMemberType.OnPremAd);
        GroupMember member2 = new(anotherValidGuid, "Name", GroupMemberType.OnPremAd);
        Assert.False(member1.Equals(member2));
    }

    [Fact]
    public void TestNotEquals2()
    {
        GroupMember member1 = new(validGuid, "Name", GroupMemberType.OnPremAd);
        GroupMember member2 = new(validGuid, "Name", GroupMemberType.AzureAd);
        Assert.False(member1.Equals(member2));
    }

    [Fact]
    public void TestGetHashCode()
    {
        GroupMember member = new(validGuid, "Name", GroupMemberType.OnPremAd);
        Assert.Equal(validGuid.GetHashCode(), member.GetHashCode());
    }

    [Fact]
    public void TestToString()
    {
        GroupMember member = new(validGuid, "Name", GroupMemberType.OnPremAd);
        Assert.Equal("Name", member.ToString());
    }
}