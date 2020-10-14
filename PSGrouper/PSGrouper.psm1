# Hide progress bar for Invoke-WebRequest
$ProgressPreference = 'SilentlyContinue'

$functions = Get-ChildItem -Path "$PSScriptRoot\Functions\*.ps1" | Select-Object -ExpandProperty FullName
foreach ($func in $functions) {
    . $func
}
