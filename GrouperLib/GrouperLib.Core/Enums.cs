namespace GrouperLib.Core;

/// <summary>
/// Represents the actions that can be taken on a group owner.
/// </summary>
public enum GroupOwnerAction
{
    /// <summary>
    /// No action will be taken. If an owner is a member of the group, it will remain a member.
    /// </summary>
    NoAction,

    /// <summary>
    /// Add all owners as members to the group, even if the owner does not exist in the member source.
    /// </summary>
    AddAll,

    /// <summary>
    /// Keep owners that are already members of the group, even it the owner does not exist in the member source.
    /// </summary>
    KeepExisting,

    /// <summary>
    /// Only add owners as members that also exists in the member source.
    /// </summary>
    MatchSource
}

/// <summary>
/// Represents the actions that can be taken on a group member.
/// </summary>
public enum GroupMemberAction
{
    /// <summary>
    /// Include the members in the group.
    /// </summary>
    Include,

    /// <summary>
    /// Exclude the members from the group.
    /// </summary>
    Exclude
}

/// <summary>
/// Represents the different group stores that Grouper can update.
/// </summary>
public enum GroupStore
{
    /// <summary>
    /// On-premises Active Directory.
    /// </summary>
    OnPremAd,

    /// <summary>
    /// Entra ID (formerly Azure AD)
    /// </summary>
    AzureAd,

    /// <summary>
    /// Exchange Online (distribution groups).
    /// </summary>
    Exo,

    /// <summary>
    /// Open ePlatform.
    /// </summary>
    OpenE
}

/// <summary>
/// Represents the different sources of group members.
/// </summary>
public enum GroupMemberSource
{
    /// <summary>
    /// Personalsystem. Currently PersonecP.
    /// </summary>
    Personalsystem,

    /// <summary>
    /// Elevregister. Currently IST Administration.
    /// </summary>
    Elevregister,

    /// <summary>
    /// On-premises Active Directory group.
    /// </summary>
    OnPremAdGroup,

    /// <summary>
    /// On-premises Active Directory LDAP query.
    /// </summary>
    OnPremAdQuery,

    /// <summary>
    /// Entra ID group.
    /// </summary>
    AzureAdGroup,

    /// <summary>
    /// Exchange Online distribution group.
    /// </summary>
    ExoGroup,

    /// <summary>
    /// Custom database view in the metadirectory.
    /// </summary>
    CustomView,

    /// <summary>
    /// Static member. Members are identified by their UPN.
    /// </summary>
    Static
}

/// <summary>
/// Represents the types of group members. Different group stores requires different types of members.
/// </summary>
public enum GroupMemberType
{
    /// <summary>
    /// On-premises Active Directory.
    /// </summary>
    OnPremAd,

    /// <summary>
    /// Entra ID (formerly Azure AD).
    /// </summary>
    AzureAd
}

/// <summary>
/// Represents the operations that can be performed on group members.
/// </summary>
public enum GroupMemberOperation
{
    /// <summary>
    /// Add the member to the group.
    /// </summary>
    Add,

    /// <summary>
    /// Remove the member from the group.
    /// </summary>
    Remove,

    /// <summary>
    /// Do nothing.
    /// </summary>
    None
}

/// <summary>
/// Represents the log levels.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Informational messages.
    /// </summary>
    Information = 1,

    /// <summary>
    /// Warning messages.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error messages.
    /// </summary>
    Error = 3
}
