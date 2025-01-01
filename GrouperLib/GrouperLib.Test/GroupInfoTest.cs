using GrouperLib.Core;
using System.Text.Json;

namespace GrouperLib.Test;

public class GroupInfoTest
{
    private static readonly string validGuidString = "16551e3f-6ce9-4630-b873-e50321bf97a7";
    private static readonly Guid validGuid = Guid.Parse(validGuidString);
    private static readonly string invalidGuid = "not-a-valid-guid";

    [Fact]
    public void TestConstruction1()
    {
        GroupInfo info = new(validGuid, "Name", GroupStore.OnPremAd);
        Assert.Equal(validGuid, info.Id);
        Assert.Equal("Name", info.DisplayName);
        Assert.Equal(GroupStore.OnPremAd, info.Store);
    }

    [Fact]
    public void TestConstruction2()
    {
        GroupInfo info = new(validGuidString, "Name", GroupStore.OnPremAd);
        Assert.Equal(validGuid, info.Id);
        Assert.Equal("Name", info.DisplayName);
        Assert.Equal(GroupStore.OnPremAd, info.Store);
    }

    [Fact]
    public void TestConstructionWithInvalidGuid()
    {
        Assert.Throws<ArgumentException>(() => { new GroupInfo(invalidGuid, "Name", GroupStore.OnPremAd); });
    }

    [Fact]
    public void TestSerializedNames()
    {
        GroupInfo groupInfo = new(Guid.Empty, "Name", GroupStore.AzureAd);
        string json = JsonSerializer.Serialize(groupInfo);
        Dictionary<string,object>? obj = JsonSerializer.Deserialize<Dictionary<string,object>>(json);
        Assert.True(obj?.ContainsKey("id"));
        Assert.True(obj?.ContainsKey("displayName"));
        Assert.True(obj?.ContainsKey("store"));
    }
}