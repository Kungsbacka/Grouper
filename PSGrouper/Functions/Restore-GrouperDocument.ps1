<#
    .SYNOPSIS
        Restores a Grouper document

    .DESCRIPTION
        Sets a grouper document as 'not deleted' (and 'unpublished') in the Grouper database

    .PARAMETER InputObject
        Grouper document, Grouper document inside metadata object, [System.Guid] or
        a GUID string.

    .INPUTS
        (see InputObject)

    .EXAMPLE
        Get-GrouperDocumentEntry -GroupName 'MyGroup' -Store 'AzureAD' -IncludeDeleted | Restore-GrouperDocument
#>
function Restore-GrouperDocument
{
    [CmdletBinding(SupportsShouldProcess=$true)]
    param (
        [Parameter(Mandatory=$true,Position=0,ValueFromPipeline=$true)]
        [object]
        $InputObject
    )

    begin {
        if (-not (CheckApi)) {
            break
        }
    }

    process {
        $documentId = GetDocumentIdFromInputObject $InputObject
        if ($null -eq $documentId) {
            return
        }
        if ($PSCmdlet.ShouldProcess($documentId, 'Restore')) {
            ApiInvokeWebRequest (GetApiUrl 'document' "restore/$documentId") 'Post'
        }
    }
}

Export-ModuleMember -Function 'Restore-GrouperDocument'
