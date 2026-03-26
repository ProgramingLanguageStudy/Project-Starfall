using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform _camTransform;

    void Start()
    {
        // 매번 Camera.main을 호출하는 것은 비용이 드므로 캐싱합니다.
        if (Camera.main != null) 
            _camTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (_camTransform == null) return;

        // 방법 1: 카메라와 평행하게 만들기 (가장 깔끔함)
        transform.rotation = _camTransform.rotation;

        // 방법 2: Y축 회전만 고정하고 싶을 때 (NPC 이름표 등에 적합)
        // Vector3 targetDir = _camTransform.position - transform.position;
        // targetDir.y = 0;
        // transform.rotation = Quaternion.LookRotation(-targetDir);
    }
}