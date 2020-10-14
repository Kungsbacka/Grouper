<#
    .SYNOPSIS
        Performs group member changes based on supplied Grouper document

    .DESCRIPTION
        Processes a Grouper document and makes changes to group members based on the
        rules in the document.
        Invoke-Grouper has no WhatIf switch and will always try to make requested
        changes to a group. To test what will happen when Invoke-Grouper is called
        on a document without making any changes, use Get-GrouperMemberDiff.

    .PARAMETER InputObject
        Grouper document or database document entry

    .PARAMETER Force
        Ignores configured change limit

    .PARAMETER PassThru
        PassThru will make Invoke-Grouper output all generated log items, both event
        (errors, warnings) and operational (changes to members).

    .INPUTS
        (see InputObject)

    .OUTPUTS
        Log items. An OperationalLogItem for each group member change (add/remove), or
        an ErrorLogItem if an error occurs.

    .EXAMPLE
        Get-GrouperDocumentEntry -GroupName 'MyGroup' | Invoke-Grouper $doc

    .EXAMPLE
        Get-GrouperDocumentEntry -All | Invoke-Grouper
#>
function Invoke-Grouper
{
    [CmdletBinding(SupportsShouldProcess=$true)]
    param (
        [Parameter(Mandatory=$true,Position=0,ValueFromPipeline=$true)]
        [object]
        $InputObject,
        [Parameter(Mandatory=$false)]
        [switch]
        $Force
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
        if ($Force) {
            Write-Warning 'Change limit is ignored when updating group members'
        }
        if ($PSCmdlet.ShouldProcess($document.Id, 'Update group members')) {
            $url = GetApiUrl 'grouper' 'invoke'
            if ($Force) {
                $url = AddUrlParameter $url 'ignoreChangelimit' 'true'
            }
            $result = ApiPostDocument $url $document
            # For some reason the foreach loop iterates once even if the array is empty
            if ($result.Length -gt 0) {
                foreach ($item in $result) {
                    $argsList = @(
                        $item.logTime
                        $item.documentId
                        $item.groupId
                        $item.groupDisplayName
                        $item.groupStore
                        $item.operation
                        $item.targetId
                        $item.targetDisplayName
                    )
                    New-Object -TypeName 'GrouperLib.Core.OperationalLogItem' -ArgumentList $argsList
                }
            }
        }
    }
}

Export-ModuleMember -Function 'Invoke-Grouper'
