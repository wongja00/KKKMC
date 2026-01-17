using UnityEngine;
using Mirror;

public class VehicleNetworkSync : NetworkBehaviour
{
    [Header("차량 컴포넌트")]
    [SerializeField] private MobileFortress mobileFortress;
    [SerializeField] private VehicleDriveController driveController;

    [Header("네트워크 동기화")]
    [SyncVar] private bool isPlayerControlled = false;
    [SyncVar] private NetworkIdentity currentDriver;
    [SyncVar] private Vector3 networkPosition;
    [SyncVar] private Quaternion networkRotation;

    //차량 탑승 요청(클라이언트 -> 서버)
    [Command(requiresAuthority = false)]
    public void CmdEnterVehicle(NetworkIdentity player)
    {
        if(!isPlayerControlled)
        {
            isPlayerControlled = true;
            currentDriver = player;

            //모든 클라이언트에게 탑승 알림
            RpcOnPlayerEnterVehicle(player);
        }
    }

    //차량 하차 요청(클라이언트 -> 서버)
    [Command(requiresAuthority = false)]
    public void CmdExitVehicle()
    {
        if(isPlayerControlled && currentDriver != null)
        {
            isPlayerControlled = false;
            NetworkIdentity driver = currentDriver;
            currentDriver = null;

            //모든 클라이언트에게 하차 알림
            RpcOnPlayerExitVehicle(driver);
        }
    }

    //모든 클라이언트에서 탑승 처리
    [ClientRpc]
    private void RpcOnPlayerEnterVehicle(NetworkIdentity player)
    {
        if(player.isLocalPlayer)
        {
            //로컬 플레이어가 탑승
            driveController.OnDriverEnter();
        }
        //차량 제어 설정
        mobileFortress.SetPlayerControl(player.isLocalPlayer);
    }
 
    //모든 클라이언트에서 하차 처리
    [ClientRpc]
    private void RpcOnPlayerExitVehicle(NetworkIdentity player)
    {
        if(player.isLocalPlayer)
        {
            //로컬 플레이어가 하차
            driveController.OnDriverExit();
        }

        mobileFortress.SetPlayerControl(false);
    }

    void Update()
    {   
        //서버에서만 위치 동기화
        if(isServer)
        {
            networkPosition = transform.position;
            networkRotation = transform.rotation;
        }

        //클라이언트에서 위치보간
        if(!isServer)
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 10);
        }

    }


}
