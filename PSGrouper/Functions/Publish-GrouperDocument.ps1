<#
    .SYNOPSIS
        Publishes a Grouper document

    .DESCRIPTION
        Sets a grouper document as 'published' in the Grouper database

    .PARAMETER InputObject
        Grouper document, Grouper document inside metadata object, [System.Guid] or
        a GUID string.

    .INPUTS
        (see InputObject)

    .EXAMPLE
        Get-GrouperDocumentEntry -GroupName 'MyGroup' -Store 'AzureAD' -IncludeUnpublished | Publish-GrouperDocument
#>
function Publish-GrouperDocument
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
        if ($PSCmdlet.ShouldProcess($documentId, 'Publish')) {
            ApiInvokeWebRequest (GetApiUrl 'document' "publish/$documentId") 'Post'
        }
    }
}

Export-ModuleMember -Function 'Publish-GrouperDocument'
