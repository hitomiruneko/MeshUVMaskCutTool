param(
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$pkgDir = Join-Path $repoRoot "Packages\io.github.hitomiruneko.mesh-uvmaskcuttool"
$pkgJsonPath = Join-Path $pkgDir "package.json"

if (-not (Test-Path $pkgJsonPath)) {
    throw "package.json not found: $pkgJsonPath"
}

$pkgMeta = (Get-Content $pkgJsonPath -Raw -Encoding UTF8) | ConvertFrom-Json
$version = $pkgMeta.version

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Split-Path $repoRoot -Parent
}
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
}

$outFile = Join-Path $OutputDir "MeshUVMaskCutTool-v$version.unitypackage"
if (Test-Path $outFile) {
    Remove-Item -Force $outFile
}

$stagingDir = Join-Path ([System.IO.Path]::GetTempPath()) ("mumct_upkg_" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $stagingDir | Out-Null
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

try {
    # Deterministic 32-hex GUID for root Assets/MeshUVMaskCutTool folder
    $md5 = [System.Security.Cryptography.MD5]::Create()
    $hashBytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes("Assets/MeshUVMaskCutTool|io.github.hitomiruneko.mesh-uvmaskcuttool"))
    $rootGuid = ([System.BitConverter]::ToString($hashBytes)).Replace("-", "").ToLowerInvariant()
    $md5.Dispose()

    $rootStage = Join-Path $stagingDir $rootGuid
    New-Item -ItemType Directory -Force -Path $rootStage | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $rootStage "pathname"), "Assets/MeshUVMaskCutTool", $utf8NoBom)
    $rootMeta = @"
fileFormatVersion: 2
guid: $rootGuid
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@ -replace "`r`n", "`n"
    [System.IO.File]::WriteAllText((Join-Path $rootStage "asset.meta"), $rootMeta, $utf8NoBom)

    $items = Get-ChildItem $pkgDir -Recurse | Where-Object {
        $_.Extension -ne ".meta" -and $_.Name -ne "package.json"
    }

    $pkgPrefixLen = $pkgDir.TrimEnd('\', '/').Length + 1
    foreach ($item in $items) {
        $metaPath = $item.FullName + ".meta"
        if (-not (Test-Path $metaPath)) {
            throw "Missing .meta file for: $($item.FullName)"
        }

        $metaText = [System.IO.File]::ReadAllText($metaPath, $utf8NoBom)
        if ($metaText -notmatch 'guid:\s*([0-9a-fA-F]{32})') {
            throw "Could not parse guid from: $metaPath"
        }
        $guid = $Matches[1].ToLowerInvariant()

        $relPath = $item.FullName.Substring($pkgPrefixLen).Replace('\', '/')
        $unityPath = "Assets/MeshUVMaskCutTool/$relPath"

        $itemStage = Join-Path $stagingDir $guid
        New-Item -ItemType Directory -Force -Path $itemStage | Out-Null
        [System.IO.File]::WriteAllText((Join-Path $itemStage "pathname"), $unityPath, $utf8NoBom)
        [System.IO.File]::Copy($metaPath, (Join-Path $itemStage "asset.meta"), $true)

        if (-not $item.PSIsContainer) {
            [System.IO.File]::Copy($item.FullName, (Join-Path $itemStage "asset"), $true)
        }
    }

    Push-Location $stagingDir
    try {
        $guidDirs = Get-ChildItem -Directory | Select-Object -ExpandProperty Name
        & tar --format ustar -czf $outFile @guidDirs
        if ($LASTEXITCODE -ne 0) {
            throw "tar exited with code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }

    Write-Output "Built: $outFile (Entries=$($guidDirs.Count))"
}
finally {
    if (Test-Path $stagingDir) {
        Remove-Item -Recurse -Force $stagingDir
    }
}
