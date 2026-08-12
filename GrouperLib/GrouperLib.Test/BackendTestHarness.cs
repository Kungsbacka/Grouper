using GrouperLib.Backend;
using GrouperLib.Core;
using GrouperLib.Database;
using Moq;
using System.Runtime.Versioning;

namespace GrouperLib.Test;

/// <summary>
/// Builds a <see cref="Grouper"/> wired to fake stores and member sources so the
/// membership pipeline can be exercised without a directory, a database or a network.
///
/// Each member object is given a single rule whose value is unique within the document.
/// The fake member source returns whatever was registered for that value, which lets one
/// source serve several member objects in the same document with different results. Which
/// rule carries the value depends on the source, because the documents built here have to
/// pass validation like any other -- see <see cref="LabelRule"/>.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class BackendTestHarness
{
    private readonly List<GrouperDocumentMember> _documentMembers = [];
    private readonly Dictionary<string, GroupMember[]> _resultsByLabel = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<GroupMemberSource> _sourcesToRegister = [];
    private GroupMember[] _currentMembers = [];
    private GroupMember[] _owners = [];
    private int _labelCounter;

    public static readonly Guid DocumentId = Guid.Parse("1d9a1a2f-2a1e-4a3b-9f4c-5d6e7f8a9b01");
    public static readonly Guid GroupId = Guid.Parse("2e8b2b3a-3b2f-4b4c-8a5d-6e7f8a9b0c12");

    /// <summary>Store the document targets. Also decides the document's member type.</summary>
    public GroupStore Store { get; set; } = GroupStore.AzureAd;

    public GroupOwnerAction OwnerAction { get; set; } = GroupOwnerAction.KeepExisting;

    public double ChangeRatioLowerLimit { get; set; }

    /// <summary>Register the owner source. Off by default; only AzureAd has one in production.</summary>
    public bool RegisterOwnerSource { get; set; }

    public bool RegisterLogger { get; set; } = true;

    public bool RegisterGroupStore { get; set; } = true;

    public bool RegisterMemberSource { get; set; } = true;

    public Mock<IGroupStore> GroupStoreMock { get; } = new();
    public Mock<IGroupOwnerSource> OwnerSourceMock { get; } = new();
    public Mock<ILogger> LoggerMock { get; } = new();

    public static GroupMember AzureMember(string name) =>
        new(DeterministicGuid(name), name, GroupMemberType.AzureAd);

    public static GroupMember OnPremMember(string name) =>
        new(DeterministicGuid(name), name, GroupMemberType.OnPremAd);

    /// <summary>Stable GUID per name so the same logical member compares equal across collections.</summary>
    private static Guid DeterministicGuid(string name)
    {
        byte[] bytes = new byte[16];
        for (int i = 0; i < name.Length && i < 16; i++)
        {
            bytes[i] = (byte)name[i];
        }
        bytes[15] = (byte)name.Length;
        return new Guid(bytes);
    }

    public BackendTestHarness WithCurrentMembers(params GroupMember[] members)
    {
        _currentMembers = members;
        return this;
    }

    public BackendTestHarness WithOwners(params GroupMember[] owners)
    {
        _owners = owners;
        RegisterOwnerSource = true;
        return this;
    }

    /// <summary>Adds a member object to the document and the members its source will return.</summary>
    public BackendTestHarness WithRule(GroupMemberSource source, GroupMemberAction action, params GroupMember[] members)
    {
        GrouperDocumentRule rule = LabelRule(source, ++_labelCounter);
        _resultsByLabel[rule.Value] = members;
        _sourcesToRegister.Add(source);
        _documentMembers.Add(new GrouperDocumentMember(source, action, [rule]));
        return this;
    }

    /// <summary>Adds a member object whose source is deliberately left unregistered.</summary>
    public BackendTestHarness WithUnregisteredRule(GroupMemberSource source, GroupMemberAction action)
    {
        // Nothing is registered under this rule's value, so the source would return no members
        // even if it were reached.
        _documentMembers.Add(new GrouperDocumentMember(source, action, [LabelRule(source, ++_labelCounter)]));
        return this;
    }

    /// <summary>
    /// The one rule that labels a member object, chosen to suit the source. GetMemberDiffAsync
    /// refuses a document that does not validate, so the label has to ride on a rule the source
    /// permits, carrying a value its format allows, rather than on an invented rule name.
    /// </summary>
    private static GrouperDocumentRule LabelRule(GroupMemberSource source, int counter) => source switch
    {
        GroupMemberSource.AzureAdGroup or GroupMemberSource.OnPremAdGroup or GroupMemberSource.ExoGroup =>
            new GrouperDocumentRule("Group", LabelGuid(counter)),
        GroupMemberSource.Elevregister => new GrouperDocumentRule("Enhet", LabelGuid(counter)),
        GroupMemberSource.Personalsystem => new GrouperDocumentRule("Befattning", $"L{counter}"),
        GroupMemberSource.OnPremAdQuery => new GrouperDocumentRule("LdapFilter", $"(cn=L{counter})"),
        GroupMemberSource.CustomView => new GrouperDocumentRule("View", $"L{counter}"),
        GroupMemberSource.Static => new GrouperDocumentRule("Upn", $"l{counter}@example.com"),
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, "No label rule is declared for this member source."),
    };

    /// <summary>
    /// A distinct GUID per label, never <see cref="GroupId"/>, which self-reference would reject.
    /// </summary>
    private static string LabelGuid(int counter) => $"00000000-0000-4000-8000-{counter:D12}";

    public GrouperDocument BuildDocument() =>
        // Internal constructor, reachable via InternalsVisibleTo, so the document is built without
        // going through Create. It still has to satisfy validation, because GetMemberDiffAsync
        // refuses a document that does not.
        new(DocumentId, GroupId, "Test Group", Store, _documentMembers, OwnerAction, 0);

    public Grouper BuildGrouper()
    {
        GroupStoreMock.Setup(s => s.GetSupportedGroupStores()).Returns([Store]);
        GroupStoreMock
            .Setup(s => s.GetGroupMembersAsync(It.IsAny<GroupMemberCollection>(), It.IsAny<Guid>()))
            .Returns((GroupMemberCollection collection, Guid _) =>
            {
                foreach (GroupMember member in _currentMembers)
                {
                    collection.Add(member);
                }
                return Task.CompletedTask;
            });

        Mock<IMemberSource> memberSourceMock = new();
        memberSourceMock.Setup(s => s.GetSupportedGrouperMemberSources()).Returns(_sourcesToRegister.ToArray());
        memberSourceMock
            .Setup(s => s.GetMembersFromSourceAsync(
                It.IsAny<GroupMemberCollection>(), It.IsAny<GrouperDocumentMember>(), It.IsAny<GroupMemberType>()))
            .Returns((GroupMemberCollection collection, GrouperDocumentMember documentMember, GroupMemberType _) =>
            {
                // Every member object built here carries exactly one rule, and its value is the label.
                string label = documentMember.Rules.First().Value;
                foreach (GroupMember member in _resultsByLabel.GetValueOrDefault(label, []))
                {
                    collection.Add(member);
                }
                return Task.CompletedTask;
            });

        OwnerSourceMock.Setup(s => s.GetSupportedGroupStores()).Returns([Store]);
        OwnerSourceMock
            .Setup(s => s.GetGroupOwnersAsync(It.IsAny<GroupMemberCollection>(), It.IsAny<Guid>()))
            .Returns((GroupMemberCollection collection, Guid _) =>
            {
                foreach (GroupMember owner in _owners)
                {
                    collection.Add(owner);
                }
                return Task.CompletedTask;
            });

        Grouper grouper = new(ChangeRatioLowerLimit);
        if (RegisterGroupStore)
        {
            grouper.AddGroupStore(GroupStoreMock.Object);
        }
        if (RegisterMemberSource && _sourcesToRegister.Count > 0)
        {
            grouper.AddMemberSource(memberSourceMock.Object);
        }
        if (RegisterOwnerSource)
        {
            grouper.AddGroupOwnerSource(OwnerSourceMock.Object);
        }
        if (RegisterLogger)
        {
            grouper.AddLogger(LoggerMock.Object);
        }
        return grouper;
    }

    /// <summary>Runs the diff for the configured document.</summary>
    public Task<GroupMemberDiff> DiffAsync(bool includeUnchanged = false) =>
        BuildGrouper().GetMemberDiffAsync(BuildDocument(), includeUnchanged);

    public static string[] Names(IEnumerable<GroupMember> members) =>
        members.Select(m => m.DisplayName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
}
