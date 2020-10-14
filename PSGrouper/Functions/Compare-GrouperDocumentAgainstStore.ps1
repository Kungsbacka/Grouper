<#
    .SYNOPSIS
        Takes a grouper document and checks if the group exists in the store
        and if the group names match.

    .DESCRIPTION
        Takes a grouper document and checks if the group exists in the store
        and if the group name in the document match the name in the store.
        The name comparison is case sensitive by default. Use CaseInsensitive
        switch to change behavoiur.

    .PARAMETER InputObject
        Grouper document, Grouper document inside metadata object, [System.Guid] or
        a GUID string.

    .PARAMETER CaseInsensitive
        Make a case insensitve group name comparison

    .PARAMETER OutputAll
        Output all entries even when the group exist and the name match

    .INPUTS
        (see InputObject)

    .OUTPUTS
        Custom object with information about if the group from the store.

    .EXAMPLE
        Get-GrouperDocument -All | Compare-GrouperDocumentAgainstStore

        Check all published documents in database
#>
function Compare-GrouperDocumentAgainstStore
{
    param (
        [Parameter(Mandatory=$true,Position=0,ValueFromPipeline=$true)]
        [object]
        $InputObject,
        [Parameter(Mandatory=$false)]
        [switch]
        $CaseInsensitive,
        [Parameter(Mandatory=$false)]
        [switch]
        $OutputAll
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
        $output = [pscustomobject]@{
            Document = $document
            NameInDocument =  $document.GroupName
            NameInStore = $null
            GroupExists = $true
            NamesMatch = $false
        }
        $groupInfo = ApiPostDocument (GetApiUrl 'groupinfo') $document
        if ($null -eq $groupInfo) {
            $output.GroupExists = $false
            $output
            return
        }
        $output.NameInStore = $groupInfo.DisplayName
        if ($CaseInsensitive) {
            $output.NamesMatch = $groupInfo.DisplayName -eq $document.GroupName
        }
        else {
            $output.NamesMatch = $groupInfo.DisplayName -ceq $document.GroupName
        }
        if ($OutputAll -or -not $output.GroupExists -or -not $output.NamesMatch) {
            $output
        }
    }
}

Export-ModuleMember -Function 'Compare-GrouperDocumentAgainstStore'
