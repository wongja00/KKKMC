using UnityEngine;
using System.Threading.Tasks;
using Mirror;
using Unity.Services.Authentication;
using Mirror.FizzySteam;
using System;
using Unity.Netcode.Components;
using System.Collections.Generic;
using Mirror.Examples.MultipleMatch;





#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
using Steamworks;
#endif


public class NetworkManager : Mirror.NetworkManager
{
    [Header("스팀 할겨?")]
    public bool isSteam = false;

    [Header("스팀 매치메이킹")]
    public SteamMatchMaking steamMatchMaker;
    
    [Header("플레이어 프리팹 리스트")]
    [SerializeField] private CharacterInfo[] playerInfos;
    private Dictionary<int, GameObject> playerPrefabDic = new Dictionary<int, GameObject>();

    public static NetworkManager instance;
    private bool isInitialized = false;

    [SerializeField] private FizzySteamworks fizzySteamworks;//steam transport
    [SerializeField] private DungeonGenerator dungeonGenerator;

    public int ConnectionID;
    
    public int PlayerNumber;
    public ulong PlayerSteamID;

    public event Action OnPlayerJoin;

    [Header("캐릭터선택창")]
    [SerializeField] private GameObject characterChoiceUI;
    
    //클라이언트 캐릭터 ID
    [SerializeField]private int curCharacterID = -1;
    public event Action<int> OnClientSceneChangedEvent;
    public event Action<string> OnServerSceneChangedEvent;

    public Dictionary<NetworkConnectionToClient, int> playersID = new Dictionary<NetworkConnectionToClient, int>();

    public override void Awake() 
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            //Destroy(this.gameObject);
        }

        foreach(CharacterInfo info in playerInfos)
        {
            playerPrefabDic.Add(info.ID, info.prefab);
        }

        if(playerInfos.Length > 0)
        {
            characterChoiceUI.GetComponent<PlayerChoiceUI>().SetCards(playerInfos);
        }

        #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
        if(fizzySteamworks == null)
        {
            fizzySteamworks = GetComponent<FizzySteamworks>();
        }

        if(!SteamManager.Initialized)
        {
            Debug.Log("[네트워크 매니저]: 스팀이 초기화 돼지 않음");
        }
        else
        {
            Debug.Log($"[네트워크 매니저]: 스팀이 초기화 완료 스팀아디: {SteamUser.GetSteamID()}");
        }
        #endif

        
        // 씬이 넘어가도 객체가 파괴되지 않도록 함
        DontDestroyOnLoad(this.gameObject);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);


    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if(characterChoiceUI != null) characterChoiceUI.SetActive(true);
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        playersID.Add(conn, -1);

    }
    
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        
        OnPlayerJoin?.Invoke();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);

    }

    public override void OnServerSceneChanged(string newSceneName)
    {
        base.OnServerSceneChanged(newSceneName);

        GeneratedDungeon(newSceneName);
    }

    private void GeneratedDungeon(string sceneName)
    {
        if(sceneName.Contains("Dungeon"))
        {
            Debug.Log("던전씬 도착 완료");

            SetupPlayer();

            FindAnyObjectByType<DungeonGenerator>().MakeDungeon();


        }
    }

    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);


        // 캐릭터 ID가 저장되어 있는지 확인 (딕셔너리 사용)
        if (playersID.TryGetValue(conn, out int selectedID))
        {
            // 이미 플레이어 객체가 붙어있는지 확인 (중복 에러 방지)
            if (conn.identity == null)
            {
                CharacterSpawn(selectedID, conn);
            }
            else
            {
                NetworkServer.Destroy(conn.identity.gameObject);
                CharacterReplace(selectedID, conn);
                
            }
        }
    }



    void SetupPlayer()
    {
        foreach(NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if(conn.identity != null)
            {
            }
        }
    }

    public void CharacterSpawn(int ID, NetworkConnectionToClient conn)
    {
        if(playerPrefabDic.TryGetValue(ID, out GameObject value))
        {
            GameObject player = Instantiate(playerPrefabDic[ID].gameObject);
            Status status = new Status();

            for (int i = 0; i < playerInfos.Length; i++)
            {
                if (playerInfos[i].ID == ID)
                {
                    status = playerInfos[i].status;
                    break;
                }
            };
            player.GetComponent<Player>().StatChange(status);
            player.GetComponent<Player>().CharacterStartInit();
            player.GetComponent<Player>().SetStatUI();
            player.GetComponent<Player>().OnStatChangeUI();
            //player.GetComponent<NetworkTransform>().Teleport(Vector3.zero, Quaternion.identity, Vector3.one);

            NetworkServer.AddPlayerForConnection(conn, player);
        }
        else
        {
            GameObject player = Instantiate(playerPrefab);
            Status status = new Status();

            for (int i = 0; i < playerInfos.Length; i++)
            {
                if (playerInfos[i].ID == ID)
                {
                    status = playerInfos[i].status;
                    break;
                }
            };
            player.GetComponent<Player>().StatChange(status);
            player.GetComponent<Player>().CharacterStartInit();
            player.GetComponent<Player>().SetStatUI();
            player.GetComponent<Player>().OnStatChangeUI();
            //player.GetComponent<NetworkTransform>().Teleport(Vector3.zero, Quaternion.identity, Vector3.one);


            NetworkServer.AddPlayerForConnection(conn, player);

            Debug.Log("캐릭 생성");
        }
    }

    public void CharacterReplace(int ID, NetworkConnectionToClient conn)
    {
        GameObject player = Instantiate(playerPrefabDic[ID].gameObject);

        Status status = new Status();

        for (int i = 0; i< playerInfos.Length; i++)
        {
            if(playerInfos[i].ID == ID)
            {
                status = playerInfos[i].status;
                break;
            }
        };

        ReplacePlayerOptions options = new ReplacePlayerOptions();
        
        NetworkServer.ReplacePlayerForConnection(conn, player, options);

        player.GetComponent<Player>().StatChange(status);
        player.GetComponent<Player>().CharacterStartInit();
        player.GetComponent<Player>().SetStatUI();
        player.GetComponent<Player>().OnStatChangeUI();

        Debug.Log("캐릭 교체");

        player.transform.Translate(new Vector3(0,2,0));

        playersID[conn] = ID;
    }
    
    public void SetCurCharacterID(int ID)
    {
        curCharacterID = ID;
    }

    public int GetCurCharacterName()
    {
        return curCharacterID;
    }

    public void CloseChoiceUI()
    {
        characterChoiceUI.SetActive(false);
    }


    async public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        
        if(NetworkServer.active)
        {
            if(FirebaseRoomManager.instance != null)
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

    public async Task<string> StartSteamHost()
    {
        #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
        if(!SteamManager.Initialized)
        {
            Debug.LogError("[SteamHost] Steam이 초기화되지 않았습니다.");
            return null;
        }

        //Steam Transport 사용 확인
        if(fizzySteamworks == null)
        {
            Debug.LogError("[SteamHost] 트랜스포트 미설정");
            return null;
        }

        //네트워크 매니저의 트랜스 포트를 스팀 트랜스포트로 설정
        if(transport != fizzySteamworks && isSteam == true)
        {
            transport = fizzySteamworks;
        }

        Debug.Log($"[SteamHost] 호스트 시작: {SteamUser.GetSteamID()}");
        StartHost();

        string tempID = System.Guid.NewGuid().ToString();
        //일단 유저 ID로 로비 아이디
        string lobbyId = SteamUser.GetSteamID().ToString();
        Debug.Log($"[SteamHost] 호스트 시작완료: {lobbyId}");
        return lobbyId;

        #else
        Debug.LogError($"[SteamHost] 스팀은 스탠드어론 플랫폼에서만 사용가능");
        return null;
        #endif
    }

    public async Task<string> StartRelayHost()
    {
        //await EnsureInitialized();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();            
        }

        //최대 20명
        Unity.Services.Relay.Models.Allocation allocation = await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(20);

        string joinCode = await Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);



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

    public async Task JoinSteamRoom(string steamIdOrLobbyId)
    {
        #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
        try
        {
            if(!SteamManager.Initialized)
            {
                Debug.LogError("[JoinSteamRoom] 스팀 초기화 되지 않음");
                return;
                        
            }
            
            if(fizzySteamworks == null)
            {
                Debug.LogError("[JoinSteamRoom] FizzySteamworks 트랜스포트 미설정");
            }

            //스팀 아디로 변경
            if(ulong.TryParse(steamIdOrLobbyId, out ulong steamId))
            {
                networkAddress = steamIdOrLobbyId;
                Debug.Log($"[JoingSteamRoom] Steam ID로 연결 시도: {steamIdOrLobbyId}");
                StartClient();
            }
            else
            {
                Debug.LogError("[JoingSteamRoom] 유효하지 않은 스팀 아이디");
            }
        }
        catch(System.Exception ex)
        {
            Debug.LogError($"[JoinSteamRoom] 연결중 오류: {ex.Message}");
            throw;
        }
        #else
        Debug.LogError($"[JoinSteamRoom] Steam은 Standalone 플랫폼에서만 사용가능");
        #endif
    }

    public async Task JoinRelayRoom(string joinCode)
    {
        try
        {
            joinCode = joinCode?.Trim();

            if (string.IsNullOrEmpty(joinCode) || joinCode.Length < 6)
            {
                Debug.LogError("유효하지 않은 join code입니다.");
                return;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log($"조인코드로 연결시도{joinCode}");

            StartClient();

        }
        catch(System.Exception ex)
        {
            Debug.Log($"연결중 오류 발생{ex.Message}");
            throw;
        }
    }


}
