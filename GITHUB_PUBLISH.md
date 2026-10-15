# GitHub에 게시하기

이 폴더는 개인 대화 기록과 사용자 스크린샷을 뺀 공개용 소스 저장소다.

1. GitHub Desktop을 설치하고 GitHub 계정으로 로그인한다.
2. `File → Add Local Repository`를 누르고 이 `GitHub` 폴더를 선택한다. 저장소로 인식되지 않으면 `Create a repository`를 선택하고 Local path를 이 폴더로 설정한다.
3. 변경 파일을 확인하고 커밋한다. 첫 커밋 메시지 예: `Initial public source release`.
4. `Publish repository`를 눌러 올린다. 공개할 준비가 되었을 때 Public을 선택한다. 확신이 없으면 Private으로 먼저 만든다.
5. GitHub 웹 저장소의 `Releases → Draft a new release`에서 태그(예: `v1.0.0`)를 만들고 설명을 쓴다.
6. `tools/package-release.ps1`로 만든 `releases/VoltorbLens-버전.zip`을 Release asset으로 첨부하고 Publish release를 누른다.

다음 수정은 GitHub Desktop에서 commit 후 Push origin으로 올린다. 새 실행 파일은 소스 저장소에 덮어쓰지 않고 새 버전 Release에 ZIP asset으로 첨부한다.

GitHub Desktop 안내: https://docs.github.com/en/desktop/adding-and-cloning-repositories/adding-an-existing-project-to-github-using-github-desktop
Release 안내: https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository
