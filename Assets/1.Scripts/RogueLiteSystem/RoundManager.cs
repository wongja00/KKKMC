using UnityEngine;
using System.Collections.Generic;
using Mirror;
using System.Collections;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance;
    
    [SerializeField]
    private DungeonGenerator dungeonGenerator;
    private RoundUI roundUI;

    private int maxRoom = 0;

    //[SyncVar]
    public int curRoom = 0;

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }

        roundUI = InteractUIManager.Instance.roundUI;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [ClientRpc]
    public void StartRound()
    {
        roundUI.AddRoundCount();
    }

    public void EndRound()
    {
        
    }

    
    public void SetRooms(int Rooms)
    {
        maxRoom = Rooms;
    }

    [Command(requiresAuthority = false)]
    public void ClearRoom()
    {
        if(dungeonGenerator == null) return;

        if(dungeonGenerator.dungeonState == DungeonState.completed)
        {
            curRoom++;

            if(curRoom >= maxRoom)
            {
                StartCoroutine(ClearAndMakeDungeon());
            }
        }
    }

    [Server]
    IEnumerator MakeDugeon()
    {
        dungeonGenerator.MakeDungeon();

        yield return null;

        SetPlayerPos();
    }

    [ClientRpc]
    void SetPlayerPos()
    {
        StartCoroutine(WaitAndSetPos());
    }

    IEnumerator WaitAndSetPos()
    {
        yield return new WaitUntil(() => dungeonGenerator.dungeonState == DungeonState.completed);

        yield return new WaitForSecondsRealtime(3f);

        // 1. 물리 엔진 개입을 막기 위해 CharacterController 잠시 끄기
        Transform playerObj = NetworkClient.localPlayer.transform;

        CharacterController cc = playerObj.GetComponent<CharacterController>();
        if (cc != null) 
        {
            cc.enabled = false;
        }

        // 2. 안전하게 위치 이동
        playerObj.transform.position = new Vector3(0, 2, 0);

        // (참고: 만약 Rigidbody를 사용 중이라면 관성 때문에 날아가지 않도록 속도 초기화)
        Rigidbody rb = playerObj.GetComponent<Rigidbody>();
        if (rb != null) 
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 3. 물리 엔진이 새 위치를 인식할 수 있게 물리 프레임(0.02초) 한 번 대기
        yield return new WaitForFixedUpdate();

        // 4. 다시 조작 가능하게 켜기
        if (cc != null) 
        {
            cc.enabled = true;
        }
    }

    [Server]
    IEnumerator ClearAndMakeDungeon()
    {
        dungeonGenerator.ServerClearDungeon();
        dungeonGenerator.wasSeeding = false;

        
        yield return new WaitForSeconds(2f);
                
        StartCoroutine(MakeDugeon());

        StartRound();
        yield return null;
    }
}
