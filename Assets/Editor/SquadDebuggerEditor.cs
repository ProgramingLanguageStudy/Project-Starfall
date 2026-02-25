using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SquadDebugger))]
public class SquadDebuggerEditor : Editor
{
    private const double RepaintInterval = 0.1;
    private double _lastRepaintTime;
    private List<string> _lastValidationIssues = new List<string>();
    private bool _hasValidated;

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (!Application.isPlaying || target == null) return;
        if (EditorApplication.timeSinceStartup - _lastRepaintTime >= RepaintInterval)
        {
            _lastRepaintTime = EditorApplication.timeSinceStartup;
            Repaint();
        }
    }

    public override void OnInspectorGUI()
    {
        var debugger = (SquadDebugger)target;
        var so = new SerializedObject(debugger);
        var squadProp = so.FindProperty("_squadController");
        var teleportProp = so.FindProperty("_teleportTarget");
        var spawnableProp = so.FindProperty("_spawnableCharacters");

        EditorGUILayout.PropertyField(squadProp);
        if (squadProp.objectReferenceValue == null && GUILayout.Button("씬에서 SquadController 찾기"))
        {
            var found = FindAnyObjectByType<SquadController>();
            if (found != null) squadProp.objectReferenceValue = found;
        }

        EditorGUILayout.PropertyField(teleportProp);
        EditorGUILayout.PropertyField(spawnableProp, true);

        EditorGUILayout.Space(4);

        if (GUILayout.Button("설정 검증 (Validate Setup)"))
        {
            _hasValidated = true;
            debugger.ValidateSetup(out _lastValidationIssues);
            Repaint();
        }

        if (_hasValidated)
        {
            EditorGUILayout.Space(2);
            if (_lastValidationIssues.Count > 0)
                EditorGUILayout.HelpBox(string.Join("\n", _lastValidationIssues), MessageType.Warning);
            else
                EditorGUILayout.HelpBox("검증 통과: 부품 구성 정상", MessageType.Info);
        }

        so.ApplyModifiedProperties();
        EditorGUILayout.Space(8);

        var pc = debugger.PlayerCharacter;
        var sc = debugger.SquadControllerRef;

        if (Application.isPlaying && pc != null && pc.Model != null)
        {
            var model = pc.Model;
            EditorGUILayout.LabelField("현재 조종 캐릭터 스탯", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("체력", $"{model.CurrentHp} / {model.MaxHp}");
            EditorGUILayout.LabelField("사망", model.IsDead ? "예" : "아니오");
            EditorGUILayout.LabelField("이동 속도", model.MoveSpeed.ToString("F1"));
            EditorGUILayout.LabelField("공격력", model.AttackPower.ToString());
            EditorGUILayout.LabelField("공격 속도", model.AttackSpeed.ToString("F2"));
            EditorGUILayout.LabelField("방어력", model.Defense.ToString());
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);

        if (GUILayout.Button("텔레포트: 지정 위치로 이동"))
            debugger.TeleportToTarget();

        if (GUILayout.Button("체력 풀회복"))
        {
            if (pc == null)
                Debug.LogWarning("[SquadDebugger] PlayerCharacter가 없습니다.");
            else if (pc.Model != null)
            {
                int need = pc.Model.MaxHp - pc.Model.CurrentHp;
                if (need > 0) pc.Model.Heal(need);
            }
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("동료 소환", EditorStyles.boldLabel);
        foreach (var data in debugger.SpawnableCharacters)
        {
            if (data == null) continue;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(data.displayName ?? data.characterId);
            if (GUILayout.Button("소환", GUILayout.Width(50)))
                debugger.SpawnCompanion(data);
            EditorGUILayout.EndHorizontal();
        }

        if (sc != null && sc.Characters != null && sc.Characters.Count > 0)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("분대원 (제거)", EditorStyles.boldLabel);
            var charsCopy = new List<Character>(sc.Characters);
            foreach (var c in charsCopy)
            {
                if (c == null) continue;
                var isPlayer = c == sc.PlayerCharacter;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{(isPlayer ? "[조종] " : "")}{c.name}");
                EditorGUI.BeginDisabledGroup(isPlayer);
                if (GUILayout.Button("제거", GUILayout.Width(50)))
                    debugger.RemoveCompanion(c);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUI.EndDisabledGroup();

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("플레이 모드에서만 스탯·텔레포트·동료 소환/제거가 동작합니다.", MessageType.Info);
    }
}
