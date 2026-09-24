# Grouper

## What Grouper is

Grouper makes group membership declarative. Instead of adding and removing members by hand, you write
a **Grouper document**: a JSON file that describes one group together with rules for who belongs in
it. A background service reads the published documents and keeps each real group in step with its
rules, adding and removing members as the underlying source data changes.

Grouper can manage groups in on-premises Active Directory, in Entra ID, as Exchange Online
distribution groups, and in Open ePlatform. Members can come from eight different sources, including a
staff register, a student register, other directory groups, and lists of individual users.

The system has four parts:

| Part | What it does |
| --- | --- |
| GrouperLib | All the domain logic: document validation, membership rules, store adapters and data access. |
| GrouperService | A Windows service that processes published documents on a schedule. It is the only part that writes to groups automatically. |
| GrouperApi | A web API that exposes the document and group operations. |
| [PSGrouper](https://github.com/Kungsbacka/PSGrouper) | A PowerShell module for authoring documents and reading logs. It works through the API. |

## Dependencies

Dependencies can vary depending on what kind of groups Grouper should manage (Entra ID, on-premise AD, or EXO) and which sources
are used for members. Below is a list of all external dependencies:

* Access to Entra ID and an Entra ID app registration with permission to read and write group members.
* Access to on-premise AD and a user account (or gMSA) with permission to read and write group members.
* Access to Exchange Online and a user account with permission to read and write distribution group members.
* Access to a database for Grouper documents (more information below).
* Access to a log database
* Access to a meta-directory database for information about group members.

## Building and deploying

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

* Register the event source. The service writes its start and stop messages, together with errors it
cannot handle on its own, to the Application log under the source name `GrouperService`. Windows requires
the source to be registered before the first entry can be written, and `sc.exe` does not create it. The
command below has to run elevated, and is only needed once per server. If the source is missing, the
service still runs, but the messages are lost.

```powershell
[System.Diagnostics.EventLog]::CreateEventSource('GrouperService', 'Application')
```

### API

* Copy appsetting.Example.json to appsettings.Development.json and appsettings.Production.json
* Update configuration files to match your environment. You are strongly advised to encrypt all
secrets (see [Encrypting secrets](#encrypting-secrets) below)
* Build. There have been issues with using `win-x64` as runtime identifier. To avoid issues, use `win10-x64` when deploying to Windows Server.
* Deploy to a website that is configured with Windows Authentication.

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
in the LocalMachine store, you have to give the service account (for GrouperService) or application pool (for the API) read permission for the private key. You can do this using tools/GrantPrivateKeyAccess.ps1.

1. Import the certificate (including the private key) to Cert:\LocalMachine\My.
2. Run tools/GrantPrivateKeyAccess as an administrator.
3. Enter certificate thumbprint
4. Enter DOMAIN\gMSA for a service account, or IIS APPPOOL\<application pool name> for an application pool.

## Working with Grouper documents

Everything about authoring documents lives in the
[PSGrouper README](https://github.com/Kungsbacka/PSGrouper), because that is the tool you use to do
it. It covers:

* the document format, field by field, with complete examples
* all eight member sources, which rules each one accepts, and the format of every rule value
* how include and exclude interact, and why `Static` members always win
* group owners and the change ratio guard
* which member sources work with which group stores, and why they cannot be mixed freely

The rest of this document describes how Grouper is built, and is aimed at developers rather than at
the people who write documents.

## Architecture

### Component map

| Piece | Path | Role |
| --- | --- | --- |
| GrouperLib | [GrouperLib](GrouperLib) | Eight projects, `net10.0`. All domain logic, validation, store adapters, data access. |
| GrouperService | [GrouperService](GrouperService) | Windows Service (`ServiceBase`). The only component that writes to groups on a schedule. |
| GrouperApi | [GrouperApi](GrouperApi) | ASP.NET Core, Windows/Negotiate auth. The operator-facing surface. |
| PSGrouper | [Kungsbacka/PSGrouper](https://github.com/Kungsbacka/PSGrouper) | PowerShell 7.6 module, 23 cmdlets, driven through the API. |
| database | [database/grouper.sql](database/grouper.sql) | Document store plus three logs. |
| json-schema | [json-schema/document.json](json-schema/document.json) | Draft-07 schema for editor tooling. |
| tools | [tools](tools) | `ProtectString.ps1` (DPAPI-encrypt a config secret), `GrantPrivateKeyAccess.ps1`. |

Inside GrouperLib the dependency direction is one-way:

```text
Language <- Core <- ┬ Store    ┐
                    └ Database ┴ <- Backend
Config <- Store, Database, Backend
```

[GrouperLib.Core](GrouperLib/GrouperLib.Core) holds the domain model and all validation, and has
**no I/O dependencies at all** — no SQL client, no Graph, no LDAP. Neither `Core` nor `Language`
references a single NuGet package. That is what lets PSGrouper load `GrouperLib.Core.dll` on the
operator's machine and build and validate documents entirely offline, before anything reaches the
server.

### The Grouper document

Defined by [GrouperDocument.cs](GrouperLib/GrouperLib.Core/GrouperDocument.cs):

```json
{
  "id": "1c5ec8b9-05e6-467a-969c-fa9be4513126",
  "interval": 30,
  "groupId": "4a31e904-a33a-476e-95da-4d0ec7ab602a",
  "groupName": "Elever i klass 7A",
  "store": "AzureAd",
  "owner": "KeepExisting",
  "members": [
    { "source": "Elevregister", "action": "Include",
      "rules": [ { "name": "Roll", "value": "Elev" },
                 { "name": "Klass", "value": "EG_41e60dc2-…" } ] }
  ]
}
```

Each member object is `{source, action, rules[]}`; each rule is a `{name, value}` pair whose meaning
depends on the source. `interval` is a processing-interval *hint* in minutes. The PSGrouper README
documents every field and every rule name in full.

The type is immutable — get-only properties and an `internal` constructor. It can only be created
through `GrouperDocument.Create(...)` or `FromJson(...)`, and both validate first, returning `null`
(or throwing `InvalidGrouperDocumentException`) on failure. **An invalid Grouper document cannot
exist as an object**, which is why the rest of the codebase never re-checks document shape.

### Document validation

Validation is split across two files. The engine is in
[DocumentValidator.cs](GrouperLib/GrouperLib.Core/DocumentValidator.cs) and is about 180 lines.
What each member source allows is declared separately, in
[DocumentValidator.Sources.cs](GrouperLib/GrouperLib.Core/DocumentValidator.Sources.cs), in about
80 lines. The split is deliberate. Every check in the engine applies to all eight sources, and no
part of the engine needs to know which source it is looking at.

Each source is described by a `MemberSourceSpec`, which is built with a small fluent API.
`Personalsystem` reads like this:

```csharp
[GroupMemberSource.Personalsystem] = MemberSourceSpec.Independent()
    .AtLeastOneOf("Organisation", "Befattning", "Plats")
    .Requires(dependent: "IncludeManager", prerequisite: "Organisation")
    .Repeatable("Befattning")
    .Matches("Organisation", PersonecIdRegex())
    .Matches("Plats", PersonecPlatsRegex())
    .Matches("IncludeManager", TrueFalseRegex()),
```

Four of these methods add a **clause**. A clause is a rule about which combinations of rule names
are allowed, and each one is a small class in
[RuleClause.cs](GrouperLib/GrouperLib.Core/RuleClause.cs):

| Clause | What it means |
| --- | --- |
| `Required(name)` | This name must be present. |
| `AtLeastOneOf(names)` | At least one of these names must be present. |
| `Requires(dependent, prerequisite)` | If the first name is present, the second one must be present too. |
| `MutuallyExclusiveGroups(groups)` | Rule names may be taken from at most one of the groups. |

`Elevregister` is the source that benefits most from this. It previously listed all 23 allowed
combinations of its six rule names, one row at a time. It now says the same thing in two lines:

```csharp
.AtLeastOneOf("Roll", "Enhet", "Klass", "Grupp", "Skolform", "Årskurs")
.MutuallyExclusiveGroups(["Klass"], ["Grupp"], ["Skolform", "Årskurs"])
```

The remaining methods do not constrain combinations at all. `Optional(names)` only declares that a
name is recognised. `Repeatable(name)` allows a name to appear more than once in the same member
object. `Matches(name, regex)` gives a rule value a format it has to satisfy. `Custom(validator)`
attaches an `ICustomValidator` for checks that no clause can express, such as rejecting a document
that draws its members from its own target group.

The set of recognised rule names is not written down separately. It is collected from the names that
the clauses mention, together with anything passed to `Optional`. A rule name therefore cannot be
recognised unless a clause or an `Optional` call has introduced it, which keeps the two from drifting
apart.

Rule **names** are compared exactly, including their casing, so `organisation` is rejected as an
unrecognised name. Rule **values** are compared without regard to case. The distinction matters,
because the value formats are looked up by rule name. If names were compared loosely, a
differently-cased name would pass the name check and then quietly skip its own format check.

When a combination is rejected, the engine asks the clause that failed to describe the problem, and
each clause produces its own message. An administrator therefore sees something specific, such as
"Rule name IncludeManager can only be used together with Organisation", instead of a general
statement that the combination is not allowed. Only the first failing clause is reported, so one
problem is fixed at a time.

A `ResourceLocation` cross-check (`OnPrem`, `Azure` or `Independent`) rejects on-premises sources
feeding cloud groups, and cloud sources feeding on-premises groups. All messages exist in English and
Swedish in `GrouperLib.Language`, keyed by the constants in `ResourceString`.

### How a membership change flows

The pipeline lives in [Grouper.cs](GrouperLib/GrouperLib.Backend/Grouper.cs). `Grouper` is a
registry: `AddGroupStore`, `AddMemberSource` and `AddGroupOwnerSource` index each implementation
under the enum values it reports supporting, and `Grouper.CreateFromConfig` wires them from
configuration.

1. **Read current state** — `GetMemberDiffAsync` asks the document's `IGroupStore` for the group's
   actual members.
2. **Resolve target state** — each member object goes to its `IMemberSource`, then results are
   combined in a deliberate four-step order (`Grouper.cs:257-263`): non-static includes → subtract
   non-static excludes → add static includes → subtract static excludes. **Static membership
   therefore always wins** over rule-derived membership.
3. **The exclude-only special case** (`Grouper.cs:239-243`) — a document with *no* include rules seeds
   the target from the group's **current** members instead of from nothing. This is what allows a
   pair of groups with inverse rules: one automatically managed, the other manually managed but
   guaranteed not to overlap. Without it, such a document would simply empty the group.
4. **Owners** — folded in per `GroupOwnerAction`: `AddAll`, `KeepExisting` (owners intersected with
   current members) or `MatchSource` (owner source not consulted at all). Two things worth knowing:
   owners are added *after* the include/exclude pipeline, so an exclude rule naming an owner does
   not keep that owner out; and only `AzureAd` implements `IGroupOwnerSource`, so the owner action
   is inert for `OnPremAd`, `Exo` and `OpenE` documents.
5. **Change-ratio guard** — `UpdateGroupAsync` refuses to write when the ratio falls below
   `ChangeRatioLowerLimit`, throwing `ChangeRatioException`. `ignoreChangeLimit` overrides it.
6. **Write and log** — removals first, then additions, each recorded in the operational log.

#### What the change ratio actually measures

The formula (`Grouper.cs:165`) is `(currentCount - removals + additions) / currentCount`, which is
the group's **resulting size divided by its current size**. So a limit of `0.5` means "refuse if the
group would shrink below half its current size". It is a floor on shrinkage rather than a cap on
churn, and that is a deliberate design decision, not an oversight.

The failure mode worth guarding against is a member source returning empty or partial data, and that
always manifests as the group collapsing in *size* — which this formula catches. Identity turnover at
stable size is a legitimate business event: many education groups replace their entire membership at
each new school year. An earlier retention-based measure did catch those swaps, but it flagged so
many groups every August that they had to be reviewed and re-run by hand. The probability of an
erroneous full swap is low, so that risk is knowingly accepted in exchange for unattended school-year
rollover. Consequences:

* Growth always passes; the ratio exceeds 1.
* **Replacing every member yields exactly 1.0** and passes any limit at or below one. This is the
  intended behaviour described above, and the reason the guard is not retention-based.
* With an empty group (`Grouper.cs:159-161`) the value is the raw target *count* rather than a ratio,
  so populating an empty group is never blocked.

### Store and source adapters

The `IGroupStore` / `IMemberSource` interfaces hide four very different transports:

* [AzureAd.cs](GrouperLib/GrouperLib.Store/AzureAd.cs) — Microsoft Graph SDK with
  `PageIterator`. Group store, member source, and the only owner source.
* [Exo.cs](GrouperLib/GrouperLib.Store/Exo.cs) — direct REST against
  `outlook.office365.com/adminapi/beta/{tenant}/InvokeCommand`, posting EXO cmdlets
  (`Get-DistributionGroupMember`, `Add-`/`Remove-DistributionGroupMember`) as JSON. Carries a custom
  `EntraTokenHandler` and a `ThrottleRetryHandler` (4 attempts, honours `Retry-After`, 20s cap,
  deliberately does *not* retry 500). Paging is bounded twice: it throws if a `nextLink` repeats and
  again at a `MaxPages` ceiling of 105. Known errors are recovered by regex-matching EXO's English
  prose messages.
* [OnPremAd.cs](GrouperLib/GrouperLib.Store/OnPremAd.cs) /
  [Ldap.cs](GrouperLib/GrouperLib.Store/Ldap.cs) —
  `System.DirectoryServices.Protocols`, Kerberos with sealing and signing, paged at 1000, 10-minute
  cache of GUID→DN lookups.
* [OpenE.cs](GrouperLib/GrouperLib.Store/OpenE.cs) — SQL stored procedures
  (`dbo.spOpenE*`). Group store only, no member source.
* [MemberDb.cs](GrouperLib/GrouperLib.Database/MemberDb.cs) — the metadirectory, and the
  source for everything that is not a directory group: `Personalsystem`, `Elevregister`, `Static`
  and `CustomView`, each via its own `dbo.spGrouper*` proc.

`MemberDb` resolves each person to whichever identifier the target group needs, reading either the
on-premises AD GUID or the Entra ID GUID from the same row. Anybody whose GUID for that directory is
missing is silently skipped, so a group can come out smaller than expected when an account has not
been synchronised.

Entra ID and EXO each accept a client secret or a certificate loaded from file, base64 or the
Windows certificate store. Any setting can be DPAPI-protected individually by listing its name in
`DpapiProtectedSettings` —
[GrouperConfiguration.cs](GrouperLib/GrouperLib.Config/GrouperConfiguration.cs) decrypts
on property read; `tools/ProtectString.ps1` produces the encrypted values.

### Persistence

From [grouper.sql](database/grouper.sql) — three concerns in one database, all reached
through stored procedures. Data access is raw `Microsoft.Data.SqlClient`; there is no ORM and no
inline SQL in C#.

**Documents.** The `document` table is keyed `(document_id, revision)`. The JSON *is* the record;
`group_id`, `group_store`, `group_name` and `processing_interval` are computed columns projected out
of it with `JSON_VALUE`. Nothing is mutated in place: `update_document` inserts a new revision and
resets `published` to 0, so publishing is always a deliberate separate act. The `latest_revision`
and `latest_revision_flattened` views (the latter `OPENJSON`-expanded down to individual rules) back
the query procs, which is how "find every document with rule X" works without a search index.

The invariants live in the procs, not in application code: one *published* document per group, group
ID and store immutable for the life of a document, no updating a deleted document, deleting also
unpublishes.

**Logs.** `audit_log` — who changed which document, written by the document procs themselves so it
cannot be bypassed. `event_log` — errors and warnings from the service, per document.
`operational_log` — every individual member add and remove.

### The two runtime hosts

**GrouperService** ([Worker.cs](GrouperService/GrouperService/Worker.cs)) runs a
10-second non-reentrant `System.Timers.Timer`. Each tick it picks up documents created or changed in
the last 10 seconds, plus documents whose `interval` hint has elapsed. Three times a day — 06:00,
12:00 and 16:00, hardcoded in `ShouldProcessAllDocuments` — it instead does a full pass over every
published document, then rebuilds the `Grouper` instance to refresh tokens and connections.

Documents are processed strictly sequentially, and a failure on one is logged and skipped rather
than aborting the tick. `GroupNotFoundException`, `MemberNotFoundException` and
`ChangeRatioException` are treated as expected operational noise and go only to the log database;
anything else also goes to the Windows event log. Run interactively, the same worker prints to the
console instead of logging.

**GrouperApi** ([Program.cs](GrouperApi/GrouperApi/Program.cs)) uses Negotiate/Windows
authentication. AD groups map to authorization policies through a `RoleMapping` configuration
section, plus a synthetic `All` policy meaning "member of any mapped role". Read endpoints require
`All`, mutating ones require `Admin`. Controllers: `Document` (query by
id/group/name/source/rule, store, publish, unpublish, delete, restore, revisions, tags, validate),
`Grouper` (`diff` and `invoke`), `GroupInfo`, `AuditLog`, `EventLog`, `OperationalLog`, `Test`
(`echo`, `version`) and `Error`.

The `Grouper` backend is a singleton, but `DocumentDb` is constructed per request from
`HttpContext.User.Identity.Name` — that is how the caller's Windows identity ends up as the `author`
on every audit-log row.
