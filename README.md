# 찌리리공 렌즈 (Voltorb Lens)

한국어판 하트골드의 찌리리공 뒤집기 보드와 힌트를 화면에서 읽어 확률을 표시하는 Windows 보조 프로그램입니다. 보드를 자동으로 찾아 인식하며, 화면 크기를 바꾸면 다시 탐색합니다. 실제 게임에서 열린 숫자 카드와 행·열 힌트를 사용합니다.

## 사용

GitHub Releases에서 최신 `VoltorbLens-<버전>.zip`을 내려받아 압축을 풀고, 최상위 `찌리리공 렌즈.exe`를 실행합니다. 에뮬레이터 창을 선택하고 **계산 시작**을 누르면 됩니다. 새 라운드 자동 인식은 기본으로 켜져 있습니다. 자세한 설명은 배포본의 `사용법.txt`에 있습니다.

## 개발

Windows 10/11 x64와 .NET 8 SDK가 필요합니다.

```powershell
dotnet build src/VoltorbLens.csproj -c Release -p:UseSharedCompilation=false
Start-Process src/bin/Release/net8.0-windows/VoltorbLens.exe -ArgumentList '--self-test' -Wait
Get-Content src/bin/Release/net8.0-windows/test-results.txt
```

버전별 독립 실행 배포본과 ZIP은 다음처럼 생성합니다.

```powershell
& .\tools\package-release.ps1 -Version '2026.10.16'
```

## 제한 사항

프로그램은 보드 화면만 읽으며 ROM, 저장 파일, 게임 메모리에는 접근하지 않습니다. 보드 생성 확률의 게임별 사전분포를 적용하지 않고, 화면 조건에 맞는 각 보드를 같은 비중으로 계산합니다. 추정 확률의 오차 정보는 프로그램 화면을 참고하세요. 실제 게임 화면의 인식은 에뮬레이터와 화면 구성에 따라 달라질 수 있습니다.

에뮬레이터와 게임 ROM은 포함하지 않습니다. 게임 메모 기능을 끄고 사용하세요.

## 라이선스와 출처

프로젝트 소스는 `LICENSE`의 GPL-3.0-or-later를 따릅니다. 템플릿 출처와 관련 고지는 `THIRD_PARTY.md`를 참고하세요.
