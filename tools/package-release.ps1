param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9][A-Za-z0-9._-]*$')][string]$Version,
    [string]$OutputRoot
)
$ErrorActionPreference='Stop'
$sourceRoot=Split-Path $PSScriptRoot -Parent
if(-not $OutputRoot){$OutputRoot=Join-Path (Split-Path $sourceRoot -Parent) '배포본'}
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
$name="VoltorbLens-$Version"
$releaseRoot=Join-Path $OutputRoot $name
$archivePath=Join-Path $OutputRoot "$name.zip"
if((Test-Path -LiteralPath $releaseRoot) -or (Test-Path -LiteralPath $archivePath)){throw '이 버전의 배포본이 이미 있습니다. 새 버전 이름을 사용하세요.'}
$runtimeRoot=Join-Path $releaseRoot '프로그램'
New-Item -ItemType Directory -Path $runtimeRoot -Force | Out-Null
dotnet publish (Join-Path $sourceRoot 'src/VoltorbLens.csproj') -c Release -r win-x64 --self-contained true -o $runtimeRoot -p:UseSharedCompilation=false -p:DebugType=None -p:DebugSymbols=false
if($LASTEXITCODE -ne 0){throw '배포 빌드 실패'}
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $compiler /nologo /target:winexe /reference:System.Windows.Forms.dll "/out:$releaseRoot/찌리리공 렌즈.exe" (Join-Path $PSScriptRoot 'Launcher.cs')
if($LASTEXITCODE -ne 0){throw '실행 런처 빌드 실패'}
Copy-Item (Join-Path $sourceRoot 'LICENSE'),(Join-Path $sourceRoot 'THIRD_PARTY.md') -Destination $runtimeRoot
@"
찌리리공 렌즈 — $Version

1. ZIP 전체를 쓰기 가능한 폴더에 압축 해제하세요.
2. 찌리리공 렌즈.exe를 더블클릭하세요.
3. melonDS 창 선택 → 계산 시작.

보드 위치는 자동으로 찾습니다. 새 라운드 자동 인식은 기본 ON입니다.
게임의 메모 기능은 끄고, 카드와 힌트 숫자가 모두 보이게 해 주세요.
자동 탐색이 어려우면 수동 영역을 사용할 수 있습니다.

Windows 10/11 x64용. 별도 .NET 설치는 필요 없습니다.
프로그램 폴더는 반드시 실행 파일과 함께 보관하세요.
개인 설정은 실행 후 프로그램/settings.json에 생성됩니다.
다른 버전과 독립된 폴더에 풀어서 사용하세요.
"@ | Set-Content -LiteralPath (Join-Path $releaseRoot '사용법.txt') -Encoding utf8
@{version=$Version;builtAt=[DateTimeOffset]::Now.ToString('o');runtime='win-x64';selfContained=$true} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $runtimeRoot 'version.json') -Encoding utf8
foreach($required in @('VoltorbLens.exe','VoltorbLens.dll','coreclr.dll','assets/tile_blank.png')){
    if(-not(Test-Path -LiteralPath (Join-Path $runtimeRoot $required))){throw "필수 파일 누락: $required"}
}
if(Test-Path -LiteralPath (Join-Path $runtimeRoot 'settings.json')){throw '개인 설정이 배포본에 포함되었습니다.'}
Compress-Archive -LiteralPath $releaseRoot -DestinationPath $archivePath -CompressionLevel Optimal
Write-Output "배포 폴더: $releaseRoot"
Write-Output "배포 ZIP: $archivePath"
