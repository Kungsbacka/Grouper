namespace GrouperLib.Core;

public enum GroupOwnerAction { AddAll, KeepExisting, MatchSource }
public enum GroupMemberAction { Include, Exclude }
public enum GroupStore { OnPremAd, AzureAd, Exo, OpenE }
public enum GroupMemberSource { Personalsystem, Elevregister, OnPremAdGroup, OnPremAdQuery, AzureAdGroup, ExoGroup, CustomView, Static }
public enum GroupMemberType { OnPremAd, AzureAd }
public enum GroupMemberOperation { Add, Remove, None }
public enum LogLevel { Information = 1, Warning = 2, Error = 3 }