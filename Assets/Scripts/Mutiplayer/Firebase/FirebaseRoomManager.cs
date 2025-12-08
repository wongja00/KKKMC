using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Auth;
using System;
using System.Net;
using System.Collections;
using System.Threading.Tasks;
using Unity.VisualScripting;
using TMPro;
using System.Collections.Generic;
using Steamworks;

public class FirebaseRoomManager : MonoBehaviour
{
    public static FirebaseRoomManager instance;

    DatabaseReference dbRef;
    private string userID = "";

    [SerializeField] private RoomCard roomCardPrefab;
    [SerializeField] private GameObject roomCardParent;

    [SerializeField] private Button hostButton; 
    [SerializeField] private Button refreshButton; 
    [SerializeField] private TMP_InputField roomNameText;
    private int maxPlayer;
    public string roomId = "";
    public string joinCode = "";
    Dictionary<string, string> roomDic = new Dictionary<string, string>();


    void Awake()
    {
        if(instance == null)
            instance = this;

        DontDestroyOnLoad(this);            
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        while(FirebaseInit.DB == null || FirebaseInit.Auth == null)
        {
            await Task.Delay(100);
        }


        dbRef = FirebaseInit.DB;

        #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
                if (SteamManager.Initialized)
                {
                    userID = Steamworks.SteamUser.GetSteamID().ToString();
                }
                else
                {
                    userID = Guid.NewGuid().ToString();
                }
        #else
                userID = FirebaseInit.Auth.CurrentUser.UserId ?? Guid.NewGuid().ToString();
        #endif

        Debug.Log($"id: {userID}");

        if(hostButton)
        {
            hostButton.onClick.AddListener(async () => { await CreateRoom(); });
        }

        if(refreshButton)
        {
            refreshButton.onClick.AddListener(RefreshRoomList);
        }

        await GetRoomList();
    }


    public async Task CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameText.text))
        {
            return;
        }

        //방 만들기
        joinCode = null;
         #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
        string nickName = SteamFriends.GetPersonaName();
         //Steam 사용 가능하면 Steam으로 호스트 시작
         if(SteamManager.Initialized)
         {
            Debug.Log("[CreateRoom] 스팀으로 호스트 시작");
            joinCode = await NetworkManager.instance.StartSteamHost();
         }
         else
         {
            Debug.LogError("[CreateRoom] 스팀이 초기화 되지 않음.");
         }
         #else
         //모바일 혹은 그외

         #endif

         if(string.IsNullOrEmpty(joinCode))
         {
            Debug.LogError("[CreateRoom] 조인 코드를 못받음");
            return;

         }


        maxPlayer = 20;

        if(roomId == "")
            roomId = Guid.NewGuid().ToString();

        //방 정보 등록
        await dbRef.Child("rooms").Child(roomId).SetRawJsonValueAsync(
            $"{{\"hostID\":\"{userID}\",\"joinCode\":\"{joinCode}\",\"roomName\":\"{roomNameText.text}\", \"maxPlayers\":\"{maxPlayer}\", \"hostName\":\"{nickName}\"}}");

        
        //방장 플레이어 등록
        await dbRef.Child("rooms").Child(roomId).Child("players").Child(userID).SetValueAsync(true);

        Debug.Log($"방생성 완료: {roomNameText.text} ({roomId}): {joinCode}: {nickName}");

        DatabaseReference roomRef = dbRef.Child("rooms").Child(roomId);

        await roomRef.OnDisconnect().RemoveValue();
    }


    public async Task GetRoomList()
    {
        if(await dbRef.Child("rooms").GetValueAsync() == null)
        {
            return;
        }

        var snapShot = await dbRef.Child("rooms").GetValueAsync();

        roomCardPrefab.gameObject.SetActive(true);

        foreach(var room in snapShot.Children)
        {
            string name = room.Child("roomName").Value.ToString();
            string hostName =  room.Child("hostName").Value.ToString();
            int maxPlayer = int.Parse(room.Child("maxPlayers").Value.ToString());
            int CurrentPlayers = (int)room.Child("players").ChildrenCount;
            string roomJoinCode = room.Child("joinCode").Value.ToString();

            RoomCard card = Instantiate(roomCardPrefab, roomCardParent.transform);

            CSteamID steamID = new CSteamID(ulong.Parse(roomJoinCode));
            int avatar = SteamFriends.GetLargeFriendAvatar(steamID);

            if(avatar != -1)
            {
                uint width, height;
                if(SteamUtils.GetImageSize(avatar, out width, out height))
                {
                    byte[] image = new byte[4* (int)width * (int)height];
                    if(SteamUtils.GetImageRGBA(avatar, image, 4 * (int)width * (int)height))
                    {
                        Texture2D avatarImage = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
                        avatarImage.LoadRawTextureData(image);
                        avatarImage.Apply();                        
                        card.SetProfileImage(avatarImage);
                    }
                }
            }

            card.SetTexts(name, hostName, CurrentPlayers, maxPlayer);
            card.OnAddButtonEvent(async () => {
                try{

                     await dbRef.Child("rooms").Child(room.Key).Child("players").Child(userID).SetValueAsync(true);
                    
                    #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
                    //스팀 아이디 형식인지 체크
                    if(ulong.TryParse(roomJoinCode, out _) && SteamManager.Initialized)
                    {
                        await NetworkManager.instance.JoinSteamRoom(roomJoinCode);
                    }
                    else
                    {
                        await NetworkManager.instance.JoinRelayRoom(roomJoinCode);
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

            Debug.Log($"방: {name} ({CurrentPlayers}/{maxPlayer} - {roomJoinCode}), 방장: {hostName}");
        }

        roomCardPrefab.gameObject.SetActive(false);
    }

    public async void RefreshRoomList()
    {
        ClearRooms();

        refreshButton.interactable = false;
        await GetRoomList();
        refreshButton.interactable = true;
    }

    public async Task JoinRoom(string roomID)
    {
        var snapShot = await dbRef.Child("room").Child(roomID).Child("players").GetValueAsync();
        int maxPlayers = int.Parse((await dbRef.Child("rooms").Child(roomID).Child("maxPlayers").GetValueAsync()).Value.ToString());
    
        if(snapShot.ChildrenCount >= maxPlayers)
        {
            Debug.Log("방참");
            return;
        }

        await dbRef.Child("rooms").Child(roomID).Child("players").Child(userID).SetValueAsync(true);



        Debug.Log("방 입장!");
    }
    public async Task DeleteRoom(string roomId)
    {
        await dbRef.Child("rooms").Child(roomId).RemoveValueAsync();
    }
    private async void OnApplicationQuit()
    {
        if (!string.IsNullOrEmpty(roomId))
        {
            var snap = await dbRef.Child("rooms").Child(roomId).Child("hostID").GetValueAsync();
            if (snap.Exists && snap.Value.ToString() == userID)
                await DeleteRoom(roomId);
        }
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
}
