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

See [PSGrouper](https://github.com/Kungsbacka/PSGrouper).

### Encrypting secrets

It is recommended that all secrets in the configuration files are encrypted. Grouper supports
DPAPI protected secrets in all configuration files. To protect a string with DPAPI you do the
following:

1. Start PowerShell as the user that will run GrouperService or GrouperApi (if it's a gMSA you can use
[PsExec](https://docs.microsoft.com/en-us/sysinternals/downloads/psexec) to
start PowerShell: `psexec.exe -i -u DOMAIN\gmsa$ powershell.exe`).
2. Use tools/ProtectString.ps1 to encrypt the secret.
3. Paste the protected string into the configuration file.

## Grouper documents

A Grouper document describes a group and what members the group should contain.
For more information about Grouper documents and how to work with them, see
[PSGrouper](https://github.com/Kungsbacka/PSGrouper).
