<#
    .SYNOPSIS
        Gets entries from the operational log

    .DESCRIPTION
        Gets entries from the operational log that contains information about
        which members have been added or removed.
    .PARAMETER DocumentId
        Grouper document entry, Grouper document, [System.Guid] or a string that can
        be converted to a GUID.

    .PARAMETER GroupId
        Returns entries for a particular GroupId.

    .PARAMETER TargetId
        Returns entries for a particular TargetId.

    .PARAMETER GroupDisplayNameContains
        Part of group display name. Does a wildcard search (*text*)

    .PARAMETER TargetDisplayNameContains
        Part of target display name. Does a wildcard search (*text*)

    .PARAMETER Newest
        Number of new entries to return

    .PARAMETER Start
        Start of date interval. Returns entries on or after this date

    .PARAMETER End
        End of date interval. Returns entries before or on this date. If omitted,
        current date and time is used.

    .INPUTS
        (see InputObject)

    .EXAMPLE
        Get-GrouperDocumentEntry -GroupName 'MyGroup' | Get-GrouperOperationalLog -Newest 10

    .EXAMPLE
        Get-GrouperOperationalLog -Start '2019-01-01' -End '2019-01-31'
#>
function Get-GrouperOperationalLog
{
    [CmdletBinding(DefaultParameterSetName='Newest')]
    param (
        [Parameter(Mandatory=$false,Position=0,ValueFromPipeline=$true,ParameterSetName='Newest')]
        [Parameter(Mandatory=$false,Position=0,ValueFromPipeline=$true,ParameterSetName='Range')]
        [object]
        $DocumentId,
        [Parameter(Mandatory=$false,ParameterSetName='Newest')]
        [Parameter(Mandatory=$false,ParameterSetName='Range')]
        [guid]
        $GroupId,
        [Parameter(Mandatory=$false,ParameterSetName='Newest')]
        [Parameter(Mandatory=$false,ParameterSetName='Range')]
        [string]
        $GroupDisplayNameContains,
        [Parameter(Mandatory=$false,ParameterSetName='Newest')]
        [Parameter(Mandatory=$false,ParameterSetName='Range')]
        [string]
        $TargetDisplayNameContains,
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
        if ($DocumentId) {
            $docId = GetDocumentIdFromInputObject $DocumentId
            if (-not $docId) {
                return
            }
            $query.DocumentId = $docId
        }
        if ($GroupId) {
            $query.GroupId = $GroupId
        }
        if ($TargetId) {
            $query.TargetId = $TargetId
        }
        if ($GroupDisplayNameContains) {
            $query.GroupDisplayNameContains = $GroupDisplayNameContains
        }
        if ($TargetDisplayNameContains) {
            $query.TargetDisplayNameContains = $TargetDisplayNameContains
        }
        if ($PSCmdlet.ParameterSetName -eq 'Newest') {
            $query.Count = $Newest
        }
        else {
            $query.StartDate = $Start
            if ($End) {
                $query.EndDate = $End
            }
        }
        $url = GetApiUrl 'operationallog'
        $items = ApiGetLogItems $url $query
        foreach ($item in  $items) {
            $argList = @(
                $item.logTime
                $item.documentId
                $item.groupId
                $item.groupDisplayName
                $item.groupStore
                $item.operation
                $item.targetId
                $item.targetDisplayName
            )
            New-Object -TypeName 'GrouperLib.Core.OperationalLogItem' -ArgumentList $argList
        }
    }
}

Export-ModuleMember -Function 'Get-GrouperOperationalLog'
