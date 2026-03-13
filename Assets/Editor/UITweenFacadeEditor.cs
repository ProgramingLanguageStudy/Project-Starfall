using System;
using DG.Tweening;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UITweenFacade))]
public class UITweenFacadeEditor : Editor
{
    private SerializedProperty _controlActive;
    private SerializedProperty _useDefaultPreset;
    private SerializedProperty _role;
    private SerializedProperty _customDuration;
    private SerializedProperty _customEase;
    private SerializedProperty _customScaleFrom;
    private SerializedProperty _customScaleTo;
    private SerializedProperty _customAlphaFrom;
    private SerializedProperty _customAlphaTo;

    private void OnEnable()
    {
        _controlActive = serializedObject.FindProperty("controlActive");
        _useDefaultPreset = serializedObject.FindProperty("useDefaultPreset");
        _role = serializedObject.FindProperty("role");
        _customDuration = serializedObject.FindProperty("customDuration");
        _customEase = serializedObject.FindProperty("customEase");
        _customScaleFrom = serializedObject.FindProperty("customScaleFrom");
        _customScaleTo = serializedObject.FindProperty("customScaleTo");
        _customAlphaFrom = serializedObject.FindProperty("customAlphaFrom");
        _customAlphaTo = serializedObject.FindProperty("customAlphaTo");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_controlActive);

        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(_useDefaultPreset);
        EditorGUILayout.PropertyField(_role);

        bool useDefault = _useDefaultPreset.boolValue;
        var role = (UIRole)_role.enumValueIndex;

        EditorGUILayout.Space(5);
        var preset = GetPresetForRole(role);

        if (useDefault)
        {
            EditorGUILayout.LabelField($"[{role}] 기본 수치", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Duration", preset.Duration);
            EditorGUILayout.EnumPopup("Ease", preset.Ease);
            EditorGUILayout.Vector3Field("Scale From", preset.ScaleFrom);
            EditorGUILayout.Vector3Field("Scale To", preset.ScaleTo);
            EditorGUILayout.FloatField("Alpha From", preset.AlphaFrom);
            EditorGUILayout.FloatField("Alpha To", preset.AlphaTo);
            EditorGUI.EndDisabledGroup();
        }

        if (!useDefault)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("커스텀 수치", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_customDuration);
            EditorGUILayout.PropertyField(_customEase);
            EditorGUILayout.PropertyField(_customScaleFrom);
            EditorGUILayout.PropertyField(_customScaleTo);
            EditorGUILayout.PropertyField(_customAlphaFrom);
            EditorGUILayout.PropertyField(_customAlphaTo);

            if (GUILayout.Button("현재 Role 기본값으로 초기화"))
            {
                ApplyPresetToCustom(preset);
            }
        }
        else if (GUILayout.Button("Role 기본값을 커스텀으로 복사 후 수정 모드"))
        {
            _useDefaultPreset.boolValue = false;
            ApplyPresetToCustom(preset);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static UIPresetData GetPresetForRole(UIRole role)
    {
        return role switch
        {
            UIRole.Title => TitlePreset.Data,
            UIRole.Panel => PanelPreset.Data,
            UIRole.Toast => ToastPreset.Data,
            _ => PanelPreset.Data
        };
    }

    private void ApplyPresetToCustom(UIPresetData preset)
    {
        _customDuration.floatValue = preset.Duration;
        var easeValues = (Ease[])Enum.GetValues(typeof(Ease));
        var idx = Array.IndexOf(easeValues, preset.Ease);
        if (idx >= 0) _customEase.enumValueIndex = idx;
        _customScaleFrom.vector3Value = preset.ScaleFrom;
        _customScaleTo.vector3Value = preset.ScaleTo;
        _customAlphaFrom.floatValue = preset.AlphaFrom;
        _customAlphaTo.floatValue = preset.AlphaTo;
    }
}
