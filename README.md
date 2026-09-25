# Mogimogi · 엑셀 문제은행 WinForms MVP

C# + .NET 10 WinForms + ClosedXML 0.105.0 + System.Text.Json으로 만든 학습용 데스크톱 프로그램입니다.

## 빠른 실행

1. 이 저장소를 clone하거나 Code → Download ZIP으로 내려받아 Windows PC의 쓰기 가능한 폴더에 풀어주세요. ZIP 내부에서 바로 실행하지 마세요.
2. [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)의 Windows 버전을 설치하세요.
3. `run.cmd`를 실행하세요. 처음 실행할 때 NuGet 패키지를 받으므로 인터넷 연결이 필요합니다.
4. 분류와 문제집을 고르고, 풀 문제 수를 입력한 뒤 `풀이 시작`을 누르세요.
5. 선지를 선택하고 `답안 제출`을 누르면 채점 및 저장 후 다음 문제로 이동할 수 있습니다.

개발 환경은 Windows용 .NET 10 SDK와 VS Code 또는 .NET 데스크톱 개발 환경을 갖춘 Visual Studio를 사용하면 됩니다. Visual Studio에서는 `QuestionBank.sln`을 열고 `QuestionBank.WinForms`를 시작 프로젝트로 설정하세요. macOS/Linux에서는 Core·Infrastructure 검증 프로그램을 실행할 수 있지만 WinForms 화면은 Windows에서 실행해야 합니다.

명령어로 실행하려면 프로젝트 루트에서:

```powershell
dotnet run --project src/QuestionBank.WinForms/QuestionBank.WinForms.csproj -c Release
```

## 이번 버전에 들어 있는 기능

- 실행 파일 옆 `Data` 폴더의 `.xlsx` 파일 탐색. Excel 임시 파일 `~$...xlsx` 제외.
- 파일명의 첫 `_` 앞부분을 분류로 사용. `_`가 없으면 `미분류`.
- 분류 필터, 문제집 하나 선택, 전체 문제 수 표시, 출제 수 입력 및 전체 선택.
- 지정한 문제 수만큼 Question ID 기준 중복 없이 랜덤 출제.
- 매 풀이 시작마다 각 문제의 선지 순서를 독립적으로 섞음.
- AS ID로 채점, 정답 표시, 제출 시마다 JSON 저장.
- 풀이 종료 후 제출 수, 정답·오답 수, 정답률과 각 문제의 내 답·정답 표시.
- 문제집 새로고침, 데이터 폴더·기록 폴더 열기.
- 잘못된 엑셀 행 안내, 손상 파일 격리, 저장 재시도, JSON 백업.

무작위로 섞은 결과가 우연히 이전 순서와 같을 수 있습니다. 한 세션 안에서는 선지 순서가 바뀌지 않으며, 다른 세션에서는 같은 문제가 다시 나올 수 있습니다.

## 프로젝트 구조

| 경로 | 역할 |
|---|---|
| `src/QuestionBank.Core/Models` | 문제, 선지, 문제집, 풀이 이력 모델 |
| `src/QuestionBank.Core/Abstractions` | 엑셀 리더·기록 저장소 인터페이스 |
| `src/QuestionBank.Core/Services` | 랜덤 출제, 세션 진행, ID 채점, 기록 집계 |
| `src/QuestionBank.Infrastructure` | ClosedXML 읽기, 문제집 탐색, JSON 파일 저장 |
| `src/QuestionBank.WinForms/Forms` | 시작·풀이·결과 화면과 공통 UI |
| `tests/QuestionBank.Checks` | 핵심 동작을 실제 파일과 함께 검증하는 실행형 테스트 |
| `Data` | 동작 확인용 샘플 문제집 3개, 총 20문제 |
| `docs/ARCHITECTURE.md` | 설계 규칙과 다음 기능 확장 방법 |
| `docs/VALIDATION.md` | 검증 결과 및 Windows 수동 확인 항목 |

의존성은 WinForms → Core/Infrastructure, Infrastructure → Core입니다. Core에는 WinForms나 ClosedXML 참조가 없습니다. 화면은 코드로 작성했으며 `.Designer.cs` 기반 디자이너 파일은 포함하지 않았습니다.

## 엑셀 작성 규칙

문제 파일의 '첫 번째 워크시트'만 읽습니다. 첫 번째 비어 있지 않은 행이 아래 제목과 일치하면 헤더로 처리하며, 헤더 없이 첫 행부터 문제를 넣어도 됩니다.

| A: 문제 | B: 선지1 | C: 선지2 | D: 선지3 | E: 선지4 | F: 정답ID |
|---|---|---|---|---|---|
| 「学校」의 읽는 법은? | がっこう | がこう | がっこ | かっこう | AS1 |

- B열은 `AS1`, C열은 `AS2`, D열은 `AS3`, E열은 `AS4`입니다.
- F열에는 화면 번호가 아닌 `AS1`, `AS2`, `AS3`, `AS4` 중 하나를 입력하세요. 소문자는 대문자로 정규화합니다.
- 문제와 선지 4개, 정답 ID는 필수입니다. 완전히 빈 행은 무시합니다.
- 문제·선지의 셀 안 줄바꿈을 지원합니다. 이미지, 병합 셀로 나눈 지문, 수식 문제 데이터는 이번 버전에서 지원하지 않습니다.
- Excel에서 B-E열의 선지 내용을 옮길 때는 F열의 정답 ID도 새 열 위치에 맞게 바꾸세요. 화면에서만 섞을 때는 자동으로 ID를 유지합니다.
- 오류가 있는 파일은 일부 문제만 가져오지 않고 해당 문제집 전체를 제외합니다. `불러오기 오류`에서 파일명과 행 번호를 확인하세요.
- 동일 내용의 중복 행은 오류로 처리합니다. 의도적으로 별도 문제로 관리하려면 각 행에 서로 다른 고정 ID를 넣으세요.

샘플은 앱 동작 확인을 위해 작성한 예제이며 공식 JLPT 기출 문제가 아닙니다.

## Question ID: 6열 유지와 선택 7열

기본 6열만 사용하면 `문제 + AS ID별 선지 내용 + 정답 ID`를 정규화해 SHA-256 기반 `AUTO-...` ID를 만듭니다. 파일 이름이나 행 순서를 바꿔도 ID가 유지됩니다. 내용이 같은 문제는 다른 문제집에서도 같은 ID로 간주하며 풀이 기록을 공유합니다.

'문제 문구·선지·정답 ID를 수정하면 자동 ID가 바뀝니다.' 수정한 문제에 이전 기록을 이어 붙여야 한다면 처음부터 선택 7열을 사용하는 편이 좋습니다.

| G: QuestionID |
|---|
| VOCAB-001 |
| VOCAB-002 |
| READING-001 |

- 헤더를 넣었다면 G1은 `QuestionID`로 작성하세요. G열이 비어 있는 행만 자동 ID를 사용합니다.
- 모든 문제집에 걸쳐 고유하고 변하지 않는 ID를 사용하세요. ID는 대소문자를 구분합니다.
- 고정 ID가 같고 현재 문제 내용이 다른 파일들이 함께 있으면 충돌한 문제집을 모두 제외합니다.
- 이미 자동 ID로 풀이를 시작한 문제를 7열 방식으로 전환할 때는 JSON의 기존 `questionId`를 그대로 G열에 넣으면 기록을 유지할 수 있습니다. 별도의 자동 마이그레이션 도구는 아직 없습니다.
- 완전히 다른 문제로 교체할 때는 새로운 ID를 부여하세요.

## 문제와 사용자 기록 분리

| 데이터 | 위치 | 동작 |
|---|---|---|
| 문제은행 | 실행 파일 옆 `Data/*.xlsx` | 읽기 전용으로 사용 |
| 사용자 이력 | `%LOCALAPPDATA%/QuestionBank/history.json` | 답안 제출마다 저장 |
| 직전 이력 백업 | 같은 경로의 `history.json.bak` | 두 번째 저장부터 직전 버전 보관 |

개발 시 루트 `Data`가 빌드 출력의 `Data`로 복사됩니다. `Data 폴더 열기` 버튼은 현재 실행 중인 앱의 폴더를 엽니다. 개발 과정에서 계속 보관할 문제 파일은 루트 `Data`에도 넣으세요. 실행 도중 엑셀을 수정했다면 저장한 뒤 시작 화면에서 `새로고침`을 누르세요. 이미 진행 중인 세션에는 변경 내용을 적용하지 않습니다.

JSON에는 개별 풀이의 ID, Question ID, 문제집, 풀이 시각, 당시 문제 내용·선지 표시 순서·선택 ID·정답 ID를 저장합니다. 정답·오답 횟수는 `HistoryQueries.Summarize()`로 집계하므로 이력과 누적 수치가 서로 어긋나지 않습니다. 여러 실행 폴더에서 실행해도 같은 Windows 사용자라면 이 기록 파일을 공유합니다.

저장은 같은 폴더의 임시 파일에 완료한 다음 기존 파일을 교체합니다. 파일 잠금으로 동시 쓰기를 막고, `AttemptId`로 같은 답안의 재시도를 중복 기록하지 않습니다. 저장이 실패하면 다음 문제로 넘어가지 않고 `저장 재시도`를 보여줍니다.

손상된 JSON은 자동 초기화하지 않습니다. 앱을 모두 닫고 `history.json`을 별도 복사한 뒤, `history.json.bak`가 정상인지 확인하여 복원하세요. `.bak`는 직전 버전이므로 가장 최근 풀이가 빠질 수 있습니다. 첫 저장 이전에는 백업이 없습니다.

풀이를 중단해도 이미 제출한 답안은 남습니다. 미제출 문제는 오답으로 기록하지 않습니다. 중단한 세션의 이어 풀기는 아직 구현하지 않았습니다.

## 검증 및 배포

```powershell
dotnet build QuestionBank.sln -c Release
dotnet run --project tests/QuestionBank.Checks/QuestionBank.Checks.csproj -c Release
```

테스트는 별도의 테스트 프레임워크 없이 실행되는 콘솔 프로그램입니다. 실패 시 비정상 종료하고, 성공하면 `PASS: 17 checks`를 출력합니다. `dotnet test` 대신 위 명령 또는 `check.cmd`를 사용하세요.

`publish.cmd`는 .NET 런타임을 포함하는 Windows x64 배포본을 `publish/win-x64`에 만듭니다. 생성된 `QuestionBank.WinForms.exe`와 DLL, Data를 포함한 폴더 전체를 전달하세요. Windows ARM64 배포는 명령어의 `win-x64`를 `win-arm64`로 바꾸면 됩니다. 이 프로젝트는 설치 프로그램이나 자동 업데이트를 포함하지 않습니다.

## GitHub 자동 빌드

`main`에 커밋을 올리면 `Windows build and checks`가 Windows에서 빌드 및 17개 검증을 실행합니다. 성공한 실행의 Artifacts에서 `Mogimogi-win-x64`를 내려받아 압축을 풀면 `QuestionBank.WinForms.exe`로 실행할 수 있습니다. 이 배포본은 런타임을 포함하며, Data와 DLL 파일을 함께 보관해야 합니다. Artifacts는 14일간 보관하도록 설정했습니다. 화면 조작 자체를 자동 검증하는 워크플로는 아닙니다.

## 다음 구현 순서

1. 시작 화면의 문제집 목록을 다중 선택으로 변경하고 고유 Question ID 기준 총 문제 수 표시.
2. `HistoryQueries.LatestWrongIds()`를 연결해 마지막 풀이가 오답인 문제만 출제.
3. 누적 정답·오답 수, 정답률, 날짜별 풀이 수를 보여주는 통계 화면.
4. 필요하면 오답노트 메모, 즐겨찾기, 중단한 세션 이어 풀기.

## 참고 문서

확인 날짜: 2026-09-26.

- [Microsoft: WinForms 앱 만들기](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/get-started/create-app-visual-studio)
- [Microsoft: .NET 설치](https://learn.microsoft.com/en-us/dotnet/core/install/windows)
- [ClosedXML 0.105.0 릴리스](https://github.com/ClosedXML/ClosedXML/releases/tag/0.105.0) — 이 프로젝트에서 고정한 패키지 버전의 설명.
