<#
    .SYNOPSIS
        Tests if a JSON string is a valid Grouper document

    .DESCRIPTION
        Takes a JSON string an tries to deserialize it as a Grouper document. Use -OutputErrors
        to see any error from the validator.

    .PARAMETER InputObject
        JSON string

    .PARAMETER OutputErrors
        Outputs validation error to the pipeline

    .INPUTS
        (see InputObject)

    .OUTPUTS
        Boolean or ValidationErrors

    .EXAMPLE
        Get-Content .\MyGroup.json -Encoding UTF8 | Test-GrouperDocument -OutputErrors
#>
function Test-GrouperDocument
{
    param (
        [Parameter(Mandatory=$true,Position=0,ValueFromPipeline=$true)]
        [AllowNull()]
        [string[]]
        $InputObject,
        [Parameter(Mandatory=$false)]
        [switch]
        $OutputErrors
    )
    begin {
        if (-not (CheckApi)) {
            break
        }
        $sb = [System.Text.StringBuilder]::new()
    }

    process {
        foreach ($str in $InputObject) {
            $null = $sb.AppendLine($str)
        }
    }

    end {
        $errors = ApiPostDocument (GetApiUrl 'document' 'validate') $sb.ToString()
        if ($OutputErrors) {
            $errors.errorText
        }
        else
        {
            $errors.Count -eq 0
        }
    }
}

Export-ModuleMember -Function 'Test-GrouperDocument'
