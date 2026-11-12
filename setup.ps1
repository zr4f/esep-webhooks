# Run from inside your cloned esep-webhooks folder, with Function.cs next to this script:
#   powershell -ExecutionPolicy Bypass -File .\setup.ps1

# One time per machine: Lambda templates and the deploy tool
dotnet new install Amazon.Lambda.Templates
dotnet tool install -g Amazon.Lambda.Tools
if ($LASTEXITCODE -ne 0) { dotnet tool update -g Amazon.Lambda.Tools }

# Generate the function project
dotnet new lambda.EmptyFunction --name EsepWebhook

# Find the folder with EsepWebhook.csproj (new template: EsepWebhook, old: EsepWebhook\src\EsepWebhook)
$proj = Get-ChildItem -Recurse -Filter "EsepWebhook.csproj" | Select-Object -First 1
if (-not $proj) { Write-Error "EsepWebhook.csproj not found. Did dotnet new fail?"; exit 1 }
$projDir = $proj.DirectoryName

# Add Newtonsoft.Json and swap in the finished handler
Push-Location $projDir
dotnet add package Newtonsoft.Json
Copy-Item "$PSScriptRoot\Function.cs" ".\Function.cs" -Force
dotnet build
Pop-Location

# Remove the loose copy at the repo root so only the project copy is committed
if ($projDir -ne $PSScriptRoot) { Remove-Item "$PSScriptRoot\Function.cs" }

Write-Host ""
Write-Host "Project ready in: $projDir"
Write-Host "Next: cd there and run  dotnet lambda deploy-function EsepWebhook"