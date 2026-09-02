#Requires -Version 5.1
param(
  [switch]$KeepLocalSettings
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$LocalSettingsPath = Join-Path $RepoRoot "backend\src\Flexis.Api\appsettings.Development.local.json"
$FrontendPort = 5173

Get-Process -Name "ngrok" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "Stopped ngrok processes (if any)."

if ($KeepLocalSettings) {
  Write-Host "Kept $LocalSettingsPath"
  exit 0
}

if (-not (Test-Path $LocalSettingsPath)) {
  Write-Host "No local settings file to restore."
  exit 0
}

$config = Get-Content -Path $LocalSettingsPath -Raw | ConvertFrom-Json
$localOrigins = @(
  "http://localhost:$FrontendPort",
  "http://127.0.0.1:$FrontendPort"
)

if ($config.Frontend -and $config.Frontend.Origins) {
  $kept = @($config.Frontend.Origins | Where-Object {
    $origin = [string]$_
    ($localOrigins -contains $origin) -or ($origin -notmatch "ngrok")
  })
  foreach ($origin in $localOrigins) {
    if ($kept -notcontains $origin) {
      $kept += $origin
    }
  }
  $config.Frontend.Origins = $kept
}

if ($config.Google) {
  $config.Google | Add-Member -NotePropertyName "RedirectUri" -NotePropertyValue "http://localhost:5080/api/google/connections/callback" -Force
}

if ($config.Microsoft) {
  $config.Microsoft | Add-Member -NotePropertyName "RedirectUri" -NotePropertyValue "http://localhost:5080/api/mail-check/mailbox/outlook/callback" -Force
}

$config | ConvertTo-Json -Depth 20 | Set-Content -Path $LocalSettingsPath -Encoding UTF8
Write-Host "Restored localhost OAuth redirects in:"
Write-Host "  $LocalSettingsPath"
Write-Host "Restart backend\run.bat to apply."
