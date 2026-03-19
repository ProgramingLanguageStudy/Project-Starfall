# SaveManager 단순화 계획

> 현재 구조가 복잡해 이해하기 어려우므로, 단계적으로 단순화하는 방향을 정리.

---

## 1. 현재 구조 (복잡한 이유)

### 1.1 레이어가 많음

```
SaveManager
    ├── Backend (Firestore / Local) 선택 로직
    ├── ISaveHandler (PlaySaveCoordinator, GlobalSaveCoordinator)
    │       └── ISaveContributor (Squad, Flag, Quest, Inventory, Currency...)
    └── Task ↔ Coroutine 브릿지
```

- **SaveManager**: 백엔드 선택 + Handler 등록 + Gather/Apply
- **ISaveHandler**: Coordinator가 여러 Contributor를 모음
- **ISaveContributor**: 각 도메인(Squad, Flag 등)이 Gather/Apply 구현

→ Handler → Contributor 2단계로 위임. 이해하려면 여러 파일을 봐야 함.

### 1.2 Task가 섞여 있음

- `LoadAsync()` → `Task<SaveData>` 반환
- `SaveAsync()` → `Task<bool>` 반환
- PlayScene: `yield return new WaitUntil(() => task.IsCompleted)` 로 Task → 코루틴 변환
- Quit 시: `QuitAfterSave(Task)` 코루틴으로 Task 대기

→ Unity 코루틴과 Task가 섞여서 흐름 추적이 어려움.

### 1.3 백엔드 선택 로직

- Boot 경유 여부, Firebase Auth 로그인 여부에 따라 Firestore vs 로컬 분기
- `_bootCompleted`, `_backendLogged` 등 static 플래그

→ "지금 어디서 저장되는지"가 직관적이지 않음.

---

## 2. 단순화 목표

```
[목표] 한 줄로 이해 가능
"SaveManager가 SaveData를 파일(또는 서버)에 쓰고 읽는다. 
각 시스템이 Gather/Apply로 참여한다."
```

### 2.1 단계별 제안

| 단계 | 내용 | 효과 |
|------|------|------|
| **1단계** | 로컬만 사용. Firestore 제거 | 백엔드 선택 로직 제거 |
| **2단계** | 로컬을 동기 I/O로 변경 | Task 제거, 코드 흐름 단순 |
| **3단계** | Load를 IEnumerator로 통일 | 코루틴만 사용, 일관성 |
| **4단계** | Handler/Contributor 구조 유지 or 단순화 | 이해 용이 |

---

## 3. 1단계: 로컬만 사용 (Firestore 제거)

**현재**: Boot + 로그인 → Firestore, 아니면 로컬  
**변경**: 항상 로컬 파일만 사용

- `SaveManager`에서 `Backend` 프로퍼티 제거
- `FirestoreSaveBackend`, `_bootCompleted`, `MarkBootCompleted` 제거
- `LocalSaveBackend`만 사용

**장점**: 백엔드 분기 로직 제거. "파일에 저장"만 남음.

**단점**: 나중에 Firestore 다시 넣으려면 분기 복구 필요. (선택: Firestore용 별도 클래스로 분리해 두면 복구 가능)

---

## 4. 2단계: 로컬을 동기 I/O로

**현재**: `LocalSaveBackend`가 `Task.Run`으로 백그라운드 실행  
**변경**: `File.ReadAllText`, `File.WriteAllText` 직접 호출

```csharp
// LocalSaveBackend
public SaveData Load()
{
    return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
}
public bool Save(SaveData data)
{
    File.WriteAllText(path, JsonUtility.ToJson(data));
    return true;
}
```

**장점**: Task 제거. 호출부가 `Load()`, `Save()`로 단순해짐.

**단점**: 파일 I/O 동안 메인 스레드 블로킹. (작은 JSON이면 보통 수 ms)

---

## 5. 3단계: Load를 코루틴으로

**현재**: `LoadAsync()` → `Task<SaveData>`, PlayScene에서 `WaitUntil`  
**변경**: `IEnumerator LoadAsync()` → `yield return`로 완료

```csharp
// SaveManager
public IEnumerator LoadAsync(Action<SaveData> onComplete)
{
    var data = Backend.Load();  // 동기
    onComplete?.Invoke(data);
    yield break;
}
```

또는 Firestore를 쓰는 경우만 Task → 코루틴 변환 유틸 사용.

**장점**: DataManager, ResourceManager와 같은 패턴. `yield return`만 사용.

---

## 6. 4단계: Handler/Contributor 구조

**현재**: ISaveHandler(Coordinator) → ISaveContributor 여러 개

**옵션 A (유지)**: 구조 유지, 단순히 코드 정리만  
- 이미 동작함. Gather/Apply 패턴은 확장에 유리.

**옵션 B (단순화)**: Coordinator 제거, SaveManager가 Contributor 직접 수집  
- `SaveManager`가 `FindObjectsByType<SaveContributorBehaviour>()` 등으로 한 번에 수집  
- PlaySaveCoordinator, GlobalSaveCoordinator 제거

**옵션 C (최대 단순화)**: SaveManager가 직접 Squad, Flag, Quest 등 참조  
- `SaveManager`에 `SquadController`, `FlagSystem` 등 SerializeField  
- Gather: 각 시스템에서 직접 SaveData에 쓰기  
- Apply: 각 시스템에서 직접 SaveData 읽기  
- Contributor 클래스 제거

→ 옵션 C가 가장 단순하지만, SaveManager가 모든 시스템을 알아야 함.  
→ 옵션 A 유지 + 1~3단계 적용이 현실적.

---

## 7. 권장 순서

1. **1단계**: Firestore 제거 (로컬만 사용)
2. **2단계**: LocalSaveBackend를 동기로 변경
3. **3단계**: SaveManager.LoadAsync → IEnumerator로 변경, Task 제거
4. **4단계**: Handler/Contributor는 유지 (이미 동작하는 부분)

---

## 8. 최종 목표 형태 (개념)

```
SaveManager
    - Load() / LoadAsync()  → SaveData 반환
    - Save() / SaveAsync()  → 저장
    - Gather() → 등록된 Handler들에서 데이터 수집
    - Apply(data) → 등록된 Handler들에 데이터 적용

LocalSaveBackend (또는 SaveManager 내부)
    - 파일 경로: persistentDataPath / SaveData
    - ReadAllText → JSON 파싱
    - WriteAllText → JSON 직렬화
```

---

*작성: 2026-03-19*
