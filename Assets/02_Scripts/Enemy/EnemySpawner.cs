using UnityEngine;

/// <summary>
/// EnemyTeamData 기반 팀 스폰. RM Enemy/{id}·DM EnemyData·PoolManager.
/// <see cref="Initialize"/>는 CombatController만 주입. 스폰은 <see cref="PlayScene.OnSceneReady"/> 이후 자동 또는 <see cref="SpawnTeam"/> 수동.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    private const string EnemyPrefabCategory = "Enemy";

    [SerializeField] [Tooltip("스폰할 팀 정의. enemyIds·spawnRadius")]
    private EnemyTeamData _teamData;
    [SerializeField] [Tooltip("비면 Spawner 위치에 생성")]
    private Transform _spawnPoint;
    [SerializeField] [Tooltip("true면 씬 준비 후 자동 스폰, false면 SpawnTeam() 수동")]
    private bool _spawnOnStart = true;

    private bool _didAutoSpawn;
    private CombatController _combatController;

    // 부트·RM/DM 이후에만 스폰: 이미 준비됐으면 즉시, 아니면 OnSceneReady 1회 구독
    private void OnEnable()
    {
        if (!_spawnOnStart)
            return;

        if (PlayScene.IsSceneReady)
            TryAutoSpawnOnce();
        else
            PlayScene.OnSceneReady += OnPlaySceneReadyForAutoSpawn;
    }

    private void OnDisable()
    {
        PlayScene.OnSceneReady -= OnPlaySceneReadyForAutoSpawn;
    }

    private void OnPlaySceneReadyForAutoSpawn()
    {
        PlayScene.OnSceneReady -= OnPlaySceneReadyForAutoSpawn;
        TryAutoSpawnOnce();
    }

    private void TryAutoSpawnOnce()
    {
        if (_didAutoSpawn)
            return;
        _didAutoSpawn = true;
        SpawnTeam();
    }

    /// <summary>PlayScene 루트에서 CombatController만 주입. 스폰은 OnSceneReady·수동 호출.</summary>
    public void Initialize(CombatController combatController)
    {
        _combatController = combatController;
    }

    /// <summary>TeamData 기반 팀 생성. 멤버는 풀에서 Pop 후 ConfigureFromSpawn.</summary>
    public EnemyTeam SpawnTeam()
    {
        if (_teamData == null || _combatController == null)
            return null;

        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[EnemySpawner] GameManager.Instance is null.", this);
            return null;
        }

        var rm = gm.ResourceManager;
        var dm = gm.DataManager;
        var pm = gm.PoolManager;
        if (rm == null || dm == null || pm == null)
        {
            Debug.LogError("[EnemySpawner] ResourceManager, DataManager 또는 PoolManager 가 null입니다.", this);
            return null;
        }

        // 풀에 들어가는 개별 Enemy와 분리: 팀은 빈 오브젝트로만 추적·해산
        var teamObj = new GameObject($"EnemyTeam_{_teamData.name}");
        teamObj.transform.SetParent(transform);
        var team = teamObj.AddComponent<EnemyTeam>();
        team.Initialize(_combatController);

        // 바닥 높이: Terrain이 있으면 SampleHeight, 없으면 원래 Y 유지
        Vector3 basePos = _spawnPoint != null ? _spawnPoint.position : transform.position;
        float baseY = TerrainSpawnUtil.GetTerrainHeight(basePos.x, basePos.z, basePos.y);
        basePos.y = baseY;

        float radius = _teamData.spawnRadius;
        var ids = _teamData.enemyIds;
        if (ids == null || ids.Count == 0)
            return team;

        int count = ids.Count;
        // 멤버마다 동일 키로 DM(데이터)·RM(프리팹)·풀 인스턴스 연결
        for (int i = 0; i < count; i++)
        {
            var id = ids[i];
            if (string.IsNullOrEmpty(id))
                continue;

            var data = dm.Get<EnemyData>(id);
            if (data == null)
            {
                Debug.LogWarning($"[EnemySpawner] EnemyData 없음 (DM): {id}", this);
                continue;
            }

            var prefab = rm.GetPrefab(EnemyPrefabCategory, id);
            if (prefab == null)
            {
                Debug.LogWarning($"[EnemySpawner] 프리팹 없음 (RM): {EnemyPrefabCategory}/{id}", this);
                continue;
            }

            // 수평 원형 배치 (Y는 아래에서 지형에 맞춤)
            var offset = count == 1
                ? Vector3.zero
                : new Vector3(
                    Mathf.Cos(i * Mathf.PI * 2f / count) * radius,
                    0f,
                    Mathf.Sin(i * Mathf.PI * 2f / count) * radius);

            Vector3 pos = basePos + offset;
            pos.y = TerrainSpawnUtil.GetTerrainHeight(pos.x, pos.z, pos.y);

            var go = pm.Pop(prefab);
            if (go == null)
                continue;

            go.transform.SetParent(teamObj.transform);
            go.transform.SetPositionAndRotation(pos, Quaternion.identity);

            var enemy = go.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.ConfigureFromSpawn(_combatController, data, pos);
                team.AddMember(enemy);
            }
        }

        return team;
    }
}
