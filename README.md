# Grouper

## Description

Grouper manages group membership for on-premise AD groups, Azure AD groups and Exchange Online (EXO) distribution groups.

Grouper is comprised of four parts. Core functionality, database access, logging and group management are found in GrouperLib.
GrouperApi exposes Grouper functionality as a web API. PSGrouper uses the web API to create a PowerShell module for working
with Grouper documents. Finally the Grouper service is a document processor that reads published documents from the Grouper
database and updates group members.

## Dependencies

The PowerShell module work with both PowerShell 5.1 and PowerShell 7. GrouperLib is targeting .NET Standard 2.0 and have been
successfully built for both .NET Framework 4.7.2 and .NET Core 3.0.

Dependencies can vary depending on what kind of groups are involved (Azure AD, on-premise AD or EXO) and where information
about members are coming from. Below is a list of all external dependencies:

* Access to Azure AD and an Azure AD app registration with permission to read and write group members.
* Access to on-premise AD and a user account (or gMSA) with permission to read and write group members.
* Access to Exchange Online and a user account with permission to read and write distribution group members.
* Access to a database for Grouper documents (more information below).
* Access to a log database
* Access to a metadirectory or similar to use as a source for group members.

## API

GrouperApi exposes the most important functions for working with documents and updating groups.

The API uses Windows Authentication just to get the API up and running faster, but a transition to OIDC/OAuth2
will likely happen in the near future.

## Deploying

### Grouper service

* Copy App.example.config to App.Debug.config and App.Release.config
* Update configuration files to match your environment. You are strongly advised to encrtypt all
secrets (see [Encrypting secrets](#encrypting-secrets) below)
* Build
* Copy DLLs and config to the server that is going to run the service
* Install service (example with sc below. Remove the password parameter if you are using a gMSA)

```batch
sc.exe create GrouperService binPath= "C:\Program Files\Grouper\GrouperService.exe" start= auto obj= user password= pass
```

### API

* Copy appsetting.example.json to appsettings.Development.json and appsettings.Production.json
* Update configuration files to match your environment. You are strongly advised to encrtypt all
secrets (see [Encrypting secrets](#encrypting-secrets) below)
* Build
* Deploy to a web site that is configured with Windows Authentication

### PowerShell module

* Copy the PowerShell module to a folder that is included in the PSModulePath (if you want the module to autoload)
* Build GrouperLib (CompileTargetFramework or CompileTargetCore depending on PowerShell version)
* Copy GrouperLib.Core.dll, Newtonsoft.Json.dll, GrouperLib.Language.dll the module folder (if you want support for Swedish, also copy sv\GrouperLib.Language.resources.dll to <module folder>\sv)

### Encrypting secrets

It is recommended that all secrets in the configuration files are encrypted. Grouper supports
DPAPI protected secrets in all configuration files. To protect a string with DPAPI you do the
following:

1. Start PowerShell as the user that will run GrouperService or GrouperApi (if it's a gMSA you can use
[PsExec](https://docs.microsoft.com/en-us/sysinternals/downloads/psexec) to
start PowerShell: `psexec.exe -i -u DOMAIN\gmsa$ powershell.exe`).
2. Use tools/ProtectString.ps1 to encrypt the secret.
3. Paste the protected string into the configuration file.

## Working with Grouper documents

Below are some examples of how to perform common tasks. Take a look at the cmdlet help for more information.

Before you can use the PowerShell module you have to connect to the API using Connect-GrouperApi.

```PowerShell
# Connect to the API
Connect-GrouperApi -Uri 'https://api-server/path/to/api'

# Process a single document
Get-GrouperDocumentEntry -GroupName 'My Group' | Invoke-Grouper

# Process all published documents in the database
Get-GrouperDocumentEntry -All | Invoke-Grouper

# Edit a document, save and publish in one go
Get-GrouperDocumentEntry -GroupName 'My Group' | Edit-GrouperDocument | Save-GrouperDocument -Publish

# Create a new document, edit and save (without publishing)
# A new document always contains one member object (static) as a placeholder just to make the document valid.
# Remove or edit the member object to match your needs.
New-GrouperDocument -GroupId '4a31e904-a33a-476e-95da-4d0ec7ab602a' -GroupName 'My Group' -Store AzureAd | Edit-GrouperDocument | Save-GrouperDocument

# It is also possible to create new documents by hand. Create a document in your favorite text editor,
# save the document as a JSON file
Get-Content document.json | ConvertTo-GrouperDocument | Save-GrouperDocument

# ...or straight from the clipboard
Get-Clipboard | ConvertTo-GrouperDocument | Save-GrouperDocument

# To check if a "hand made" document is valid before converting, use Test-GrouperDocument
Get-Content document.json | Test-GrouperDocument -OutputErrors
```

## Group document

A Grouper document contains all information required by Grouper to process a single group.
Name, ID and Store are required properties. The document must also contain at least one
member object that describes the members that the group should contain.

Interval is used by the GrouperService as a recommended (but not contractual) processing
interval in minutes for a document. GrouperProcess will try to process the document in
the given interval. A document with an interval of zero (default) will be processed either
when GrouperService does a full pass through all published documents, or when the document
is updated in the document database.

A full pass is done three times a day (at 6 am, 12 pm and 4 pm). This is hardcoded into the
service (see method ShouldProcessAllDocuments), but may be configurable at a later time.

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

Grouper documents can be stored anywhere, but some of the PowerShell cmdlets, the API and
the service can only work with documents stored in the Grouper database.

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
