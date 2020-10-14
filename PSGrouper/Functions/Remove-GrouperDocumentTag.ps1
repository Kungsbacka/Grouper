<#
    .SYNOPSIS
        Removes an existing tag from a Grouper document

    .DESCRIPTION
        Remove an existing tag on a Grouper document. If the tag toes not exist,
        the request is ignored and no error is generated.

    .PARAMETER InputObject
        Grouper document, Grouper document inside metadata object, [System.Guid] or
        a GUID string.

    .PARAMETER Tag
        One or more tags that should be removed from the document.

    .INPUTS
        (see InputObject)

    .EXAMPLE
        Get-GrouperDocumentEntry -GroupName 'MyGroup' | % {Remove-GrouperDocumentTag -InputObject $_ -Tag $_.Tags}

        Remove all tags from a document
#>
function Remove-GrouperDocumentTag
{
    [CmdletBinding(SupportsShouldProcess=$true)]
    param (
        [Parameter(Mandatory=$true,Position=0,ValueFromPipeline=$true)]
        [object]
        $InputObject,
        [Parameter(Mandatory=$true,Position=1,ValueFromPipelineByPropertyName=$true)]
        [string]
        $Tag
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
        if ($PSCmdlet.ShouldProcess($documentId, 'Remove tag')) {
            $url = GetApiUrl 'document' "tag/$documentId"
            $url = AddUrlParameter $url 'tag' $Tag
            ApiInvokeWebRequest $url 'Delete'
        }
    }
}

Export-ModuleMember -Function 'Remove-GrouperDocumentTag'
