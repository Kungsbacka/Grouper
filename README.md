# Grouper

## Description

Grouper manages group membership for Active Directory, Entra ID, Exchange Online (distribution groups), and OpenE Platform.

Grouper is comprised of four parts. Core functionality, database access, logging, and group management are found in GrouperLib.
GrouperApi exposes Grouper functionality as a web API. [PSGrouper](https://github.com/Kungsbacka/PSGrouper) uses the web API to
create a PowerShell module for working with Grouper documents. Finally, the Grouper service is a document processor that reads
published documents from the Grouper database and updates group members.

## Dependencies

Dependencies can vary depending on what kind of groups Grouper should manage (Entra ID, on-premise AD, or EXO) and which sources
are used for members. Below is a list of all external dependencies:

* Access to Entra ID and an Entra ID app registration with permission to read and write group members.
* Access to on-premise AD and a user account (or gMSA) with permission to read and write group members.
* Access to Exchange Online and a user account with permission to read and write distribution group members.
* Access to a database for Grouper documents (more information below).
* Access to a log database
* Access to a meta-directory database for information about group members.

## API

GrouperApi exposes the most used functions for working with documents and groups.

## Deploying

### Grouper service

* Copy App.example.config to App.Debug.config and App.Release.config
* Update configuration files to match your environment. You are strongly advised to encrypt all
secrets (see [Encrypting secrets](#encrypting-secrets) below)
* Build
* Copy DLLs and config to the server that is going to run the service
* Install service (example with sc below. Remove the password parameter if you are using a gMSA)

```batch
sc.exe create GrouperService binPath= "C:\Program Files\Grouper\GrouperService.exe" start= auto obj= user password= pass
```

### API

* Copy appsetting.Example.json to appsettings.Development.json and appsettings.Production.json
* Update configuration files to match your environment. You are strongly advised to encrypt all
secrets (see [Encrypting secrets](#encrypting-secrets) below)
* Build. There have been issues with using `win-x64` as runtime identifier. To avoid issues, use `win10-x64` when deploying to Windows Server.
* Deploy to a website that is configured with Windows Authentication. If you want to use Exchange Online
as a group store and a member source, you also have to install PowerShell 7 or later and the Exchange Online
PowerShell module (ExchangeOnlineManagement).

### PowerShell module

See [PSGrouper](https://github.com/Kungsbacka/PSGrouper).

### Encrypting secrets

It is recommended that all secrets in the configuration file are encrypted. Grouper supports
DPAPI protected secrets in all configuration files. To protect a string with DPAPI you do the
following:

1. Start PowerShell as the user that will run GrouperService or GrouperApi (if it's a gMSA you can use
[PsExec](https://docs.microsoft.com/en-us/sysinternals/downloads/psexec) to
start PowerShell: `psexec.exe -i -u DOMAIN\gmsa$ powershell.exe`).
2. Use tools/ProtectString.ps1 to encrypt the secret.
3. Paste the protected string into the configuration file.

### Certificate authentication

If you use certificate authentication for Graph and Exchange Online, and you store the certificates
in the LocalMachine store, you have to give the service account read permission to the private key.
You can do this using tools/GrantPrivateKeyAccess.ps1.

1. Import the certificate (including the private key) to Cert:\LocalMachine\My.
2. Give the service account read access to the key by running tools/GrantPrivateKeyAccess as an administrator.

## Grouper documents

A Grouper document describes a group and what members the group should contain.
For more information about Grouper documents and how to work with them, see
[PSGrouper](https://github.com/Kungsbacka/PSGrouper).
