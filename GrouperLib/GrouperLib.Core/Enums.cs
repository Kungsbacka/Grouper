namespace GrouperLib.Core
{
    public enum GroupOwnerActions { AddAll, KeepExisting, MatchSource }
    public enum GroupMemberActions { Include, Exclude }
    public enum GroupStores { OnPremAd, AzureAd, Exo }
    public enum GroupMemberSources { Personalsystem, Elevregister, OnPremAdGroup, OnPremAdQuery, AzureAdGroup, ExoGroup, CustomView, Static }
    public enum GroupMemberTypes { OnPremAd, AzureAd }
    public enum GroupMemberOperations { Add, Remove, None }
    public enum LogLevels { Information = 1, Warning = 2, Error = 3 }
}
