<#
    .SYNOPSIS
        Converts a Grouper document to JSON

    .DESCRIPTION
        Converts a Grouper document to a JSON string

    .PARAMETER InputObject
        Grouper document or database document entry

    .PARAMETER Compact
        Outputs compacted JSON (no line breaks or indentation)

    .INPUTS
        (see InputObject)

    .OUTPUTS
        JSON string

    .EXAMPLE
        Get-GrouperDocumentEntry -GroupName 'MyGroup' | ConvertFrom-GrouperDocument
#>
function ConvertFrom-GrouperDocument
{
    param (
        [Parameter(Mandatory=$true,Position=0,ValueFromPipeline=$true)]
        [object]
        $InputObject,
        [Parameter(Mandatory=$false)]
        [switch]
        $Compact
    )
    process {
        $document = GetDocumentFromInputObject $InputObject
        if ($null -eq $document) {
            return
        }
        if ($Compact) {
            $formatting = 'None'
        }
        else {
            $formatting = 'Indent'
        }
        $document.ToJson($formatting)
    }
}

Export-ModuleMember -Function 'ConvertFrom-GrouperDocument'
