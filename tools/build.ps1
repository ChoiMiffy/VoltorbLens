$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    dotnet build src/VoltorbLens.csproj -c Release -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    $process = Start-Process -FilePath './src/bin/Release/net8.0-windows/VoltorbLens.exe' -ArgumentList '--self-test' -PassThru -Wait -WindowStyle Hidden
    Get-Content './src/bin/Release/net8.0-windows/test-results.txt'
    if ($process.ExitCode -ne 0) { throw 'Tests failed' }
    dotnet publish src/VoltorbLens.csproj -c Release -r win-x64 --self-contained true -o dist/VoltorbLens -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
    Copy-Item README.md,THIRD_PARTY.md,LICENSE -Destination dist/VoltorbLens
    Copy-Item src/bin/Release/net8.0-windows/test-results.txt -Destination dist/VoltorbLens
} finally { Pop-Location }
