using UnityEngine;
using Mirror;
using Unity.Cinemachine;

public class PlayerNetworkManager : NetworkBehaviour
{
   [Header("플레이어 컴포넌트")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private MouseLook mouseLook;

    [Header("네트워크 동기화")]
    [SyncVar] public string playerName = "Player";
    [SyncVar] public Color playerColor = Color.white;

    void Awake()
    {
        if(playerCamera != null)
            playerCamera = CameraManager.Instance.GetPlayerCemera();
    }

    //네트워크 연결시 실행
    public override void OnStartLocalPlayer()
    {

        //로컬 플레이어만 실행
        if(playerCamera != null)
            playerCamera.enabled = true;
        playerMovement.enabled = true;
        mouseLook.enabled = true;

        SetupLocalPlayerUI();
    }

    public override void OnStartClient()
    {
        if(!isLocalPlayer && playerCamera != null)
        {
            playerCamera.enabled = false;
            playerMovement.enabled = false;
            mouseLook.enabled = false;
        }
    }

    private void SetupLocalPlayerUI()
    {
        Debug.Log($"로컬 플레이어 설정:: {playerName}");
    }
}
