# DialogueData & 대화 시스템 사용

> **대화 시스템** = 받은 대화만 재생 + 다음/끝내기. 로딩·선택·NPC·버튼 종류는 모름.  
> **DialogueCoordinator** = OnNpcInteracted 구독 → DialogueSelector로 선택 → DialogueSystem에 직접 전달. 대화 종료 시 플래그·퀘스트 처리.

---

## 1. DialogueData 필드

| 필드 | 설명 |
|------|------|
| **npcId** | 이 대화를 쓰는 NPC ID. DataManager가 npcId로 그룹핑. |
| **speakerDisplayName** | 화자 표시명. 비어 있으면 npcId 사용. |
| **priority** | 선택 우선순위. 낮을수록 먼저 검사. 0=첫대화, 10=퀘스트제시, 15=진행중, 20=퀘스트완료, 30=일반 |
| **requiredFlagsOn** | 이 대화가 선택되려면 켜져 있어야 하는 플래그 (1) |
| **requiredFlagsOff** | 이 대화가 선택되려면 꺼져 있어야 하는 플래그 (0) |
| **flagsToModify** | 대화 종료 시 적용할 플래그 변경. Set=값 지정, Add=현재값+value (호감도 등) |
| **questId / questDialogueType** | 퀘스트 연동. None/Accept/InProgress/Complete |
| **lines** | 한 문장씩 순서대로 재생. |

---

## 2. 역할 분리

| 담당 | 역할 |
|------|------|
| **DialogueSystem** | `StartDialogue(data)` 로 받은 대화만 재생. Coordinator가 직접 호출. |
| **DialogueSelector** | DataManager + FlagManager로 npcId의 대화 후보 중 조건 만족하는 첫 대화 선택. Coordinator가 보유. |
| **DialogueCoordinator** | OnNpcInteracted 구독 → Selector.Select() → System.StartDialogue(). 대화 종료 시 플래그·퀘스트 처리. |
| **DataManager** | Resources/Dialogues 로드, npcId 기준 그룹핑. GetDialoguesForNpc(npcId) |

---

## 3. 흐름

```
[플레이어가 NPC 상호작용]
    → Npc.Interact() → PlaySceneEventHub.RaiseNpcInteracted(npcId)
    → DialogueCoordinator.HandleNpcInteracted(npcId)
    → DialogueSelector.Select(npcId) → DataManager + FlagManager
    → DialogueSystem.StartDialogue(selectedData)
```

---

## 4. 요약

| 하고 싶은 일 | 사용 방법 |
|-------------|-----------|
| 대화 재생 | Coordinator가 System.StartDialogue(data) 직접 호출 |
| 어떤 대화 쓸지 | DataManager에 DialogueData 로드 (npcId 설정). Selector가 자동 선택. |
| 새 대화 추가 | DialogueData 에셋 생성 후 Resources/Dialogues에 배치. npcId 설정. |

대화 시스템은 **내용만 재생**하고, **누가/언제/어떤 버튼**은 모두 밖에서 결정.
