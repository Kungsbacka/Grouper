$thumbprint = Read-Host -Prompt 'Certificate thumbprint'
$serviceAccount = Read-Host -Prompt 'Service account (DOMAIN\User)'
$cert = Get-ChildItem -Path "Cert:\LocalMachine\My\$thumbprint" -ErrorAction SilentlyContinue
if (-not $cert) {
    Write-Error -Message "Certificate with thumbprint '$thumbprint' not found in Cert:\LocalMachine\My"
    return
}
$rsa = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($cert)
$fileName = $rsa.key.UniqueName
$keyPath = "$env:SystemDrive\Users\All Users\Application Data\Microsoft\Crypto\RSA\MachineKeys\$fileName"
$acl = Get-Acl -Path $keyPath
$rule = New-Object 'System.Security.AccessControl.FileSystemAccessRule' -ArgumentList @($serviceAccount, 'Read', 'Allow')
$acl.AddAccessRule($rule)
Set-Acl -Path $keyPath -AclObject $acl
