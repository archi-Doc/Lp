param(
    [string]$Configuration = 'Release',
    [string]$CoverageTool
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (-not $CoverageTool) {
    $packageRoot = Join-Path $env:USERPROFILE '.nuget/packages/dotnet-coverage'
    $CoverageTool = Get-ChildItem -LiteralPath $packageRoot -Recurse -Filter dotnet-coverage.dll |
        Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
}
if (-not $CoverageTool) {
    throw 'Supply -CoverageTool with the path to dotnet-coverage.dll.'
}

Push-Location $projectRoot
try {
    dotnet build xUnitTest/xUnitTest.csproj -c $Configuration --no-restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }

    $output = Join-Path $projectRoot 'artifacts/coverage.cobertura.xml'
    New-Item -ItemType Directory -Force (Split-Path $output -Parent) | Out-Null
    dotnet $CoverageTool collect -f cobertura -o $output dotnet "xUnitTest/bin/$Configuration/net10.0/xUnitTest.dll"
    if ($LASTEXITCODE -ne 0) { throw 'Coverage collection or tests failed.' }

    [xml]$coverage = Get-Content -Raw -LiteralPath $output
    $package = $coverage.coverage.packages.package | Where-Object name -eq 'Lp'
    $files = @{}
    foreach ($class in $package.classes.class) {
        if ($class.filename -match '[\\/](obj|Generated)[\\/]') { continue }
        if (-not $files.ContainsKey($class.filename)) { $files[$class.filename] = @{} }
        foreach ($line in $class.lines.line) {
            $files[$class.filename][$line.number] = [Math]::Max([int]$files[$class.filename][$line.number], [int]$line.hits)
        }
    }

    $rows = foreach ($entry in $files.GetEnumerator()) {
        $covered = @($entry.Value.Values | Where-Object { $_ -gt 0 }).Count
        [pscustomobject]@{
            File = [IO.Path]::GetRelativePath($projectRoot, $entry.Key)
            Covered = $covered
            Total = $entry.Value.Count
            Percent = [Math]::Round(100 * $covered / $entry.Value.Count, 2)
        }
    }
    $rows | Sort-Object File | Export-Csv artifacts/coverage-source.csv -NoTypeInformation
    $total = ($rows | Measure-Object Total -Sum).Sum
    $covered = ($rows | Measure-Object Covered -Sum).Sum
    Write-Output ('Lp source coverage: {0}/{1} lines ({2:P2}); generated code excluded.' -f $covered, $total, ($covered / $total))
    Write-Output "Report: $output"
}
finally {
    Pop-Location
}
