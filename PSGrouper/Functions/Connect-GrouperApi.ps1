<#
    .SYNOPSIS

    .DESCRIPTION

    .PARAMETER Uri

    .INPUTS

    .EXAMPLE

    .NOTES
#>
function Connect-GrouperApi
{
    param (
        [Parameter(Mandatory=$true)]
        [Uri]
        $Uri
    )
    $url = $Uri.AbsoluteUri.TrimEnd('/')
    $response = Invoke-WebRequest -Uri "$url/test/version" -UseBasicParsing -UseDefaultCredentials
    if ($response.Content -lt '1.0') {
        throw 'Unable to find API version or version less than 1.0'
        return
    }
    $Script:ApiUrl = $url
}

Export-ModuleMember -Function 'Connect-GrouperApi'
