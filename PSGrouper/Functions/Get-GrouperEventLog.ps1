<#
    .SYNOPSIS
        Gets entries from the event log

    .DESCRIPTION
        Gets entries from the event log. The event log contains errors, warnings
        and information genereated by Invoke-Grouper.

    .PARAMETER DocumentId
        Grouper document entry, Grouper document, [System.Guid] or a string that can
        be converted to a GUID.

    .PARAMETER GroupId
        Returns entries for a particular GroupId.

    .PARAMETER GroupDisplayNameContains
        Part of group display name. Does a wildcard search (*text*)

    .PARAMETER MessageContains
        Part of log message. Does a wildcard search (*text*)

    .PARAMETER LogLevel
        Filter entries by log level

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
        Get-GrouperDocumentEntry -GroupName 'MyGroup' | Get-GrouperEventLog -Newest 10

    .EXAMPLE
        Get-GrouperEventlLog -Start '2019-01-01' -End '2019-01-31' -LogLevel Error
#>
function Get-GrouperEventLog
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
        $GroupDisplayNameContains,
        [Parameter(Mandatory=$false,ParameterSetName='Newest')]
        [Parameter(Mandatory=$false,ParameterSetName='Range')]
        $MessageContains,
        [Parameter(Mandatory=$false,ParameterSetName='Newest')]
        [Parameter(Mandatory=$false,ParameterSetName='Range')]
        [GrouperLib.Core.LogLevels]
        $LogLevel,
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
        if ($LogLevel) {
            $query.LogLevel = $LogLevel
        }
        if ($GroupDisplayNameContains) {
            $query.GroupDisplayNameContains = $GroupDisplayNameContains
        }
        if ($MessageContains) {
            $query.MessageContains = $MessageContains
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
        $url = GetApiUrl 'eventlog'
        $items = ApiGetLogItems $url $query
        foreach ($item in  $items) {
            $argList = @(
                $item.logTime
                $item.documentId
                $item.groupId
                $item.groupDisplayName
                $item.groupStore
                $item.message
                $item.logLevel
            )
            New-Object -TypeName 'GrouperLib.Core.EventLogItem' -ArgumentList $argList
        }
    }
}

Export-ModuleMember -Function 'Get-GrouperEventLog'
