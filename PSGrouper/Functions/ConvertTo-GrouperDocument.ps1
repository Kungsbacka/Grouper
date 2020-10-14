<#
    .SYNOPSIS
        Converts JSON to a Grouper document

    .DESCRIPTION
        Parses a JSON string into an intemediate document format, validates
        the document and if it passes validation, it outputs a Grouper document.
    .PARAMETER InputObject
        JSON string

    .INPUTS
        (see InputObject)

    .OUTPUTS
        Grouper document

    .EXAMPLE
        Get-Content .\MyGroup.json -Encoding UTF8 | ConvertTo-GrouperDocument
#>
function ConvertTo-GrouperDocument
{
    param (
        [Parameter(Mandatory=$true,Position=0,ValueFromPipeline=$true)]
        [AllowNull()]
        [string]
        $InputObject
    )
    begin {
        $sb = [System.Text.StringBuilder]::new()
    }

    process {
        $null = $sb.AppendLine($InputObject)
    }

    end {
        $document = [GrouperLib.Core.GrouperDocument]::FromJson($sb.ToString())
        if ($null -eq $document) {
            throw 'Invalid document. Use Test-GrouperDocument with -OutputErrors for more information.'
            return
        }
        Write-Output -InputObject $document
    }
}

Export-ModuleMember -Function 'ConvertTo-GrouperDocument'
