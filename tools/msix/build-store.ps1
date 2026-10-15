[CmdletBinding()]
param([string]$DotNet = 'dotnet')

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$config = Join-Path $project 'packaging\windows\msix\window-manager.json'
$settings = Get-Content -LiteralPath $config -Raw | ConvertFrom-Json
$payload = [IO.Path]::GetFullPath((Join-Path (Split-Path $config -Parent) $settings.payloadPath))

# Versao Store independente: o instalador do site continua na 0.3.2.
& $DotNet publish (Join-Path $project 'FirawynixWindowManager.csproj') -c Release -r win-x64 --self-contained true `
    '-p:PublishSingleFile=true' '-p:IncludeNativeLibrariesForSelfExtract=false' `
    '-p:DebugType=None' "-p:Version=$($settings.version)" "-p:FileVersion=$($settings.version).0" -o $payload
if ($LASTEXITCODE -ne 0) { throw 'Falha ao publicar o executavel da Store.' }

# Sem assinatura local. Nao instala, nao consulta certificados e nao publica.
& (Join-Path $PSScriptRoot 'build-msix.ps1') -Config $config -Channel Store `
    -Publisher 'CN=1FDE3668-C222-4506-AFE6-E2E425EAECD8'
