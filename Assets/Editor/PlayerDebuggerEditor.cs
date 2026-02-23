using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerDebugger))]
public class PlayerDebuggerEditor : Editor
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
        double now = EditorApplication.timeSinceStartup;
        if (now - _lastRepaintTime >= RepaintInterval)
        {
            _lastRepaintTime = now;
            Repaint();
        }
    }

    public override void OnInspectorGUI()
    {
        var debugger = (PlayerDebugger)target;
        var so = new SerializedObject(debugger);
        var squadProp = so.FindProperty("_squadController");

        EditorGUILayout.PropertyField(squadProp);

        if (squadProp.objectReferenceValue == null && GUILayout.Button("씬에서 SquadController 찾기"))
        {
            var found = FindAnyObjectByType<SquadController>();
            if (found != null)
            {
                squadProp.objectReferenceValue = found;
                so.ApplyModifiedProperties();
            }
            else
            {
                EditorGUILayout.HelpBox("씬에 SquadController가 없습니다.", MessageType.Warning);
            }
        }

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

        DrawDefaultInspector();
        EditorGUILayout.Space(4);

        var pc = debugger.PlayerCharacter;

        if (Application.isPlaying && pc != null && pc.Model != null)
        {
            var model = pc.Model;
            EditorGUILayout.LabelField("현재 스탯", EditorStyles.boldLabel);
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
        {
            debugger.TeleportToTarget();
        }
        if (GUILayout.Button("체력 풀회복"))
        {
            if (pc == null)
            {
                Debug.LogWarning("[PlayerDebugger] SquadController 또는 PlayerCharacter가 없습니다. 인스펙터에서 할당하고 플레이 모드로 실행하세요.");
                return;
            }
            if (pc.Model != null)
            {
                int need = pc.Model.MaxHp - pc.Model.CurrentHp;
                if (need > 0)
                    pc.Model.Heal(need);
            }
        }
        EditorGUI.EndDisabledGroup();

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("플레이 모드에서만 스탯 표시·체력 회복·텔레포트가 동작합니다.", MessageType.Info);
    }
}
