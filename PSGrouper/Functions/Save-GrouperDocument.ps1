<#
    .SYNOPSIS
        Saves a Grouper document do the database

    .DESCRIPTION
        Saves a Grouper document to the Grouper database. If a document already exists
        with the same ID, that document is updated. If not, a new document entry is
        created in the database.

    .PARAMETER InputObject
        Grouper document entry, Grouper document

    .PARAMETER Publish
        Publish document after it is saved

    .INPUTS
        (see InputObject)

    .EXAMPLE
        Get-GrouperDocumentEntry -GroupName 'MyGroup' | Edit-GrouperDocument | Save-GrouperDocument -Publish
#>
function Save-GrouperDocument
{
    [CmdletBinding(SupportsShouldProcess=$true)]
    param (
        [Parameter(Mandatory=$true,Position=0,ValueFromPipeline=$true)]
        [object]
        $InputObject,
        [Parameter(Mandatory=$false)]
        [switch]
        $Publish
    )

    begin {
        if (-not (CheckApi)) {
            break
        }
    }

    process {
        $document = GetDocumentFromInputObject $InputObject
        if (-not $document) {
            return
        }
        if ($Publish) {
            if ($PSCmdlet.ShouldProcess($document.Id, 'Save & Publish')) {
                ApiInvokeWebRequest (GetApiUrl 'document') 'Post' $document
                ApiInvokeWebRequest (GetApiUrl 'document' "publish/$($document.Id)") 'Post'
            }
        }
        else {
            if ($PSCmdlet.ShouldProcess($document.Id, 'Save')) {
                ApiInvokeWebRequest (GetApiUrl 'document') 'Post' $document
                Write-Warning -Message 'Document is not published and may not be processed by the scheduled task. Use Publish-GrouperDocument to publish.'
            }
        }
    }
}

Export-ModuleMember -Function 'Save-GrouperDocument'
