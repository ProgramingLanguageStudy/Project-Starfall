# 대화·퀘스트·플래그 아키텍처 설계

> 목표: 대화와 퀘스트를 **플래그 기반**으로 연결하되, 각 시스템은 독립적으로 동작. 퀘스트 대화만 조율층에서 QuestSystem과 연결.

---

## 1. 설계 원칙

- **FlagManager**: 게임 전반의 모든 플래그(퀘스트 수락/완료, 첫 대화, 호감도 등)를 일괄 관리.
- **DialogueData**: 대화 선택 조건(플래그)과 대화 종료 시 효과(플래그 변경)를 **데이터로** 보유.
- **조율층**: 대화 종료 시 퀘스트 대화면 QuestSystem 호출. Dialogue·Quest는 서로를 직접 모름.

---

## 2. FlagManager 역할

### 2.1 담당 범위

- 퀘스트 관련: `quest_{questId}_수락`, `quest_{questId}_완료`
- 게임 흐름 관련: `first_talk_{npcId}`, `affection_{npcId}` 등
- **모든** 게임 상태 플래그를 한 곳에서 관리

### 2.2 퀘스트 vs 플래그

| 구분 | FlagManager | QuestSystem |
|------|-------------|-------------|
| 역할 | **기록**: "이 이벤트가 일어났는가" | **런타임**: "지금 이 퀘스트 진행 상태는?" |
| 예시 | 수락함, 제출함 (영구 기록) | 수락함, 목표 달성함 (현재 세션) |
| 용도 | 대화 분기, 세이브/로드 | UI 표시, 제출 가능 여부 판단 |

---

## 3. DialogueData 구조 (제안)

### 3.1 대화 선택 조건 (플래그 기반)

```
requiredFlagsOn  : 이 플래그들이 1(켜짐)이어야 이 대화 선택 가능
requiredFlagsOff : 이 플래그들이 0(꺼짐)이어야 이 대화 선택 가능
```

- NPC 상호작용 시, 해당 NPC의 DialogueData 후보들을 FlagManager와 대조.
- 조건을 만족하는 첫 번째(또는 우선순위에 따른) 대화를 재생.

### 3.2 대화 종료 시 효과

```
flagsToSet   : 대화가 끝나면 이 플래그들을 1로 설정
flagsToClear : 대화가 끝나면 이 플래그들을 0으로 설정 (선택)
```

- 조건/효과를 모두 DialogueData에 두면 시나리오 설계자가 코드 없이 흐름 구성 가능.

### 3.3 퀘스트 연동 (추가 필드)

```
questId : 비어 있지 않으면 "퀘스트 관련 대화". 수락/완료 시 QuestSystem 연동 대상
isQuestComplete : true면 제출(완료) 대화, false면 수락 대화 (선택)
```

- `questId`가 있으면 → 이 대화는 퀘스트 수락 또는 퀘스트 완료 대화.
- 조율층이 대화 종료 시 `questId`를 확인해 QuestSystem.AcceptQuest / CompleteQuest 호출.

---

## 4. 전체 흐름

```
[플레이어가 NPC 상호작용]
        │
        ▼
[NPC의 DialogueData 후보들 중]
        │
        ├─ FlagManager로 requiredFlagsOn/Off 체크
        │
        ▼
[조건 만족하는 대화 1개 선택] → [DialogueSystem으로 재생]
        │
        ▼
[대화 종료 시]
        │
        ├─ flagsToSet / flagsToClear → FlagManager에 반영
        │
        └─ questId가 있으면
                ├─ 수락 대화 → QuestSystem.AcceptQuest(questData)
                └─ 완료 대화 → QuestSystem.CompleteQuest(questId) + 인벤토리 차감 등
```

---

## 5. 조율층(DialogueCoordinator 등) 역할

1. **대화 선택**: NPC별 DialogueData 후보 + FlagManager → 재생할 대화 결정.
2. **대화 종료 구독**: DialogueSystem.OnDialogueEnd 또는 유사 이벤트 구독.
3. **종료 시 처리**:
   - DialogueData의 flagsToSet/flagsToClear → FlagManager 반영
   - DialogueData.questId가 있으면 QuestSystem 호출
     - 수락: `AcceptQuest(QuestData)` — questId로 Resources 등에서 QuestData 로드
     - 완료: `CompleteQuest(questId)` + RequiresItemDeduction이면 Inventory.RemoveItem

---

## 6. 퀘스트 진행 (이벤트 기반)

- 퀘스트 **수락/완료**는 대화를 통해서만 조율층이 처리.
- 퀘스트 **진행도**(채집, 처치, 방문)는:
  - 각 시스템이 이벤트만 발행 (예: Inventory.OnItemChangedWithId, 적 처치 시 OnEnemyKilled)
  - 조율층이 구독 후 `QuestSystem.NotifyProgress(targetId)` 또는 `SetTaskProgress` 호출
  - QuestSystem은 이벤트를 모름. 조율층만 연결.

---

## 7. 요약

| 구성요소 | 역할 |
|----------|------|
| **FlagManager** | 모든 플래그 저장·조회. 대화 조건·효과, 세이브/로드 |
| **DialogueData** | 조건(requiredFlagsOn/Off), 효과(flagsToSet/Clear), 퀘스트 표시(questId) |
| **DialogueSystem** | 대화 재생만. 플래그·퀘스트를 모름 |
| **QuestSystem** | 퀘스트 수락/진행/완료 로직. Dialogue·Flag를 모름 |
| **조율층** | 대화 선택, 종료 시 플래그·퀘스트 처리, 이벤트→퀘스트 연결 |

---

*이 문서는 대화·퀘스트·플래그 통합 설계를 정리한 것입니다. 구현 시 참고용.*
