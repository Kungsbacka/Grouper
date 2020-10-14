# Grouper

## Description

Grouper manages group membership for on-premise AD groups, Azure AD groups and Exchange Online (EXO) distribution groups.

Grouper is comprised of four parts. Core functionality, database access, logging and group management are found in GrouperLib.
GrouperApi exposes Grouper functionality as a web API. PSGrouper uses the web API to create a PowerShell module for working
with Grouper documents. Finally the Grouper service is a document processor that uses published documents from the Grouper
database to make changes to the corresponding groups.

## Dependencies

The PowerShell module work with both PowerShell 5.1 and PowerShell 7. GrouperLib is targeting .NET Standard 2.0 and have been
successfully built for both .NET Framework 4.7.2 and .NET Core 3.0.

Dependencies can vary depending on what kind of groups (Azure AD, on-premise AD or EXO) and where information about
members are coming from. Below is a list of all external dependencies:

* Access to Azure AD and an Azure AD app registration with permission to read and write group members.
* Access to on-premise AD and a user account (or gMSA) with permission to read and write group members.
* Access to Exchange Online and a user account with permission to read and write distribution group members.
* Access to a database for Grouper documents (more information below).
* Access to a log database
* Access to a meta directory or similar to use as a source for group members.

## API

GrouperApi exposes the most important functions for working with documents and updating groups.

The API uses Windows Authentication just to get the API up and running faster, but a transition to OIDC/OAuth2
will likely happen in the near future.

## Deploying

Both the API and the service needs configuration to function. F

### Grouper service

* Copy App.example.config to App.Debug.config and App.Release.config
* Update configuration files to match your environment. You are strongly advised to encrtypt all
secrets (see [Encrypting secrets](#encrypting-secrets) below)
* Build
* Copy DLLs and config to the server that is going to run the service
* Install service with sc. If you are using a gMSA then remove the password parameter.

```batch
sc.exe create WebSolenFileMover binPath= "C:\Program Files\Grouper\GrouperService.exe" start= auto obj= user password= pass
```

### API

* Copy appsetting.example.json to appsettings.Development.json and appsettings.Production.json
* Update configuration files to match your environment. You are strongly advised to encrtypt all
secrets (see [Encrypting secrets](#encrypting-secrets) below)
* Build
* Deploy to a web server that can do Windows Authentication

### PowerShell module

* Copy the PowerShell module to a folder that is included in the PSModulePath if you want the module to autoload.
* Build GrouperLib (CompileTargetFramework or CompileTargetCore depending on PowerShell version)
* Copy GrouperLib.Core.dll to the module folder.

### Encrypting secrets

It is recommended that all secrets in the configuration file are encrypted. Grouper supports
DPAPI protected strings in the configuration file. To protect a string with DPAPI you can do
the following:

1. Start PowerShell as the user that will run Grouper (if it's a gMSA you can use PsExec to
start PowerShell: psexec -i -u DOMAIN\gmsa$ powershell.exe).
2. Use tools/ProtectString.ps1 to encrypt the secret.
3. Paste the protected string into the configuration file.

## Working with Grouper documents

Below are some examples of how to perform common tasks. Take a look at the cmdlet help for more information.

Before you can use the PowerShell module you have to connect to the API using Connect-GrouperApi.

```PowerShell
# Process a single document
Get-GrouperDocumentEntry -GroupName 'My Group' | Invoke-Grouper

# Process all published documents in the database
Get-GrouperDocumentEntry -All | Invoke-Grouper

# Edit a document, save and publish in one go
Get-GrouperDocumentEntry -GroupName 'My Group' | Edit-GrouperDocument | Save-GrouperDocument -Publish

# Create a new document, edit and save (without publishing)
# A new document always contains one member object (static) as a placeholder to make the document valid.
# Remove or edit the member object to match your needs.
New-GrouperDocument -GroupId '4a31e904-a33a-476e-95da-4d0ec7ab602a' -GroupName 'My Group' -Store AzureAd | Edit-GrouperDocument | Save-GrouperDocument

# It is also possible to create new documents by hand. Create a document in your favorite text editor,
# copy document to clipboard, and...
Get-Clipboard | ConvertTo-GrouperDocument | Save-GrouperDocument

# To check if a "hand made" document is valid before converting, use Test-GrouperDocument
Get-Clipboard | Test-GrouperDocument -OutputErrors
```

## Group document

A Grouper document contains all information required by Grouper to process a single group.
Name, id and store are required properties. The document must also contain at least one
member object that describes what members the group should contain.

Interval is used by the GrouperService as a recommended (but not guaranteed) processing interval in minutes for the document.

Below is an example where the group lives in Azure AD and the members come from a student
roster (elevregister).

```Json
{
  "id": "1c5ec8b9-05e6-467a-969c-fa9be4513126",
  "interval": 30,
  "groupId": "4a31e904-a33a-476e-95da-4d0ec7ab602a",
  "groupName": "Elever i klass 7A på Testskolan",
  "id": "4afb1b58-0e20-4cb8-832b-9bbfed0b8d02",
  "store": "AzureAd",
  "owners": "KeepAll",
  "members": [
    {
      "source": "Elevregister",
      "action": "Include",
      "rules": [
        {
          "name": "Roll",
          "value": "Elev"
        },
        {
          "name": "Klass",
          "value": "EG_41e60dc2-1300-471d-a3a9-674664320e25"
        }
      ]
    }
  ]
}
```

Grouper documents can be stored anywhere, but some of the PowerShell cmdlets only work with
documents that are stored in the Grouper database (see additional information below).

## Member sources

The following member sources are recognized by Grouper

* __Personalsystem__: members from employees in selected parts of the organization. Befattning (title)
can be used to filter members.
* __Elevregister__: members from Procapita/IST Administration. A combination of role, school,
class, group or year can be used.
* __Static__: Explicitly add members to a group by listing the UPN for each member.
* __CustomView__: Get members from a custom database view.
* __AzureAdGroup__: Get members from an Azure AD group.
* __ExoGroup__: Get members from an Exchange Online distribution group.
* __OnPremAdGroup__: Get members from an on-premise Active Directory group.
* __OnPremAdQuery__: Get members from an on-premise Active Directory LDAP query (with optional search base)

The first four sources requires access to a member database (e.g. a metadirectory) that can output
relevant user identities, but the last four only requires access to their respective catalog
(Azure AD, On-premise AD or Exchange Online).

## Group stores

Grouper can work with groups in the following locations:

* On-premise Active Directory
* Azure AD
* Exchange Online (distribution groups)
