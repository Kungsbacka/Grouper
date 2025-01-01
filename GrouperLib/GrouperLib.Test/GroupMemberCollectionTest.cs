using GrouperLib.Core;

namespace GrouperLib.Test;

public class GroupMemberCollectionTest
{
    private static readonly GroupMember azureMember1 = 
        new("9e9abe09-5e43-4f61-a09a-f1aa370be6c1", "Azure Member 1", GroupMemberType.AzureAd);
    private static readonly GroupMember azureMember2 = 
        new("4a13ddd8-f5d4-4671-b734-949cc956a06f", "Azure Member 2", GroupMemberType.AzureAd);
    private static readonly GroupMember azureMember3 =
        new("b9ba0fda-2dad-4b85-b95e-a6c614aa3ab9", "Azure Member 3", GroupMemberType.AzureAd);
    private static readonly GroupMember onpremMember1 = 
        new("20c58480-458a-4dfa-abc3-b509ac1846a3", "On-prem Member 1", GroupMemberType.OnPremAd);
    private static readonly GroupMember onpremMember2 = 
        new("0fb7bb07-45aa-4eb9-8190-7cc96a1fa791", "On-prem Member 2", GroupMemberType.OnPremAd);

    [Fact]
    public void TestCountZero()
    {
        GroupMemberCollection collection = new();
        Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void TestCountOne()
    {
        GroupMemberCollection collection = new();
        collection.Add(azureMember1);
        Assert.Equal(1, collection.Count);
    }

    [Fact]
    public void TestAsEnumerable()
    {
        GroupMemberCollection collection = new();
        Assert.NotNull(collection.AsEnumerable());
    }

    [Fact]
    public void TestAddMember()
    {
        GroupMemberCollection collection = new();
        collection.Add(azureMember1);
        GroupMember member = collection.AsEnumerable().First();
        Assert.Equal(azureMember1.Id, member.Id);
    }

    [Fact]
    public void TestAddCollection()
    {
        GroupMemberCollection collection1 = new();
        GroupMemberCollection collection2 = new();
        collection1.Add(azureMember1);
        collection2.Add(azureMember2);
        collection1.Add(collection2);
        Assert.Equal(2, collection1.Count);
    }

    [Fact]
    public void TestExceptWith()
    {
        GroupMemberCollection collection1 = new();
        GroupMemberCollection collection2 = new();
        collection1.Add(azureMember1);
        collection1.Add(azureMember2);
        collection2.Add(azureMember2);
        collection1.ExceptWith(collection2);
        Assert.Equal(1, collection1.Count);
        Exception exception = Record.Exception(() => collection1.AsEnumerable().First(m => m.Id == azureMember1.Id));
        Assert.Null(exception);
    }

    [Fact]
    public void TestIntersectWith()
    {
        GroupMemberCollection collection1 = new();
        GroupMemberCollection collection2 = new();
        collection1.Add(azureMember1);
        collection1.Add(azureMember2);
        collection2.Add(azureMember2);
        collection2.Add(azureMember3);
        collection1.IntersectWith(collection2);
        Assert.Equal(1, collection1.Count);
        Exception exception = Record.Exception(() => collection1.AsEnumerable().First(m => m.Id == azureMember2.Id));
        Assert.Null(exception);
    }
}