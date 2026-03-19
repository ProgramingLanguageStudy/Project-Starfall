# Addressables 정리

> 동적 로딩, Local/Remote 전환, 빌드 개념 정리.

---

## 1. 왜 Addressables?

| 방식 | 동적 로딩 | 서버 다운로드 | 앱 업데이트 없이 콘텐츠 교체 |
|------|-----------|---------------|------------------------------|
| Resources | ✅ | ❌ | ❌ |
| Addressables (Local) | ✅ | ❌ | ❌ |
| Addressables (Remote) | ✅ | ✅ | ✅ |

- **Resources**: 빌드에 전부 포함, `Resources.Load()`로 로드. 서버 불가.
- **Addressables**: Local(앱 내) 또는 Remote(서버) 선택 가능. **같은 API**로 로드.

---

## 2. Local vs Remote

| 구분 | Local | Remote |
|------|-------|--------|
| **번들 위치** | 앱 패키지 안 | 서버/CDN |
| **로드 시점** | 앱 내부에서 즉시 | 네트워크로 다운로드 후 |
| **용도** | UI, 핵심 에셋 | 캐릭터, 맵, DLC 등 |
| **오프라인** | ✅ | ❌ (다운로드 필요) |

---

## 3. 빌드(Build)의 의미

**Addressables Build** = Addressable 에셋을 **번들(.bundle)**로 묶는 과정.

| 결과 | Local | Remote |
|------|-------|--------|
| 번들 파일 | 앱에 포함 | 서버에 업로드 |

- **Local**: 번들이 앱에 포함됨. 서버 없음.
- **Remote**: 번들을 서버에 올려두고, 런타임에 다운로드.

---

## 4. 에디터에서 빌드 없이 되는 이유

**Play Mode Script** 설정에 따라:

| 모드 | 설명 |
|------|------|
| **Use Asset Database** | 번들 생성 없이 프로젝트 에셋에서 직접 로드. 빠른 테스트용. |
| **Use Asset Bundles** | 실제 번들 빌드 후 사용. 빌드와 동일한 동작. |

에디터에서는 Asset Database 모드로 **빌드 없이** 테스트 가능.

---

## 5. Local → Remote 전환

| 변경 | 내용 |
|------|------|
| **코드** | 변경 없음. `Addressables.LoadAsync(key)` 그대로 사용. |
| **설정** | 그룹의 Build Path / Load Path를 Remote로 변경. |
| **배포** | 빌드된 번들을 서버/CDN에 업로드. |

**핵심**: 로딩 코드는 그대로 두고, **그룹 설정만** 바꾸면 됨.

---

## 6. UI 배치 전략

| 에셋 | 권장 위치 | 이유 |
|------|-----------|------|
| **ErrorPanel, SceneTransitionPanel** | Local | 앱 시작/씬 전환 시 즉시 필요, 오프라인 대응 |
| **캐릭터, 맵, DLC** | Remote (선택) | 용량·업데이트 유연성 |

- UI 변경 = 앱 버전 업데이트와 함께 배포 (Local 유지 시).
- Remote로 두면 앱 업데이트 없이 UI만 교체 가능하지만, UI는 보통 Local이 일반적.

---

## 7. 프로젝트 적용

- **ResourceManager**: Addressables API 래핑.
- **현재**: 전부 Local Addressables.
- **추후**: 필요한 그룹만 Remote로 전환.

---

*작성: 2026-03-19*
