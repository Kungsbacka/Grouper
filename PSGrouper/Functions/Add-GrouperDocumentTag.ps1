<#
    .SYNOPSIS
        Adds a new tag to an existing grouper document

    .DESCRIPTION
        Adds a new tag to an existing grouper document. The tag gets stored in
        the database and are retrieved as part of the database entry returned
        by Get-GrouperDocumentEntry. If the same tag is added more than once,
        the request will be ignored.

    .PARAMETER InputObject
        Grouper document, Grouper document inside metadata object, [System.Guid] or
        a GUID string.

    .PARAMETER Tag
        One or more tags that should be added to the document.

    .PARAMETER UseExisting
        Only add a tag that already exists on another document. This prevents new tags
        from being created. If a new tag is added that does not exist, an error is
        generated.

    .INPUTS
        (see InputObject)

    .EXAMPLE
        Get-GrouperDocumentEntry -GroupName 'MyGroup' | New-GrouperDocumentTag -Tag 'HR'
#>
function Add-GrouperDocumentTag
{
    [CmdletBinding(SupportsShouldProcess=$true)]
    param (
        [Parameter(Mandatory=$true,Position=0,ValueFromPipeline=$true)]
        [object]
        $InputObject,
        [Parameter(Mandatory=$true,Position=1,ValueFromPipelineByPropertyName=$true)]
        [string]
        $Tag,
        [Parameter(Mandatory=$false)]
        [switch]
        $UseExisting
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
        if ($PSCmdlet.ShouldProcess($documentId, 'Add tag')) {
            $url = GetApiUrl 'document' "tag/$documentId"
            $url = AddUrlParameter $url 'tag' $tag
            if ($UseExisting) {
                $url = AddUrlParameter $url 'useExisting' 'true'
            }
            ApiInvokeWebRequest $url 'Post'
        }
    }
}

Export-ModuleMember -Function 'Add-GrouperDocumentTag'
