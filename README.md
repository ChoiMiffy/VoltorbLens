# 찌리리공 렌즈 (Voltorb Lens)

한국어판 하트골드의 찌리리공 뒤집기 보드와 힌트를 화면에서 읽어 확률을 표시하는 Windows 보조 프로그램입니다. 보드를 자동으로 찾아 인식하며, 화면 크기를 바꾸면 다시 탐색합니다. 실제 게임에서 열린 숫자 카드와 행·열 힌트를 사용합니다.

OBS에서 확률을 보이게 하려면 프로그램의 **OBS 캡처에 확률 표시**를 켜고, OBS 윈도우 캡처에서 `VoltorbLens.exe: VoltorbLens OBS Feed`를 선택하세요. 이 전용 창은 마젠타 배경으로 확률만 출력합니다. OBS의 효과 필터에서 **크로마 키**를 추가하고 마젠타색을 제거한 뒤, 이 소스를 게임 화면 위에 배치하세요. 기본 설정은 켜짐입니다.

방송 표시 모드에서는 확률을 계속 보여 주면서 에뮬레이터 창을 직접 읽습니다. 인식이 되지 않으면 이 옵션을 끄세요.
옵션을 끄거나 계산 결과가 없을 때 OBS 피드는 숨겨지지 않고 마젠타 화면으로 비워집니다. OBS가 마지막 프레임을 남기지 않도록 하기 위한 동작입니다.

## 다운로드 및 사용

실행 파일은 소스 저장소에 넣지 않습니다. 저장소를 막 게시했다면 Releases에 배포본이 아직 없을 수 있습니다. 배포된 버전이 있으면 GitHub 페이지의 **Releases**에서 최신 `VoltorbLens-<버전>.zip`을 내려받아 압축을 풀고, 최상위 `찌리리공 렌즈.exe`를 실행하세요. 에뮬레이터 창을 선택하고 **계산 시작**을 누르면 됩니다. 새 라운드 자동 인식은 기본으로 켜져 있습니다. 사용 설명은 배포 ZIP의 `사용법.txt`에 있습니다.

첫 실행 배포 ZIP은 Windows에서 다음 명령으로 만들 수 있습니다. 생성된 `releases/VoltorbLens-<버전>.zip`을 GitHub 저장소의 **Releases → Draft a new release**에 첨부해 게시하세요.

```powershell
& .\tools\package-release.ps1 -Version '2026.10.16'
```

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
