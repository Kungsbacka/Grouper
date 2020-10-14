<#
    .SYNOPSIS
        Removes a Grouper document

    .DESCRIPTION
        Sets a grouper document as 'deleted' in the Grouper database

    .PARAMETER InputObject
        Grouper document, Grouper document inside metadata object, [System.Guid] or
        a GUID string.

    .INPUTS
        (see InputObject)

    .EXAMPLE
        Get-GrouperDocumentEntry -GroupName 'MyGroup' -Store 'AzureAD' | Remove-GrouperDocument
#>
function Remove-GrouperDocument
{
    [CmdletBinding(SupportsShouldProcess=$true)]
    param (
        [Parameter(Mandatory=$true,Position=0,ValueFromPipeline=$true)]
        [object]
        $InputObject,
        [Parameter(Mandatory=$false)]
        [switch]
        $PassThru
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
        if ($PSCmdlet.ShouldProcess($documentId, 'Remove')) {
            ApiInvokeWebRequest (GetApiUrl 'document' "id/$documentId") 'Delete'
        }
    }
}

Export-ModuleMember -Function 'Remove-GrouperDocument'
