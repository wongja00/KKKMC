using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private CinemachineCamera playerCamera;


    public static CameraManager Instance;

    void Awake()
    {
        Instance = this;

        playerCamera = GameObject.FindWithTag("SceneCamera").GetComponent<CinemachineCamera>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public CinemachineCamera GetPlayerCemera()
    {
        if(playerCamera)
            return playerCamera;
            else
            return null;
    }
}
