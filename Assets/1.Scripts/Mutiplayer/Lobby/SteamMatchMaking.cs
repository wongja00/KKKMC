using Steamworks;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System.Collections.Generic;

public class SteamMatchMaking : MonoBehaviour
{
    //스팀 서버 응답함수
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected CallResult<LobbyMatchList_t> lobbyListResult;
    protected Callback<AvatarImageLoaded_t> avatarImageLoaded;
    private Dictionary<CSteamID, RoomCard> activeRoomCard = new Dictionary<CSteamID, RoomCard>();

    
    [SerializeField] private RoomCard roomCardPrefab;
    [SerializeField] private GameObject roomCardParent;

    [SerializeField] private Button hostButton; 
    [SerializeField] private Button refreshButton; 
    [SerializeField] private TMP_InputField roomNameText;

    public string roomName;

    private const string hostAddressKey = "HostAddress";//방장주소표



    void Awake()
    {
        //방 목록 검색결과
        lobbyListResult = CallResult<LobbyMatchList_t>.Create(OnLobbyListRefreshed);
    }

    void Start()
    {
        if(!SteamManager.Initialized) return;

        //콜백 연결
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);

        hostButton.onClick.AddListener(HostPublicLobby);
        refreshButton.onClick.AddListener(RefreshLobbies);

        avatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //방만들기
    public void HostPublicLobby()
    {
        //누구나 들어올수 있게 퍼블릭으로 설정(나중에 바꿀듯)
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, NetworkManager.instance.maxConnections);
    }

    //방 만들어졌을떄
    void OnLobbyCreated(LobbyCreated_t callback)
    {
        if(callback.m_eResult != EResult.k_EResultOK) return;// 실패시 종료

        roomName = roomNameText.text;

        //미러 스타트 호스트
        NetworkManager.instance.StartHost();

        CSteamID steamIDLobby = new CSteamID(callback.m_ulSteamIDLobby);
        //방장 주소
        SteamMatchmaking.SetLobbyData(
            steamIDLobby, // 미리 변수로 할당해서 재사용 가능 (예시: var lobby = new CSteamID(callback.m_ulSteamIDLobby);)
            hostAddressKey,
            SteamUser.GetSteamID().ToString()
        );

        //추가 정보(원래 파이어베이스에 등록하던 정보)
            string maxPlayers = NetworkManager.instance.maxConnections.ToString();
            string hostName = SteamFriends.GetPersonaName();

            SteamMatchmaking.SetLobbyData(
                steamIDLobby,
                "roomName",
                roomName
            );
            SteamMatchmaking.SetLobbyData(
                steamIDLobby,
                "maxPlayers",
                maxPlayers
            );
            SteamMatchmaking.SetLobbyData(
                steamIDLobby,
                "hostName",
                hostName
            );
            SteamMatchmaking.SetLobbyData(
                steamIDLobby,
                "Gamecheck",
                "CheckValue"
            );
    }

    //방찾기 버튼
    public void RefreshLobbies()
    {
        //검색 필터(전세계)
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);

        //검색 요청
        SteamAPICall_t request = SteamMatchmaking.RequestLobbyList();
        lobbyListResult.Set(request);
    }
    
    private void ClearRooms()
    {
        foreach (Transform child in roomCardParent.transform)
        {
            if(child == roomCardPrefab.transform)
                continue;

            Destroy(child.gameObject);
        }
    }

    //방목록
    void OnLobbyListRefreshed(LobbyMatchList_t result, bool bIOFailure)
    {
        ClearRooms();
        activeRoomCard.Clear();
        roomCardPrefab.gameObject.SetActive(true);

        for(int i = 0; i<result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
                      //방이름 얻어오기
            string lobbyName = SteamMatchmaking.GetLobbyData(lobbyID, "roomName");
            string hostName = SteamMatchmaking.GetLobbyData(lobbyID, "hostName");
            string checkValue = SteamMatchmaking.GetLobbyData(lobbyID, "Gamecheck");

            if(checkValue != "CheckValue")
                continue;

            RoomCard card = Instantiate(roomCardPrefab, roomCardParent.transform);
            CSteamID hostID = SteamMatchmaking.GetLobbyOwner(lobbyID);
            
            card.SetTexts(lobbyName, hostName, 0, 0);

            activeRoomCard[hostID] = card;

            //CSteamID steamID = new CSteamID(ulong.Parse(lobbyID));
            int avatar = SteamFriends.GetLargeFriendAvatar(hostID);
            
            //스팀 프사
            if(avatar != -1)
            {
                ApplyAvatarToCard(avatar, card);
            }


            card.OnAddButtonEvent(async () => {
                try{
                    #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
                    //스팀 아이디 형식인지 체크
                    if(SteamManager.Initialized)
                    {
                        JoinLobby(lobbyID);
                    }
                    else
                    {
                        JoinLobby(lobbyID);
                    }
                    #else
                    //await NetworkManager.instance.JoinRelayRoom(roomJoinCode);
                    #endif
                }
                catch(System.Exception ex)
                {
                    Debug.Log($"실패: {ex.Message}");
                }
                finally
                {
                    refreshButton.interactable = true;
                }
                  });

            Debug.Log($"발견된 방: {lobbyName} 호스트: {hostName}");
        }
        roomCardPrefab.gameObject.SetActive(false);
    }

    private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
    {
        //다운로드한 프사 표시
        if(activeRoomCard.TryGetValue(callback.m_steamID, out RoomCard targetCard))
        {
            ApplyAvatarToCard(callback.m_iImage, targetCard);
        }
    }
    private void ApplyAvatarToCard(int avatarInt, RoomCard card)
    {
        uint width, height;
        if(SteamUtils.GetImageSize(avatarInt, out width, out height))
        {
            byte[] image = new byte[4 * (int)width * (int)height];
            if(SteamUtils.GetImageRGBA(avatarInt, image, 4 * (int)width * (int)height))
            {
                Texture2D avatarImage = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
                avatarImage.LoadRawTextureData(image);
                avatarImage.Apply();
                card.SetProfileImage(avatarImage);
            }
        }
    }

    //방입장
    public void JoinLobby(CSteamID lobbyID)
    {
        //스팀 로비 입장
        SteamMatchmaking.JoinLobby(lobbyID);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        //방장 아닐때만 실행
        if(NetworkServer.active) return;

        //방장 스팀 아이디 읽어옴
        CSteamID lobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        string hostAddress = SteamMatchmaking.GetLobbyData(lobbyID, hostAddressKey);

        //아이디를 통한 접속
        NetworkManager.instance.networkAddress = hostAddress;
        NetworkManager.instance.StartClient();
    }
}
