<#
    .SYNOPSIS
        Unpublishes a Grouper document

    .DESCRIPTION
        Sets a grouper document as 'unpublished' in the Grouper database

    .PARAMETER InputObject
        Grouper document, Grouper document inside metadata object, [System.Guid] or
        a GUID string.

    .INPUTS
        (see InputObject)

    .EXAMPLE
        Get-GrouperDocumentEntry -GroupName 'MyGroup' -Store 'AzureAD' | Unpublish-GrouperDocument
#>
function Unpublish-GrouperDocument
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
        if ($PSCmdlet.ShouldProcess($documentId, 'Unpublish')) {
            ApiInvokeWebRequest (GetApiUrl 'document' "unpublish/$documentId") 'Post'
        }
    }
}

Export-ModuleMember -Function 'Unpublish-GrouperDocument'
