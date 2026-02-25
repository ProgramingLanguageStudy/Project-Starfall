# QuestSaveData & InventorySaveData 설계

## 1. 전체 구조

```
SaveData
├── squad       (기존)
├── flags       (기존, 퀘스트는 QuestCompleted만)
├── quests      (신규) QuestSaveData
└── inventory   (신규) InventorySaveData
```

---

## 2. QuestSaveData

### 2.1 저장 대상
- **진행 중인 퀘스트만** (수락했지만 아직 제출 전)
- 완료된 퀘스트는 `QuestCompleted` 플래그로만 저장 (플래그에서 제외)

### 2.2 데이터 구조

```csharp
[System.Serializable]
public class QuestSaveData
{
    public List<QuestProgressEntry> entries = new List<QuestProgressEntry>();
}

[System.Serializable]
public class QuestProgressEntry
{
    public string questId;
    public string targetId;      // QuestData.TargetId (아이템/몬스터 ID)
    public int currentAmount;     // 현재 진행도
}
```

### 2.3 Gather (저장 시)
- `QuestSystem.GetActiveQuests()` 순회
- 각 `QuestModel` → `QuestProgressEntry` 변환
- **수집형(Gather)**: currentAmount=0 저장 (인벤토리가 진실의 원천)
- **처치/방문형(Kill/Visit)**: currentAmount 저장

### 2.4 Apply (로드 시)
1. `Resources.Load<QuestData>($"Quests/{questId}")` 로 QuestData 로드
2. `QuestSystem.AcceptQuest(questData)` 호출
3. 진행도: **수집형** → `Inventory.GetTotalCount(targetId)`, **처치/방문형** → `entry.currentAmount`
4. `QuestSystem.SetTaskProgress(questId, targetId, amount)` 로 진행도 복원
5. 각 퀘스트에 대해 `FlagSystem.SetFlag(QuestAccepted, 1)`, 목표 달성 시 `QuestObjectivesDone` 설정 (DialogueSelector 호환)

**주의**: Apply 순서상 Inventory가 Quest보다 먼저 실행되어야 함 (SaveOrder 2, 3).

### 2.5 플래그 정책
- **저장**: QuestAccepted, QuestObjectivesDone → **제외** (QuestSaveData에서 유도)
- **저장**: QuestCompleted → **포함** (완료된 퀘스트는 목록에 없으므로)
- **로드**: QuestSaveContributor가 복원 후 플래그 동기화

---

## 3. InventorySaveData

### 3.1 저장 대상
- 슬롯별 `itemId` + `count`
- 빈 슬롯은 저장하지 않거나, 전체 슬롯을 고정 길이로 저장

### 3.2 데이터 구조

```csharp
[System.Serializable]
public class InventorySaveData
{
    public List<InventorySlotEntry> slots = new List<InventorySlotEntry>();
}

[System.Serializable]
public class InventorySlotEntry
{
    public int index;        // 슬롯 인덱스 (0~N)
    public string itemId;    // 비어 있으면 "" 또는 null
    public int count;
}
```

**대안 (간단)**: 슬롯 순서대로 저장, 빈 슬롯은 `itemId=""`, `count=0`

### 3.3 Gather (저장 시)
- `Inventory.GetSlots()` 순회
- `Item != null` 인 슬롯만 또는 전체 슬롯을 `InventorySlotEntry`로 변환

### 3.4 Apply (로드 시)
1. `Inventory.Initialize()` 또는 슬롯 클리어 후
2. 각 `InventorySlotEntry`에 대해:
   - `itemId`가 비어 있으면 빈 슬롯
   - 아니면 `Resources.Load<ItemData>($"Items/{itemId}")` 로 ItemData 로드
   - `Inventory.AddItem(itemData, count)` 또는 슬롯 직접 설정

**주의**: `AddItem`은 쌓기 로직이 있어서, 슬롯 순서를 유지하려면 Inventory에 `LoadFromSave(InventorySaveData)` 같은 전용 API가 필요할 수 있음.

### 3.5 ItemData 로드 경로
- `Resources/Items/{itemId}` 또는 `Resources/Items/{itemId}.asset`
- 프로젝트에 `Resources/Items` 폴더와 ItemData 에셋 필요

---

## 4. Contributor 순서 (SaveOrder)

| 순서 | Contributor        | 이유 |
|------|--------------------|------|
| 0    | SquadSaveContributor | 분대 먼저 |
| 1    | FlagSaveContributor  | 플래그 (QuestCompleted 포함) |
| 2    | InventorySaveContributor | 인벤토리 (수집형 퀘스트 진행도 참조용으로 Quest보다 먼저) |
| 3    | QuestSaveContributor | 퀘스트 (인벤토리 이후, 수집형은 인벤토리에서 진행도 참조) |

---

## 5. FlagSaveContributor 수정

- **Gather 시**: `QuestAccepted`, `QuestObjectivesDone` 패턴 키 제외
- 구현: `GetAllForSave()` 결과에서 `quest_*_accepted`, `quest_*_objectives_done` 필터링

---

## 6. 의존 관계

```
로드 시:
  FlagSaveContributor → QuestCompleted 등 복원
  QuestSaveContributor → QuestSystem 복원 + QuestAccepted/ObjectivesDone 플래그 설정
  InventorySaveContributor → Inventory 복원

퀘스트 진행도 ↔ 인벤토리:
  - 로드 후 QuestController.HandleQuestUpdated에서 인벤토리 개수로 SetTaskProgress 호출 (기존 로직)
  - 인벤토리 먼저 로드하면 퀘스트 복원 시 진행도가 인벤토리와 맞음
```

---

## 7. 체크리스트

- [x] QuestSaveData, QuestProgressEntry 클래스 생성
- [x] InventorySaveData, InventorySlotEntry 클래스 생성
- [x] SaveData에 quests, inventory 필드 추가
- [x] QuestSaveContributor: Gather 구현, Apply를 목록 기반으로 변경
- [x] InventorySaveContributor 신규 생성
- [x] FlagSaveContributor: Gather 시 퀘스트 진행 플래그 제외
- [x] Inventory에 LoadFromSave API 추가
- [x] DataManager에 GetItemData 추가, Resources/Items에서 로드
- [x] Resources/Items/Mushroom_Heal.asset (item_mushroom) 추가

## 8. 에디터 설정

### Contributors 등록
1. **SaveCoordinator** 오브젝트 선택
2. **PlaySaveCoordinator**의 **Contributors**에 Squad, Flag, Quest, Inventory Contributor 추가
3. **의존성 주입**: PlayScene.Awake에서 `_saveCoordinator.Initialize(squad, flagManager, questPresenter, inventory)` 호출로 자동 주입됨. 별도 인스펙터 할당 불필요.
4. 씬 저장
