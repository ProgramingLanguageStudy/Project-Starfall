# 오늘 작업 커밋 계획

> 파트별 커밋 규칙에 따라 논리적 단위로 분리. 한글 메시지는 `msg.txt`에 UTF-8로 저장 후 `git commit -F msg.txt` 사용.

---

## 고민했던 핵심 포인트 (컨텍스트 보존용)

1. **대화 완료 vs 퀘스트 완료 분리**: 대화 종료는 DialogueEnded, 퀘스트 완료는 QuestCompleted로 별도 이벤트
2. **Controller가 Registry에 등록하지 않음**: Controller는 라우터(발행자), Handler만 등록
3. **QuestController + QuestPresenter**: Accept/Complete 모두 Presenter에 Request 패턴으로 위임 (Dialogue와 동일)
4. **CompleteQuest → OnQuestCompleted → InvokeAll**: 퀘스트 완료 처리(플래그·인벤토리)는 핸들러들이 담당
5. **플래그 핸들러 분리**: DialogueFlagHandler / QuestCompletedFlagHandler — 이벤트 타입별로 분리 유지
6. **QuestInventoryHandler vs Inventory 직접 구현**: Handler 분리로 도메인 결합 방지 (인벤토리 참조 필요하지만 구조상 Handler 유지)

---

## 커밋 순서 제안

### 1. PlaySceneServices + Registry 기반 이벤트 구조
**파일**: PlaySceneServices, PlaySceneEventHub, DialogueEndedRegistry, IDialogueEndedHandler, QuestCompletedRegistry, IQuestCompletedHandler

```
PlaySceneServices 및 Registry 기반 이벤트 구조 도입

- PlaySceneServices: DialogueEnded, QuestCompleted Registry 노출
- IDialogueEndedHandler, IQuestCompletedHandler 인터페이스
- DialogueEndedRegistry, QuestCompletedRegistry
- PlaySceneEventHub (NPC 상호작용 이벤트)
```

### 2. 대화 완료 리팩터링
**파일**: DialogueController, DialogueFlagHandler, DialogueSelector, DialogueData, DialogueModel, DialoguePresenter, DialogueSystem, NpcDialogueTrigger(삭제), DialogueDataLoader(삭제)

```
대화 완료 흐름 리팩터링

- DialogueController: Presenter.OnDialogueEnded 구독 → InvokeAll 라우팅
- DialogueFlagHandler: IDialogueEndedHandler 구현, flagsToModify 처리
- DialogueSelector: NPC별 대화 선택
- NpcDialogueTrigger, DialogueDataLoader 제거 (플래그 기반 구조로 전환)
```

### 3. 퀘스트 완료 이벤트 + QuestController
**파일**: QuestController, QuestPresenter, QuestSystem, QuestCompletedFlagHandler, QuestInventoryHandler, QuestFlagSync

```
퀘스트 완료 이벤트 구조 및 QuestController 도입

- QuestController: IDialogueEndedHandler, OnQuestCompleted 구독 → InvokeAll
- QuestPresenter: RequestAcceptQuest, RequestCompleteQuest
- QuestSystem.CompleteQuest → OnQuestCompleted 발행
- QuestCompletedFlagHandler, QuestInventoryHandler (IQuestCompletedHandler)
- QuestFlagSync: 목표 달성 시 QuestObjectivesDone 플래그
```

### 4. PlayScene 연결 및 씬
**파일**: PlayScene, Play.unity

```
PlayScene에 DialogueController, QuestController 연결
```

### 5. 기타 (이번 대화와 무관할 수 있음)
- CSVReader, GameServices 삭제
- Dialogue 에셋 삭제/변경
- Save, Manager, Flag 등 다른 수정

→ 위 1~4와 의존성 없으면 별도 커밋으로 분리

---

## 실행 예시

```powershell
# 1번 커밋
git add Assets/02_Scripts/PlayScene/PlaySceneServices.cs Assets/02_Scripts/PlayScene/PlaySceneServices.cs.meta
git add Assets/02_Scripts/PlayScene/PlaySceneEventHub.cs Assets/02_Scripts/PlayScene/PlaySceneEventHub.cs.meta
git add Assets/02_Scripts/Dialogue/DialogueEndedRegistry.cs Assets/02_Scripts/Dialogue/IDialogueEndedHandler.cs
git add Assets/02_Scripts/Quest/QuestCompletedRegistry.cs Assets/02_Scripts/Quest/IQuestCompletedHandler.cs
# (+ 각 .meta)
# msg.txt 작성 후
git commit -F msg.txt
```
