using UnityEngine;

public class DestroyNextScene : MonoBehaviour
{
    void Awake()
    {
        // DontDestroyOnLoad로 다음 씬에 남아있게 함
        DontDestroyOnLoad(gameObject);

        // 현재 씬에 동일한 타입의 오브젝트가 여러개 있으면 자신을 파괴
        var objs = FindObjectsOfType<DestroyNextScene>();
        if (objs.Length > 1)
        {
            // 자신만 파괴
            Destroy(gameObject);
        }
    }
}