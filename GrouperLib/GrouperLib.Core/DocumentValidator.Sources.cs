using System.Text.RegularExpressions;

namespace GrouperLib.Core;

/// <summary>
/// The declarative half of <see cref="DocumentValidator"/>: what each group store and member source
/// permits. The engine lives in DocumentValidator.cs and is shared by every source -- nothing here
/// is logic, and nothing there is source-specific.
/// </summary>
internal static partial class DocumentValidator
{
    private static readonly Dictionary<GroupStore, ResourceLocation> storeLocations = new()
    {
        { GroupStore.OnPremAd, ResourceLocation.OnPrem },
        { GroupStore.AzureAd, ResourceLocation.Azure },
        { GroupStore.Exo, ResourceLocation.Azure },
        { GroupStore.OpenE, ResourceLocation.OnPrem }
    };

    private static readonly Dictionary<GroupMemberSource, MemberSourceSpec> memberSources = new()
    {
        // At least one of Organisation or Befattning selects the population; IncludeManager only
        // means anything relative to an Organisation.
        [GroupMemberSource.Personalsystem] = MemberSourceSpec.Independent()
            .AtLeastOneOf("Organisation", "Befattning")
            .Requires("IncludeManager", "Organisation")
            .Repeatable("Befattning")
            .Matches("Organisation", PersonecIdRegex())
            .Matches("IncludeManager", TrueFalseRegex()),

        // Klass, Grupp and Skolform+Årskurs are three competing ways to pick a cohort, so at most
        // one may contribute. Roll and Enhet narrow whichever way was chosen, or stand alone.
        [GroupMemberSource.Elevregister] = MemberSourceSpec.Independent()
            .AtLeastOneOf("Roll", "Enhet", "Klass", "Grupp", "Skolform", "Årskurs")
            .MutuallyExclusiveGroups(["Klass"], ["Grupp"], ["Skolform", "Årskurs"])
            .Repeatable("Årskurs")
            .Matches("Roll", EregRollRegex())
            .Matches("Enhet", EregEnhetIdRegex())
            .Matches("Klass", EregKlassIdRegex())
            .Matches("Grupp", EregGruppIdRegex())
            .Matches("Skolform", EregSkolformRegex())
            .Matches("Årskurs", EregArskursRegex()),

        [GroupMemberSource.OnPremAdGroup] = MemberSourceSpec.OnPrem()
            .Required("Group")
            .Matches("Group", GuidRegex())
            .Custom(new OnPremAdValidator()),

        [GroupMemberSource.OnPremAdQuery] = MemberSourceSpec.OnPrem()
            .Required("LdapFilter")
            .Optional("SearchBase"),

        [GroupMemberSource.AzureAdGroup] = MemberSourceSpec.Azure()
            .Required("Group")
            .Matches("Group", GuidRegex())
            .Custom(new AzureAdValidator()),

        // No self-reference validator, unlike its Entra ID and on-premises equivalents. Pinned by
        // TestExoGroupSelfReferenceIsCurrentlyAllowed; closing the gap would start rejecting
        // documents that validate today.
        [GroupMemberSource.ExoGroup] = MemberSourceSpec.Azure()
            .Required("Group")
            .Matches("Group", GuidRegex()),

        [GroupMemberSource.CustomView] = MemberSourceSpec.Independent()
            .Required("View"),

        [GroupMemberSource.Static] = MemberSourceSpec.Independent()
            .Required("Upn")
            .Repeatable("Upn")
            .Custom(new UpnValidator())
    };

    [GeneratedRegex("^[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex GuidRegex();

    [GeneratedRegex("^011J[0-9A-Z]{8}$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex PersonecIdRegex();

    [GeneratedRegex("^(true|false)$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex TrueFalseRegex();

    [GeneratedRegex("^(ARA|ELOF|S_?[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}|[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12})$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex EregEnhetIdRegex();

    [GeneratedRegex("^(EG_?[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}|[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12})$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex EregKlassIdRegex();

    [GeneratedRegex("^(FG_?[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}|[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12})$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex EregGruppIdRegex();

    [GeneratedRegex("^(Personal|Elev)$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex EregRollRegex();

    [GeneratedRegex("^[0-9F]$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex EregArskursRegex();

    [GeneratedRegex("^(FSK|GR|GRSÄR|GY|GYSÄR)$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex EregSkolformRegex();
}
