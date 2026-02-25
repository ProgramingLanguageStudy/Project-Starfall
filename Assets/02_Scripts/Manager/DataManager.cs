using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 데이터 preload·관리. DialogueData 등 Resources 로드 후 npcId 기준 매핑.
/// GameManager가 보유.
/// </summary>
public class DataManager : MonoBehaviour
{
    private Dictionary<string, List<DialogueData>> _dialoguesByNpcId = new Dictionary<string, List<DialogueData>>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, ItemData> _itemsById = new Dictionary<string, ItemData>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, CharacterData> _charactersById = new Dictionary<string, CharacterData>(StringComparer.OrdinalIgnoreCase);

    public bool IsLoaded { get; private set; }

    /// <summary>GameManager에서 호출. 중복 호출 시 스킵.</summary>
    public void Initialize()
    {
        if (IsLoaded) return;
        LoadDialogues();
        LoadItems();
        LoadCharacters();
    }

    private void LoadCharacters()
    {
        _charactersById.Clear();
        var assets = Resources.LoadAll<CharacterData>("Characters");
        if (assets != null)
        {
            foreach (var a in assets)
            {
                if (a != null && !string.IsNullOrEmpty(a.characterId))
                    _charactersById[a.characterId] = a;
            }
        }
    }

    /// <summary>characterId로 CharacterData 반환. Resources/Characters에 에셋이 있어야 함.</summary>
    public CharacterData GetCharacterData(string characterId)
    {
        return _charactersById != null && characterId != null && _charactersById.TryGetValue(characterId, out var d) ? d : null;
    }

    private void LoadItems()
    {
        _itemsById.Clear();
        var assets = Resources.LoadAll<ItemData>("Items");
        if (assets != null)
        {
            foreach (var a in assets)
            {
                if (a != null && !string.IsNullOrEmpty(a.ItemId))
                    _itemsById[a.ItemId] = a;
            }
        }
    }

    /// <summary>ItemId로 ItemData 반환. Resources/Items에 에셋이 있어야 함.</summary>
    public ItemData GetItemData(string itemId)
    {
        return _itemsById != null && itemId != null && _itemsById.TryGetValue(itemId, out var d) ? d : null;
    }

    private void LoadDialogues()
    {
        _dialoguesByNpcId.Clear();
        var assets = Resources.LoadAll<DialogueData>("Dialogues");

        if (assets != null)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                var d = assets[i];
                if (d == null || string.IsNullOrEmpty(d.npcId)) continue;

                var key = d.npcId;
                if (!_dialoguesByNpcId.TryGetValue(key, out var list))
                {
                    list = new List<DialogueData>();
                    _dialoguesByNpcId[key] = list;
                }
                list.Add(d);
            }
        }

        IsLoaded = true;
    }

    /// <summary>해당 npcId의 대화 목록. 없으면 빈 리스트. npcId는 대소문자 구분 없음.</summary>
    public IReadOnlyList<DialogueData> GetDialoguesForNpc(string npcId)
    {
        if (string.IsNullOrEmpty(npcId) || !_dialoguesByNpcId.TryGetValue(npcId, out var list))
            return Array.Empty<DialogueData>();
        return list;
    }
}
