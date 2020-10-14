<#
    .SYNOPSIS
        Gets membership diff for Grouper document

    .DESCRIPTION
        Gets a list of objects that will be added or removed from the group if Invoke-Grouper is executed.

    .PARAMETER InputObject
        Grouper document or database document entry

    .PARAMETER IncludeUnchanged
        Include members that will not be added or removed

    .INPUTS
        (see InputObject)

    .OUTPUTS
        PSObject {Operation, TargetId, TargetDisplayName}

    .EXAMPLE
        Get-Content .\group.json | Out-String | ConvertTo-GrouperDocument | Get-GrouperMemberDiff
#>
function Get-GrouperMemberDiff
{
    param (
        [Parameter(Mandatory=$true,Position=0,ValueFromPipeline=$true)]
        [object]
        $InputObject,
        [Parameter(Mandatory=$false)]
        [switch]
        $IncludeUnchanged
    )

    begin {
        if (-not (CheckApi)) {
            break
        }
    }

    process {
        $document = GetDocumentFromInputObject $InputObject
        if ($null -eq $document) {
            return
        }
        $url = GetApiUrl 'document' 'diff'
        if ($IncludeUnchanged) {
            $url = AddUrlParameter $url 'unchanged' 'true'
        }
        $diff = ApiPostDocument $url $document
        foreach ($item in $diff.Add) {
            if ($item -isnot [GrouperLib.Core.GroupMember]) {
                $item = [GrouperLib.Core.GroupMember]::new($item.id, $item.displayName, $item.memberType)
            }
            $obj = [GrouperLib.Core.GroupMemberOperation]::new($document, $item, 'Add')
            Write-Output -InputObject $obj
        }
        foreach ($item in $diff.Remove) {
            if ($item -isnot [GrouperLib.Core.GroupMember]) {
                $item = [GrouperLib.Core.GroupMember]::new($item.id, $item.displayName, $item.memberType)
            }
            $obj = [GrouperLib.Core.GroupMemberOperation]::new($document, $item, 'Remove')
            Write-Output -InputObject $obj
        }
        if ($IncludeUnchanged) {
            foreach ($item in $diff.Unchanged) {
                if ($item -isnot [GrouperLib.Core.GroupMember]) {
                    $item = [GrouperLib.Core.GroupMember]::new($item.id, $item.displayName, $item.memberType)
                }
                $obj = [GrouperLib.Core.GroupMemberOperation]::new($document, $item, 'None')
                Write-Output -InputObject $obj
            }
        }
    }
}

Export-ModuleMember -Function 'Get-GrouperMemberDiff'
