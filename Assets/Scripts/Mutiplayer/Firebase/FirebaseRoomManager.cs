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

        userID = FirebaseInit.Auth.CurrentUser.UserId ?? Guid.NewGuid().ToString();
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
        joinCode = await NetworkManager.instance.StartRelayHost();
        // 아이피 주소를 방 정보에 등록
        string ipAddress = System.Net.Dns.GetHostName();

        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                Console.WriteLine(ip.ToString());
                ipAddress = ip.ToString();
            }
        }

        maxPlayer = 20;

        if(roomId == "")
            roomId = Guid.NewGuid().ToString();

        //방 정보 등록
        await dbRef.Child("rooms").Child(roomId).SetRawJsonValueAsync(
            $"{{\"hostID\":\"{userID}\",\"joinCode\":\"{ipAddress}\",\"roomName\":\"{roomNameText.text}\", \"maxPlayers\":{maxPlayer}}}");

        
        //방장 플레이어 등록
        await dbRef.Child("rooms").Child(roomId).Child("players").Child(userID).SetValueAsync(true);

        Debug.Log($"방생성 완료: {roomNameText.text} ({roomId})");

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
            string hostName = room.Child("hostID").Value.ToString();
            int maxPlayer = int.Parse(room.Child("maxPlayers").Value.ToString());
            int CurrentPlayers = (int)room.Child("players").ChildrenCount;
            string roomJoinCode = room.Child("joinCode").Value.ToString();

            RoomCard card = Instantiate(roomCardPrefab, roomCardParent.transform);
            card.SetTexts(name, hostName, CurrentPlayers, maxPlayer);
            card.OnAddButtonEvent(async () => {
                     await dbRef.Child("rooms").Child(room.Key).Child("players").Child(userID).SetValueAsync(true);
                     await NetworkManager.instance.JoinRelayRoom(roomJoinCode);
                  });

            Debug.Log($"방: {name} ({CurrentPlayers}/{maxPlayer} - {roomJoinCode})");
        }

        roomCardPrefab.gameObject.SetActive(false);
    }

    public async void RefreshRoomList()
    {
        ClearRooms();
        await GetRoomList();
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
