using UnityEngine;
using System.Threading.Tasks;
using Mirror;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Netcode.Transports.UTP;


public class NetworkManager : Mirror.NetworkManager
{
    [Header("플레이어 프리팹")]
    [SerializeField] private GameObject origiPlayerPrefab;

    public static NetworkManager instance;
    private UnityTransport unityTransport1;

    async public override void Awake() 
    {
        if(instance == null)
        {
            instance = this;
        }

        unityTransport1 = GetComponent<UnityTransport>();
        
        // 씬이 넘어가도 객체가 파괴되지 않도록 함
        DontDestroyOnLoad(this.gameObject);
        
        await UnityServices.InitializeAsync();
    }


    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        //플레이어 스폰
        GameObject player = Instantiate(origiPlayerPrefab);
        NetworkServer.AddPlayerForConnection(conn, player);
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("클라이언트 연결됨");
    }

    async public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        
        if(NetworkServer.active)
        {
            await FirebaseRoomManager.instance.DeleteRoom(FirebaseRoomManager.instance.roomId);
        }

        Debug.Log("클라이언트 연결 해제됨");
    }
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if(conn.identity != null)
        {
            NetworkServer.Destroy(conn.identity.gameObject);
        }
        base.OnServerDisconnect(conn);
    }

    public async Task<string> StartRelayHost()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();            
        }

        //최대 20명
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(20);

        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        StartHost();

        //unityTransport1.SetHostRelayData(
        //    allocation.RelayServer.IpV4,
        //    (ushort)allocation.RelayServer.Port,
        //    allocation.AllocationIdBytes,
        //    allocation.Key,
        //    allocation.ConnectionData
        //);

        return joinCode;
    }

    public async Task JoinRelayRoom(string joinCode)
    {
        try
        {
        if (string.IsNullOrEmpty(joinCode) || joinCode.Length < 6)
        {
            Debug.LogError("유효하지 않은 join code입니다.");
            return;
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            //await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log(joinCode);

        //JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        //unityTransport1.SetClientRelayData(
        //    allocation.RelayServer.IpV4,
        //    (ushort)allocation.RelayServer.Port,
        //    allocation.AllocationIdBytes,
        //    allocation.Key,
        //    allocation.ConnectionData,
        //    allocation.HostConnectionData
        //);
        networkAddress = joinCode;

        StartClient();

        }
        catch(RelayServiceException ex)
        {
            Debug.Log($"릴레이 접속 실패: {ex.Message}");
        }
    }


}
