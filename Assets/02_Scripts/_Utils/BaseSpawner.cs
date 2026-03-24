using UnityEngine;

/// <summary>
/// 스포너 공통 베이스.
/// - 씬 준비(PlayScene.IsSceneReady) 이후 자동 스폰 1회 보장
/// - Terrain 높이 스냅/프리팹 조회/인스턴스 생성 유틸 제공
/// 상속 클래스는 SpawnInternal만 구현하면 됨.
/// </summary>

public abstract class BaseSpawner : MonoBehaviour
{
    /// <summary>씬 준비 뒤 자동 스폰 여부</summary>
    [SerializeField] private bool _spawnOnStart = true;
    private bool _didAutoSpawn;

    protected abstract GameObject SpawnInternal();

    /// <summary>지면 높이에 맞춰 Y를 보정</summary>
    protected Vector3 SnapToTerrain(Vector3 pos)
    {
        pos.y = TerrainSpawnUtil.GetTerrainHeight(pos.x, pos.z, pos.y);
        return pos;
    }

    /// <summary>카테고리/이름으로 프리팹 조회(캐시/Addressables)</summary>
    protected GameObject GetPrefab(string category, string name)
    {
        var gm = GameManager.Instance;
        if (gm == null) return null;
        var rm = gm.ResourceManager;
        if (rm == null) return null;
        return rm.GetPrefab(category, name);
    }

    /// <summary>풀 또는 Instantiate로 인스턴스 생성 후 부모/변환 설정</summary>
    protected GameObject CreateInstance(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
    {
        var gm = GameManager.Instance;
        if (gm == null) return null;
        var pm = gm.PoolManager;
        GameObject go = pm != null ? pm.Pop(prefab) : Instantiate(prefab);
        if (go == null) return null;
        go.transform.SetParent(parent);
        go.transform.SetPositionAndRotation(pos, rot);
        return go;
    }

    /// <summary>수동 스폰 진입점</summary>
    public GameObject Spawn()
    {
        return SpawnInternal();
    }

    private void OnEnable()
    {
        if (!_spawnOnStart) return;
        if (PlayScene.IsSceneReady) TryAutoSpawnOnce();
        else PlayScene.OnSceneReady += OnSceneReady;
    }

    private void OnDisable()
    {
        PlayScene.OnSceneReady -= OnSceneReady;
    }

    private void OnSceneReady()
    {
        PlayScene.OnSceneReady -= OnSceneReady;
        TryAutoSpawnOnce();
    }

    private void TryAutoSpawnOnce()
    {
        if (_didAutoSpawn) return;
        _didAutoSpawn = true;
        SpawnInternal();
    }
}

