using GrouperLib.Core;
using System.Runtime.Versioning;

namespace CompileTarget;

public static class CompileTarget
{
    [SupportedOSPlatform("windows")]
    public static async Task<int> Main()
    {
        // CompileTarget is used to compile enough of the GrouerLib to include all the necessary classes
        // that PowerShell module PSGrouper needs. The code below is there to make sure that code that is
        // needed does not get trimmed out.
        GrouperDocument doc1 = GrouperDocument.Create(
            id: Guid.NewGuid(),
            groupId: Guid.NewGuid(),
            groupName: "Dummy",
            store: GroupStore.OnPremAd,
            owner: GroupOwnerAction.NoAction,
            interval: 0,
            members: [
                new GrouperDocumentMember(GroupMemberSource.OnPremAdGroup, action: GroupMemberAction.Include, rules:
                    [
                        new GrouperDocumentRule("Group", Guid.NewGuid().ToString())
                    ])

            ]
        );

        GrouperDocument doc2 = GrouperDocument.FromJson(doc1.ToJson());

        Console.WriteLine(doc1.ToJson(true));
        Console.WriteLine(doc2.ToJson(true));

        return await Task.FromResult(0);
    }
}