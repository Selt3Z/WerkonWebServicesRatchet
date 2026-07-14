# Step 1 of first-time setup: teach Windows that ratchet.local is this computer.
# Run once as Administrator (right-click PowerShell → Run as administrator):
#
#   cd C:\Users\Selt3Z\source\repos\WerkonWebServicesRatchet
#   powershell -ExecutionPolicy Bypass -File docker\setup-hosts.ps1
#
# Then open in browser: http://ratchet.local

$HostName = "ratchet.local"
$HostsPath = "$env:SystemRoot\System32\drivers\etc\hosts"
$Entry = "127.0.0.1`t$HostName"

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host ""
    Write-Host "ERROR: run PowerShell as Administrator, then run this script again."
    Write-Host ""
    exit 1
}

$content = Get-Content $HostsPath -Raw

# Remove old hostname if present
$content = $content -replace "(?m)^127\.0\.0\.1\s+wwsratchet\.local\s*$", ""
$content = $content -replace "(?m)^127\.0\.0\.1\s+ratchet\.wws\s*$", ""
$content = $content -replace "(?m)^127\.0\.0\.1\s+ratchet\.local\s*$", ""
Set-Content -Path $HostsPath -Value $content.TrimEnd() -NoNewline

$check = Get-Content $HostsPath -Raw
if ($check -notmatch [regex]::Escape($HostName)) {
    Add-Content -Path $HostsPath -Value "`n$Entry"
}

Write-Host ""
Write-Host "OK. Windows now knows: $HostName -> this PC"
Write-Host ""
Write-Host "Next: start Docker stack (docker compose up -d --build from repo root)"
Write-Host "Then open: http://$HostName"
Write-Host ""
