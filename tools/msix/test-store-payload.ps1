[CmdletBinding()]
param([string]$LegacyExe)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$config = Join-Path $project 'packaging\windows\msix\window-manager.json'
$settings = Get-Content -LiteralPath $config -Raw | ConvertFrom-Json
$payload = [IO.Path]::GetFullPath((Join-Path (Split-Path $config -Parent) $settings.payloadPath))
$out = Join-Path $project ('packaging\windows\msix\artifacts\regression-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $out
$required = @($settings.executable) + @($settings.requiredPayloadFiles)
$passed = @()

foreach ($missing in $settings.requiredPayloadFiles) {
    $fixture = Join-Path $out ('missing-' + $missing)
    $null = New-Item -ItemType Directory -Path $fixture
    foreach ($file in $required | Where-Object { $_ -cne $missing }) {
        Copy-Item -LiteralPath (Join-Path $payload $file) -Destination $fixture
    }
    $rejected = $false
    try {
        & (Join-Path $PSScriptRoot 'build-msix.ps1') -Config $config -Channel Store `
            -Publisher 'CN=1FDE3668-C222-4506-AFE6-E2E425EAECD8' -PayloadPath $fixture -OutputDirectory $out
    } catch {
        if ($_.Exception.Message -notlike "*Biblioteca nativa obrigatoria ausente*$missing*") { throw }
        $rejected = $true
    }
    if (-not $rejected) { throw "Build aceitou a ausencia de $missing" }
    $passed += "reject-missing-$missing"
}

if ($LegacyExe) {
    $fixture = Join-Path $out 'wrong-version'
    $null = New-Item -ItemType Directory -Path $fixture
    foreach ($file in $settings.requiredPayloadFiles) {
        Copy-Item -LiteralPath (Join-Path $payload $file) -Destination $fixture
    }
    Copy-Item -LiteralPath $LegacyExe -Destination (Join-Path $fixture $settings.executable)
    $rejected = $false
    try {
        & (Join-Path $PSScriptRoot 'build-msix.ps1') -Config $config -Channel Store `
            -Publisher 'CN=1FDE3668-C222-4506-AFE6-E2E425EAECD8' -PayloadPath $fixture -OutputDirectory $out
    } catch {
        if ($_.Exception.Message -notlike '*Versao do executavel*diferente do MSIX*') { throw }
        $rejected = $true
    }
    if (-not $rejected) { throw 'Build aceitou EXE com versao divergente.' }
    $passed += 'reject-version-mismatch'
}

$package = Join-Path (Split-Path $out -Parent) "Firaw-Organizador-de-Janelas-$($settings.version).0-x64-store.msix"
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($package)
try {
    $reader = [IO.StreamReader]::new($zip.GetEntry('AppxManifest.xml').Open())
    try { [xml]$manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
    if ($manifest.Package.Identity.Name -cne $settings.storeIdentityName -or
        $manifest.Package.Identity.Version -cne "$($settings.version).0" -or
        $manifest.Package.Identity.Publisher -cne 'CN=1FDE3668-C222-4506-AFE6-E2E425EAECD8' -or
        $manifest.Package.Identity.ProcessorArchitecture -cne 'x64' -or
        $manifest.Package.Applications.Application.Id -cne $settings.applicationId -or
        $manifest.Package.Applications.Application.Executable -cne "app\$($settings.executable)") {
        throw 'Manifesto divergente.'
    }
    foreach ($file in $required) {
        $entry = $zip.GetEntry("app/$file")
        if (-not $entry) { throw "Arquivo ausente no MSIX: $file" }
        $stream = $entry.Open()
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $hash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','') }
        finally { $stream.Dispose(); $sha.Dispose() }
        if ($hash -cne (Get-FileHash -LiteralPath (Join-Path $payload $file)).Hash) { throw "Bytes divergentes: $file" }
    }
    if ($zip.GetEntry('AppxSignature.p7x')) { throw 'Este ensaio espera pacote Store sem assinatura local.' }
    $passed += 'manifest-and-six-payload-hashes'
} finally { $zip.Dispose() }

$report = [pscustomobject]@{package=$package;sha256=(Get-FileHash -LiteralPath $package).Hash;passed=$passed;opened=$false;installed=$false}
$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'result.json') -Encoding UTF8
$report | ConvertTo-Json -Depth 5
