/// <summary>상호작용 시 필요한 API. Character가 구현.</summary>
using UnityEngine;

public interface IInteractReceiver
{
    void Teleport(Vector3 position);
    void Teleport(Transform destination);
}

public interface IInteractable
{
    string GetInteractText();
    void Interact(IInteractReceiver receiver);
}