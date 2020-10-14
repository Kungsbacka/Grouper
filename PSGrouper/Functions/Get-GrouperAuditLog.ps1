<#
    .SYNOPSIS
        Gets entries from the audit log.

    .DESCRIPTION
        Gets entries from the audit log. The audit log contains information about who
        made changes to a documents and what changes were made.

    .PARAMETER InputObject
        Grouper document entry, Grouper document, [System.Guid] or a string that can
        be converted to a GUID.

    .PARAMETER ActorContains
        Part of actor name. Does a wildcard search (*text*)

    .PARAMETER ActionContains
        Part of action. Does a wildcard search (*text*)

    .PARAMETER Newest
        Number of the most recent entries to return.

    .PARAMETER Start
        Start of date interval. Returns entries on or after this date

    .PARAMETER End
        End of date interval. Returns entries before or on this date. If omitted,
        current date and time is used.

    .INPUTS
        (see InputObject)

    .EXAMPLE
        Get-GrouperDocumentEntry -GroupName 'MyGroup' | Get-GrouperAuditLog -Newest 10

    .EXAMPLE
        Get-GrouperAuditLog -Start '2019-01-01' -End '2019-01-31'
#>
function Get-GrouperAuditLog
{
    [CmdletBinding(DefaultParameterSetName='Newest')]
    param (
        [Parameter(Mandatory=$false,Position=0,ValueFromPipeline=$true,ParameterSetName='Newest')]
        [Parameter(Mandatory=$false,Position=0,ValueFromPipeline=$true,ParameterSetName='Range')]
        [object]
        $InputObject,
        [Parameter(Mandatory=$false,ParameterSetName='Newest')]
        [Parameter(Mandatory=$false,ParameterSetName='Range')]
        [string]
        $ActorContains,
        [Parameter(Mandatory=$false,ParameterSetName='Newest')]
        [Parameter(Mandatory=$false,ParameterSetName='Range')]
        [string]
        $ActionContains,
        [Parameter(Mandatory=$false,ParameterSetName='Newest')]
        [int]
        $Newest = 10,
        [Parameter(Mandatory=$true,ParameterSetName='Range')]
        [DateTime]
        $Start,
        [Parameter(Mandatory=$false,ParameterSetName='Range')]
        [DateTime]
        $End
    )

    begin {
        if (-not (CheckApi)) {
            break
        }
    }

    process {
        $query = @{}
        if ($InputObject) {
            $docId = GetDocumentIdFromInputObject $InputObject
            if (-not $docId) {
                return
            }
            $query.DocumentId = $docId
        }
        if ($Actor) {
            $query.ActorContains = $ActorContains
        }
        if ($Action) {
            $query.ActionContains = $ActionContains
        }
        if ($PSCmdlet.ParameterSetName -eq 'Newest') {
            $query.Count = $Newest
        }
        else {
            $query.StartDate = $Start
            if ($End) {
                $query.End = $End
            }
        }
        $items = ApiGetLogItems (GetApiUrl 'auditlog') $query
        foreach ($item in  $items) {
            $argList = @(
                $item.logTime
                $item.documentId
                $item.actor
                $item.action
                $item.additionalInformation
            )
            New-Object -TypeName 'GrouperLib.Core.AuditLogItem' -ArgumentList $argList
        }
    }
}

Export-ModuleMember -Function 'Get-GrouperAuditLog'
