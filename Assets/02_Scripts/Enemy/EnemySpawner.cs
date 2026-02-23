using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// EnemyTeamData 기반 팀 스폰. Data만 바꾸면 팀 조합 변경.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] [Tooltip("스폰할 팀 정의. 프리팹 목록·배치 반경 포함")]
    private EnemyTeamData _teamData;
    [SerializeField] [Tooltip("비면 Spawner 위치에 생성")]
    private Transform _spawnPoint;

    private CombatController _combatController; // Initialize로 PlayScene이 주입

    /// <summary>PlayScene에서 CombatController·초기 스폰 등 호출.</summary>
    public void Initialize(CombatController combatController)
    {
        _combatController = combatController;
        SpawnTeam();
    }

    /// <summary>TeamData 기반으로 팀 생성. Spawner가 멤버 스폰 후 Team에 등록.</summary>
    public EnemyTeam SpawnTeam()
    {
        if (_teamData == null || _combatController == null) return null;

        var teamObj = new GameObject($"EnemyTeam_{_teamData.name}");
        teamObj.transform.SetParent(transform);
        var team = teamObj.AddComponent<EnemyTeam>();
        team.Initialize(_combatController);

        Vector3 basePos = _spawnPoint != null ? _spawnPoint.position : transform.position;
        float radius = _teamData.spawnRadius;
        var prefabs = _teamData.enemyPrefabs;
        if (prefabs == null || prefabs.Count == 0) return team;

        int count = prefabs.Count;
        for (int i = 0; i < count; i++)
        {
            var prefab = prefabs[i];
            if (prefab == null) continue;

            var offset = count == 1 ? Vector3.zero : new Vector3(
                Mathf.Cos(i * Mathf.PI * 2f / count) * radius, 0f,
                Mathf.Sin(i * Mathf.PI * 2f / count) * radius);

            Vector3 pos = basePos + offset;
            if (NavMesh.SamplePosition(pos, out var hit, radius * 2f, NavMesh.AllAreas))
                pos = hit.position;

            var go = Object.Instantiate(prefab, pos, Quaternion.identity, teamObj.transform);
            var enemy = go.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Initialize();
                team.AddMember(enemy);
            }
        }

        return team;
    }
}
