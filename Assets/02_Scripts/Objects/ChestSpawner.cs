using UnityEngine;

/// <summary>
/// ChestData를 받아 현재 위치에 상자 프리팹을 생성하는 스포너.
/// BaseSpawner의 공통 라이프사이클(씬 준비 대기/자동 1회 스폰, Terrain 높이 스냅, Pool 연동)을 사용한다.
/// </summary>

public class ChestSpawner : BaseSpawner
{
    /// <summary>스폰할 상자 데이터(보상 구성을 포함)</summary>
    [SerializeField] private ChestData _chestData;
    
    protected override GameObject SpawnInternal()
    {
        if (_chestData == null) return null;

        var prefab = GetPrefab("Object", _chestData.Id);
        if (prefab == null) return null;

        var pos = SnapToTerrain(transform.position);
        var rot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        var go = CreateInstance(prefab, pos, rot, transform);
        if (go == null) return null;

        // 생성된 Chest에 데이터 주입
        var chest = go.GetComponent<Chest>();
        if (chest != null)
        {
            var saveId = ResolveSaveId();
            if (string.IsNullOrEmpty(saveId))
                Debug.LogError("[ChestSpawner] SaveId is empty. Chest will not be saved/loaded.", this);
            chest.SetSaveId(saveId);
            if (_chestData != null)
                chest.SetData(_chestData);
        }

        return go;
    }

    private string ResolveSaveId()
    {
        var n = gameObject.name;
        if (string.IsNullOrEmpty(n)) return string.Empty;

        if (n.StartsWith("ChestSpawner"))
        {
            n = "Chest" + n.Substring("ChestSpawner".Length);
        }
        else if (n.Contains("Spawner"))
        {
            n = n.Replace("Spawner", "");
        }

        n = n.Trim();
        while (n.StartsWith("_"))
            n = n.Substring(1);

        return n;
    }
}
