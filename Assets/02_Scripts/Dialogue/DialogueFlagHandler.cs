using UnityEngine;

/// <summary>
/// flagsToModify만 처리. DialogueEndedRegistry에 등록.
/// </summary>
public class DialogueFlagHandler : MonoBehaviour, IDialogueEndedHandler
{
    private void Awake()
    {
        PlaySceneServices.DialogueEnded.Register(this);
    }

    private void OnDestroy()
    {
        PlaySceneServices.DialogueEnded.Unregister(this);
    }

    public void OnDialogueEnded(DialogueData data)
    {
        if (data == null || data.flagsToModify == null) return;

        var fm = GameManager.Instance?.FlagManager;
        if (fm == null) return;

        foreach (var mod in data.flagsToModify)
        {
            if (string.IsNullOrEmpty(mod.key)) continue;
            if (mod.op == FlagOp.Set)
                fm.SetFlag(mod.key, mod.value);
            else
                fm.AddFlag(mod.key, mod.value);
        }
    }
}
