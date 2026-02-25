using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class ChangeMainScene : MonoBehaviour
{
    [Header("이동할씬")]
    [SerializeField] private string sceneName;

    [SerializeField] private Button btn;

    public void OnClickDungeon()
    {
        if(NetworkServer.active)
        {
            NetworkManager.instance.ServerChangeScene(sceneName);
        }
    }

    void Start()
    {
        //if(NetworkServer.active)
        {
            btn.onClick.AddListener(OnClickDungeon);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
