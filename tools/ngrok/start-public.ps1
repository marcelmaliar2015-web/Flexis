#Requires -Version 5.1
param(
  [int]$FrontendPort = 5173,
  [int]$ApiPort = 5080,
  [int]$NgrokApiPort = 4040,
  [switch]$SkipHealthCheck
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$LocalSettingsPath = Join-Path $RepoRoot "backend\src\Flexis.Api\appsettings.Development.local.json"
$NgrokLogPath = Join-Path $env:TEMP "flexis-ngrok.log"

function Test-LocalPort([int]$Port) {
  return [bool](Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
}

function Get-NgrokHttpsUrl {
  try {
    $response = Invoke-RestMethod -Uri "http://127.0.0.1:$NgrokApiPort/api/tunnels" -TimeoutSec 2
  }
  catch {
    return $null
  }

  $https = $response.tunnels |
    Where-Object { $_.proto -eq "https" -and $_.public_url } |
    Select-Object -First 1
  if ($https) {
    return $https.public_url.TrimEnd("/")
  }

  $any = $response.tunnels |
    Where-Object { $_.public_url } |
    Select-Object -First 1
  if ($any) {
    return $any.public_url.TrimEnd("/")
  }

  return $null
}

function Merge-LocalSettings([string]$PublicUrl) {
  $googleCallback = "$PublicUrl/api/google/connections/callback"
  $microsoftCallback = "$PublicUrl/api/mail-check/mailbox/outlook/callback"
  $requiredOrigins = @(
    "http://localhost:$FrontendPort",
    "http://127.0.0.1:$FrontendPort",
    $PublicUrl
  )

  if (Test-Path $LocalSettingsPath) {
    $config = Get-Content -Path $LocalSettingsPath -Raw | ConvertFrom-Json
  }
  else {
    $config = [pscustomobject]@{}
  }

  if (-not $config.PSObject.Properties["Frontend"]) {
    $config | Add-Member -NotePropertyName "Frontend" -NotePropertyValue ([pscustomobject]@{ Origins = @() })
  }
  elseif (-not $config.Frontend.PSObject.Properties["Origins"]) {
    $config.Frontend | Add-Member -NotePropertyName "Origins" -NotePropertyValue @() -Force
  }

  $origins = @()
  if ($config.Frontend.Origins) {
    $origins = @($config.Frontend.Origins | ForEach-Object { [string]$_ })
  }
  foreach ($origin in $requiredOrigins) {
    if ($origins -notcontains $origin) {
      $origins += $origin
    }
  }
  $config.Frontend.Origins = $origins

  if (-not $config.PSObject.Properties["Google"]) {
    $config | Add-Member -NotePropertyName "Google" -NotePropertyValue ([pscustomobject]@{})
  }
  $config.Google | Add-Member -NotePropertyName "RedirectUri" -NotePropertyValue $googleCallback -Force

  if (-not $config.PSObject.Properties["Microsoft"]) {
    $config | Add-Member -NotePropertyName "Microsoft" -NotePropertyValue ([pscustomobject]@{})
  }
  $config.Microsoft | Add-Member -NotePropertyName "RedirectUri" -NotePropertyValue $microsoftCallback -Force

  $dir = Split-Path -Parent $LocalSettingsPath
  if (-not (Test-Path $dir)) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
  }
  $config | ConvertTo-Json -Depth 20 | Set-Content -Path $LocalSettingsPath -Encoding UTF8
}

function Ensure-Ngrok {
  $ngrok = Get-Command ngrok -ErrorAction SilentlyContinue
  if ($ngrok) {
    return $ngrok.Source
  }

  throw @"
ngrok was not found on PATH.

Install once:
  1. https://ngrok.com/download
  2. Sign up and run: ngrok config add-authtoken <token>
  3. Re-open this terminal and run run-public.bat again

Or: winget install ngrok.ngrok
"@
}

if (-not $SkipHealthCheck) {
  if (-not (Test-LocalPort $FrontendPort)) {
    Write-Host "Frontend is not listening on port $FrontendPort."
    Write-Host "Start frontend\run.bat first, then run this again."
    exit 1
  }
  if (-not (Test-LocalPort $ApiPort)) {
    Write-Host "API is not listening on port $ApiPort."
    Write-Host "Start backend\run.bat first, then run this again."
    exit 1
  }
}

$existingUrl = Get-NgrokHttpsUrl
if ($existingUrl) {
  Write-Host "Using existing ngrok tunnel: $existingUrl"
}
else {
  $ngrokPath = Ensure-Ngrok
  if (Test-Path $NgrokLogPath) {
    Remove-Item $NgrokLogPath -Force -ErrorAction SilentlyContinue
  }

  Write-Host "Starting ngrok tunnel to http://127.0.0.1:$FrontendPort ..."
  Start-Process -FilePath $ngrokPath `
    -ArgumentList @("http", "127.0.0.1:$FrontendPort", "--log=stdout", "--log-format=logfmt") `
    -RedirectStandardOutput $NgrokLogPath `
    -WindowStyle Hidden

  $publicUrl = $null
  for ($i = 0; $i -lt 40; $i++) {
    Start-Sleep -Milliseconds 500
    $publicUrl = Get-NgrokHttpsUrl
    if ($publicUrl) {
      break
    }
  }

  if (-not $publicUrl) {
    Write-Host "ngrok did not publish a public URL."
    if (Test-Path $NgrokLogPath) {
      Write-Host "Log: $NgrokLogPath"
      Get-Content $NgrokLogPath -Tail 20
    }
    exit 1
  }

  $existingUrl = $publicUrl
}

Merge-LocalSettings -PublicUrl $existingUrl

Write-Host ""
Write-Host "Public Flexis URL:"
Write-Host "  $existingUrl"
Write-Host ""
Write-Host "Wrote OAuth CORS overrides to:"
Write-Host "  $LocalSettingsPath"
Write-Host ""
Write-Host "Add these redirect URIs (keep localhost ones too):"
Write-Host "  Google:    $existingUrl/api/google/connections/callback"
Write-Host "  Microsoft: $existingUrl/api/mail-check/mailbox/outlook/callback"
Write-Host ""
Write-Host "Restart backend\run.bat so CORS Origins and OAuth RedirectUri reload."
Write-Host "Restart frontend\run.bat once if Vite was started before allowedHosts was added."
Write-Host ""
Write-Host "Ngrok inspector: http://127.0.0.1:$NgrokApiPort"
Write-Host "Stop tunnel:     stop-public.bat"
Write-Host ""
