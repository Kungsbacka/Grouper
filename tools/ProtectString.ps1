Add-Type -AssemblyName 'System.Security'
$secureString = Read-Host -Prompt "Enter unprotected string" -AsSecureString
$credential = New-Object -TypeName 'System.Management.Automation.PSCredential' -ArgumentList @('n/a', $secureString)
$unprotectedBytes = [System.Text.Encoding]::Unicode.GetBytes($credential.GetNetworkCredential().Password)
$protectedString = [Convert]::ToBase64String([System.Security.Cryptography.ProtectedData]::Protect($unprotectedBytes, $null, 'CurrentUser'))
$protectedString | Set-Clipboard
Remove-Variable secureString,credential,unprotectedBytes,protectedString
Write-Host 'Protected string is now in the clipboard.' -ForegroundColor Green
