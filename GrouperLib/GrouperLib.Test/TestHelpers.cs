using GrouperLib.Core;
using System.Reflection;

namespace GrouperLib.Test;

static class TestHelpers
{
    public static readonly Guid DefaultDocumentId = Guid.Parse("3cbc0481-23b0-4860-a58a-7a723ee250c5");
    public static readonly Guid DefaultGroupId = Guid.Parse("baefe5f4-d404-491d-89d0-fb192afa3c1d");
    public static readonly string DefaultGroupName = "Test Group";
    public static readonly GroupStore DefaultGroupStore = GroupStore.OnPremAd;
    public static readonly GroupOwnerAction DefaultOwnerAction = GroupOwnerAction.KeepExisting;
    public static readonly int DefaultProcessingInterval = 0;
    public static readonly GroupMemberSource DefaultGroupMemberSource = GroupMemberSource.Static;
    public static readonly GroupMemberAction DefaultGroupMemberAction = GroupMemberAction.Include;
    public static readonly string DefaultRuleName = "Upn";
    public static readonly string DefaultRuleValue = "member@example.com";

    public static GrouperDocument MakeDocument() => MakeDocument(new { });
        
    public static GrouperDocumentMember MakeMember() => MakeMember(new { });

    public static GrouperDocumentRule MakeRule() => MakeRule(new { });

    public static GrouperDocument MakeDocument(dynamic obj)
    {
        var objType = obj.GetType();
        var members = new List<GrouperDocumentMember>();
        if (objType.GetProperty("Members") != null)
        {
            foreach (dynamic member in obj.Members)
            {
                members.Add(MakeMember(member));
            }
        }
        else
        {
            members.Add(MakeMember(new { }));
        }
        var doc = (GrouperDocument?)Activator.CreateInstance(typeof(GrouperDocument), BindingFlags.Instance | BindingFlags.NonPublic, binder: null, culture: null, args: [
            null != objType.GetProperty("Id")        ? obj.Id        : DefaultDocumentId,
            null != objType.GetProperty("GroupId")   ? obj.GroupId   : DefaultGroupId,
            null != objType.GetProperty("GroupName") ? obj.GroupName : DefaultGroupName,
            null != objType.GetProperty("Store")     ? obj.Store     : DefaultGroupStore,
            members,
            null != objType.GetProperty("Owner")     ? obj.Owner     : DefaultOwnerAction,
            null != objType.GetProperty("Interval")  ? obj.Interval  : DefaultProcessingInterval,
        ]);

        return doc ?? throw new InvalidOperationException();
    }

    public static GrouperDocumentMember MakeMember(dynamic def)
    {
        var defType = def.GetType();
        var rules = new List<GrouperDocumentRule>();
        if (defType.GetProperty("Rules") != null)
        {
            foreach (dynamic rule in def.Rules)
            {
                rules.Add(MakeRule(rule));
            }
        }
        else
        {
            rules.Add(MakeRule(new { }));
        }

        return new GrouperDocumentMember(
            null != defType.GetProperty("Source") ? def.Source : DefaultGroupMemberSource,
            null != defType.GetProperty("Action") ? def.Action : DefaultGroupMemberAction,
            rules
        );
    }

    public static GrouperDocumentRule MakeRule(dynamic def)
    {
        var defType = def.GetType();
        return new GrouperDocumentRule(
            null != defType.GetProperty("Name")  ? def.Name              : DefaultRuleName,
            null != defType.GetProperty("Value") ? def.Value?.ToString() : DefaultRuleValue
        );
    }
}