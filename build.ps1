param(
    [ValidateSet("Dev", "Plugin")]
    [string] $Target = "Dev"
)

$ErrorActionPreference = "Stop"

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$gameDir = Resolve-Path (Join-Path $projectDir "..\..")
$outputDir = if ($Target -eq "Plugin") {
    Join-Path $gameDir "BepInEx\plugins\AbilitiesUnlocked"
} else {
    Join-Path $gameDir "BepInEx\scripts"
}

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

dotnet build "$projectDir\AbilitiesUnlocked.csproj" `
    --configuration Release `
    --nologo `
    -p:OutDir="$outputDir\"

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

$depsFile = Join-Path $outputDir "AbilitiesUnlocked.deps.json"
if (Test-Path -LiteralPath $depsFile) {
    Remove-Item -LiteralPath $depsFile -Force
}

Write-Host "Built $(Join-Path $outputDir 'AbilitiesUnlocked.dll')"
