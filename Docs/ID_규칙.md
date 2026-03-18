# ID 규칙

세이브·DataManager·플래그·대화 등에서 사용하는 식별자 규칙. 접두사로 타입을 구분해 오타·중복을 줄이고, 마이그레이션 시 일관성을 유지한다.

---

## 1. 접두사별 규칙

| 타입 | 접두사 | 예시 | 사용처 |
|-----|--------|------|--------|
| **Character** | `character_` | `character_celeste`, `character_pico`, `character_sapphi` | SquadSaveData, CharacterMemberData, DataManager |
| **Quest** | `quest_` | `quest_pico_recruit`, `quest_mushroom_gather`, `quest_wolf_kill_5` | QuestData.Id, QuestSaveData, GameStateKeys |
| **NPC** | `npc_` | `npc_pico`, `npc_sapphi`, `npc_chief` | DialogueData.npcId, Npc 컴포넌트, GameStateKeys.FirstTalkNpc |
| **Item** | `item_` | `item_mushroom`, `item_apple`, `item_sprint_potion` | ItemData.Id, InventorySaveData, QuestData.TargetId |
| **Enemy** | (접두사 없음 또는 `enemy_`) | `Wolf`, `enemy_wolf` | EnemyData.enemyId, QuestData.TargetId(처치형) |
| **Portal** | (자유 형식) | `Portal_Village_01` | PortalData.portalId, GameStateKeys.PortalUnlocked |

---

## 2. 데이터별 필드

### CharacterData
- **Id** (`_id`): `character_xxx` 형식. 세이브·DataManager·RM 프리팹 경로에 사용. **프리팹 파일명과 일치** (Character/character_celeste.prefab).

### QuestData
- **Id** (`_id`): `quest_xxx` 형식.
- **TargetId**: 목표 ID. 수집형=item_xxx, 처치형=enemyId.
- **recruitCharacterId** (RecruitmentQuestData): `character_xxx`. 영입 대상 캐릭터.

### DialogueData
- **npcId**: `npc_xxx`. 이 대화를 쓰는 NPC.
- **questId**: `quest_xxx`. 퀘스트 대화일 때만.

### ItemData
- **Id** (`_id`): `item_xxx` 형식. 인벤토리·퀘스트 목표·RM 프리팹 경로에 사용. **프리팹 파일명과 일치** (Item/item_mushroom.prefab).

### Npc (씬 오브젝트)
- **_npcId**: `npc_xxx`. 씬의 NPC 컴포넌트에 설정.

### EnemyData
- **enemyId**: 퀘스트 TargetId와 매칭. 처치 퀘스트 진행용.

---

## 3. 플래그 키 (GameStateKeys)

플래그 키는 ID 기반으로 생성. `GameStateKeys.XXX(id)` 사용 권장.

| 메서드 | 형식 | 예시 |
|--------|------|------|
| `FirstTalkNpc(npcId)` | `first_talk_` + npcId | `first_talk_npc_chief` |
| `Affection(npcId)` | `affection_` + npcId | `affection_npc_pico` |
| `QuestAccepted(questId)` | questId + `_accepted` (quest_ 없으면 접두사 추가) | `quest_mushroom_gather_accepted` |
| `QuestObjectivesDone(questId)` | questId + `_objectives_done` | `quest_mushroom_gather_objectives_done` |
| `QuestCompleted(questId)` | questId + `_completed` | `quest_mushroom_gather_completed` |
| `PortalUnlocked(portalId)` | `portal_unlocked_` + portalId | `portal_unlocked_Portal_Village_01` |

**주의**: questId에 `quest_` 접두사가 있으면 그대로 사용, 없으면 자동 추가. 중복 방지.

---

## 4. 세이브 데이터와의 연관

| SaveData 필드 | 저장되는 ID | 규칙 |
|---------------|-------------|------|
| SquadSaveData.currentPlayerId | 조종 캐릭터 | `character_xxx` |
| CharacterMemberData.id | 분대원 | `character_xxx` |
| QuestProgressEntry.id | 퀘스트 ID | `quest_xxx` |
| QuestProgressEntry.targetId | 목표 ID | `item_xxx` 또는 enemyId |
| InventorySlotEntry.id | 아이템 ID | `item_xxx` |
| FlagSaveData | 플래그 키 전체 | GameStateKeys 형식 |

**세이브 호환**: ID 접두사 변경 시 기존 세이브와 불일치할 수 있음. 마이그레이션 필요 시 SaveManager 등에서 처리.

---

## 5. Id = 프리팹 파일명 (RM 경로)

Character·Item은 **Id와 프리팹 파일명을 일치**시킨다. RM이 `Category/Id.prefab`으로 로드.

| Id | RM 경로 |
|----|---------|
| character_celeste | Character/character_celeste.prefab |
| item_mushroom | Item/item_mushroom.prefab |

---

## 6. 새 ID 추가 시 체크리스트

1. **Character**: `character_이름` → CharacterData._id, **프리팹 파일명도 character_이름.prefab**
2. **Quest**: `quest_이름` → QuestData._id, TargetId, recruitCharacterId(영입형)
3. **NPC**: `npc_이름` → DialogueData.npcId, Play 씬 Npc._npcId
4. **Item**: `item_이름` → ItemData._id, **프리팹 파일명도 item_이름.prefab**
5. **Enemy**: enemyId → EnemyData, 처치 퀘스트 TargetId와 일치
6. **플래그**: GameStateKeys 메서드 사용, 직접 문자열 하드코딩 지양

---

## 7. 참고 파일

- `Assets/02_Scripts/Flag/GameStateKeys.cs` — 플래그 키 생성
- `Assets/02_Scripts/Manager/DataManager.cs` — `Category/Id` 캐시
- `Assets/02_Scripts/Save/*.cs` — 세이브 구조
