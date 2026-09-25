# 구조와 확장 지점

## 읽기 경로

`MainForm`이 `QuestionBookCatalog`를 통해 문제집을 불러옵니다. `ExcelQuestionBookReader`는 Excel 데이터를 읽고 `Question`을 생성하면서 필수값을 검증합니다. 원본 Excel은 저장하거나 수정하지 않습니다.

현재는 폴더의 모든 문제집을 메모리에 올려 분류 및 개수를 표시합니다. 대형 문제집이 많아지면 파일별 메타데이터 캐시와 선택 시 지연 로딩을 추가할 수 있습니다. 로딩은 백그라운드 작업으로 실행합니다.

## 출제와 채점

`QuizService.CreateSession(books, count, allowedQuestionIds)`가 여러 문제집에서 후보를 모으고 Question ID 기준으로 중복을 제거합니다. ID가 같으면서 내용이 다르면 오류로 처리합니다. Fisher-Yates 방식으로 문제와 각 문제의 선지 배열을 각각 섞습니다.

`Question.Options`는 원래 ID와 내용의 묶음입니다. 섞은 복사본은 `QuizItem.DisplayOptions`에 둡니다. 화면의 1·2·3·4는 표시용 번호이고, RadioButton의 `Tag`에 담긴 AS ID로 답안을 제출합니다.

```csharp
var selectedId = radioButton.Tag as string;
var attempt = await session.SubmitAsync(selectedId!);
// 실제 판정은 SelectedAnswerId == CorrectAnswerId입니다.
```

세션 상태는 다음 규칙을 지킵니다.

- 답안 미제출 상태에서는 다음 문제로 이동할 수 없습니다.
- 제출은 한 문제당 한 번만 성공할 수 있습니다.
- 저장 성공 뒤에만 답안 수와 채점 결과를 확정합니다.
- 저장 실패 시 동일 Attempt ID, 동일 선택 ID로 재시도합니다.
- 정답 표시 후 `MoveNext()`로 이동합니다. 이때 선지는 다시 섞지 않습니다.

## 기록의 기준

`AnswerAttempt`가 이력의 기본 단위입니다. `SchemaVersion = 1`로 파일 형식을 구분하고, 지원하지 않는 스키마는 읽기를 중단합니다. 과거 이력을 덮어쓰거나 누적 카운터를 직접 증가시키는 방식 대신, 확정된 개별 풀이를 추가합니다.

`ContentHash`와 당시 문제/선지 스냅샷을 보관하므로 고정 Question ID의 내용을 수정해도 이전 채점이 소급 변경되지 않습니다. 집계는 Question ID 기준이며, 현재 버전만 집계하려면 ContentHash 필터를 추가하면 됩니다.

정답·오답 횟수는 `HistoryQueries.Summarize()`가 계산합니다. `LatestWrongIds()`는 가장 최근 제출이 오답인 문제만 반환합니다. 한 번이라도 틀린 문제를 모두 모으는 기능은 `WrongCount > 0`으로 별도 구현할 수 있습니다.

## 다음 화면 연결 예시

다중 선택:

```csharp
var books = selectedBooks.ToArray();
var total = books.SelectMany(b => b.Questions)
    .Select(q => q.Id).Distinct().Count();
var session = quizService.CreateSession(books, requestedCount);
```

오답만 출제:

```csharp
var attempts = await historyStore.ReadAsync();
var wrongIds = HistoryQueries.LatestWrongIds(attempts);
var eligibleCount = selectedBooks.SelectMany(b => b.Questions)
    .Where(q => wrongIds.Contains(q.Id))
    .Select(q => q.Id).Distinct().Count();
// 0문제일 때는 화면에 안내하고 시작을 막습니다.
var session = quizService.CreateSession(selectedBooks, requestedCount, wrongIds);
```

오답노트 메모·즐겨찾기는 `QuestionId`를 키로 하는 별도의 JSON 문서/모델에 두세요. 원본 문제나 과거 AnswerAttempt를 수정해서 메모를 넣지 않습니다.

## 범위와 운영상 한계

- 하나의 로컬 사용자 기록을 사용합니다. 계정, 클라우드 동기화, 프로필 전환은 없습니다.
- 현재 JSON 파일 전체를 읽고 다시 저장합니다. 개인용 MVP에 적합하며 이력이 매우 커지면 SQLite 같은 저장소로 바꿀 수 있습니다. `IHistoryStore` 구현체를 교체하면 됩니다.
- 제출 시 저장 실패를 보여주고 종료 전 확인하지만, 강제 종료·전원 차단 상황까지 저장 성공을 보장하지는 않습니다.
- WinForms UI 실행·DPI·스크린리더 동작은 Windows에서 별도로 확인해야 합니다.
