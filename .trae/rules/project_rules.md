## 프로젝트 규칙 (Cursor → Trae 이관)

이 파일은 `.cursor/rules/*.mdc`의 핵심 규칙을 이 환경에서 참고하기 위해 옮겨 적은 것입니다.

### 1) Null 처리
- `?.`로 조용히 무시하기보다, null이면 `Debug.LogError(...)` 후 `return` 한다.

### 2) Model 책임
- Model은 상태와 상태 전환 규칙(메서드)을 함께 가진다.
- Presenter/System이 Model 필드를 직접 변경하지 않고, Model의 public API로만 상태를 바꾼다.

### 3) C# #region 순서 (쓸 때만)
- Nested Types → Constants → SerializeField → Fields/Internal State → Public API → Unity Lifecycle → Private Helpers
- 짧은 파일은 region 없이 유지해도 된다.

### 4) Git 커밋 규칙
- 논리적으로 다른 변경은 커밋을 분리한다.
- 커밋 메시지는 “요약 1줄 + 빈줄 + 본문”을 권장한다.
- 한글 커밋 메시지는 PowerShell 인코딩 이슈가 있으니 `git commit -F msg.txt`(UTF-8) 방식을 사용한다.

### 5) 날짜
- “오늘/현재 날짜”가 필요한 요청은 컨텍스트가 아니라 시스템 날짜를 기준으로 한다.

### 6) 스크립트 주석/인코딩
- C# 주석은 한글로 작성한다.
- 파일 인코딩은 UTF-8(65001)을 유지한다.

### 7) 개발일지
- 개발일지 작성 요청이 오면 커밋 로그 기반으로 작성한다(대화 컨텍스트 의존 X).
- 날짜는 시스템에서 다시 조회해 파일명/본문 날짜를 맞춘다.

