using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Unity.AI.Navigation;
using UnityEngine;

public enum DungeonState{inactive, generatingMain, generatingBranches, cleanup, completed}

public class DungeonGenerator : NetworkBehaviour
{
    [Header("내비")]
    [SerializeField] private NavMeshSurface surface;
    [Header("시작점")]
    public NetworkStartPosition startPos;

    [Header("디버깅 옵션")]
    public bool useBacktracking = false;
    public bool useBoxcollider;
    public bool useLightForDebug;
    public bool restoreLightsAfterDebug;
    public KeyCode toggleMapKey = KeyCode.M;
    public Color startLightColor = Color.white;


    [Header("방갯수")]
    [Range(2, 100)][SerializeField] int mainLength = 10;

    [Header("사이드방 갯수(얼마나 깊게 만들건지)")]
    [Range(0, 50)][SerializeField] int branchLength = 5;

    [Header("사이드방 갯수(얼마나 채울건지)")]
    [Range(0, 25)][SerializeField] int numBranches = 10;

    [Header("문 확률")]
    [Range(0, 100)][SerializeField] int doorPercent = 25;

    [Header("딜레이")]
    [Range(0, 1f)][SerializeField] float constructionDelay = 0;

    [Header("타일셋팅")]
    //타일
    [SerializeField] GameObject[] roomPrefabs;
    [SerializeField] GameObject[] hallwayPrefabs;
    //시작 타일
    [SerializeField] GameObject[] startPrefabs;
    [SerializeField] GameObject[] exitPrefabs;
    [SerializeField] GameObject[] blockedPrefabs;
    [SerializeField] GameObject[] doorPrefabs;
    [SerializeField] GameObject dungeonControllerPrefab;
    [SerializeField] GameObject barrierObject;

    [Header("만든 타일")]
    [SerializeField] List<Tile> generatedTiles = new List<Tile>();

    [SerializeField] List<Connector> availableConnectors = new List<Connector>();
    [SerializeField] List<DungeonController> dungeonControllers = new List<DungeonController>();
    [SerializeField] List<GameObject> doors = new List<GameObject>();
    public DungeonState dungeonState = DungeonState.inactive;

    [Header("몇 번 시도할지")]
    [SerializeField]private int maxAttemps = 15;
    int attempt = 0;

    Transform tileFrom;
    Transform tileTo;
    Transform tileRoot;
    Transform container;
    
    [SyncVar(hook = nameof(OnChangeseed))]
    public int dungeonSeed = 0;

    [SyncVar]
    public bool wasSeeding = false;
    
    DungeonRNG rng;
    
    void Start()
    {
        NetworkManager.instance.OnPlayerJoin += MakeDungeonEvent;
    }

    [Server] 
    public void ServerClearDungeon()
    {
        foreach (var controller in dungeonControllers)
        {
            if (controller != null) NetworkServer.Destroy(controller.gameObject);
        }
        dungeonControllers.Clear();

        foreach (var door in doors)
        {
            if (door != null) NetworkServer.Destroy(door);
        }
        doors.Clear();

        ClearLocalObjects();
        dungeonState = DungeonState.inactive;

        RpcClearClientVisuals(); 
    }

    [ClientRpc]
    private void RpcClearClientVisuals()
    {
        ClearLocalObjects();
        
        dungeonState = DungeonState.inactive;
        
        if (surface != null)
        {
            surface.RemoveData();
        }
    }

    private void ClearLocalObjects()
    {
        foreach (var tile in generatedTiles)
        {
            if (tile != null) Destroy(tile.tile.gameObject);
        }
        generatedTiles.Clear();

        foreach (var conn in availableConnectors)
        {
            if (conn != null) Destroy(conn.gameObject);
        }
        availableConnectors.Clear();

        foreach (Transform child in transform.GetComponentInChildren<Transform>())
        {
            if (child != null) Destroy(child.gameObject);
        }
    }

    void MakeDungeonEvent()
    {
        MakeDungeon();
    }

    [Server]
    public void MakeDungeon()
    {      
        Debug.Log($"던전 시도{dungeonState.ToString()}-{transform.childCount}");
        //현재 던전이 안만들어진 상태에서만 생성
        if(dungeonState != DungeonState.inactive && transform.childCount > 0)
            return;

        if(wasSeeding == false)
        {
            dungeonSeed = Random.Range(int.MinValue, int.MaxValue);
            
            wasSeeding = true;
        }
    }

    void OnChangeseed(int OldSeed, int NewSeed)
    {
        rng = new DungeonRNG(NewSeed);
        RpcSetDungeonSeed(NewSeed);
    }

    void RpcSetDungeonSeed(int seed)
    {        
        //InitState(seed);

        //Debug.Log("시드: "+seed);
        
        StartCoroutine(DungeonBuild());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator DungeonBuild()
    {
        GameObject goContainer = new GameObject("MainPath");
        container = goContainer.transform;
        container.SetParent(transform);

        tileRoot = CreateStartRoom();
        SetStartPos(tileRoot);
        
        DebugRoomLighting(tileRoot, Color.cyan);
        tileTo = tileRoot;
        dungeonState = DungeonState.generatingMain;
        
        //tileTo.SetParent(container);

        for(int i = 0; i < mainLength - 1; i++)
        {
            yield return new WaitForSeconds(constructionDelay);
            tileFrom = tileTo;
            tileTo = CreateTile();
            DebugRoomLighting(tileTo, Color.blue);
            tileTo.SetParent(container);
            ConnectTiles();
            while(isCollision()== true && attempt < maxAttemps*1.5f)
            {
                //Debug.Log("메인재시도");
                Coroutine co = StartCoroutine(CollisionCheck());

                yield return co;
            }
            attempt = 0;
        }

        //마지막이 복도면 삭제
        if(hallwayPrefabs.Any(tile => tile.name == tileTo.name))
        {
            int toIndex = generatedTiles.FindIndex(x => x.tile == tileTo);
            if(generatedTiles[toIndex].connector != null)
            {
                generatedTiles[toIndex].connector.isConnected = false;
                //availableConnectors.RemoveAll(x => x.transform.parent.parent == tileTo);
            }
            generatedTiles.RemoveAt(toIndex);

            DestroyImmediate(tileTo.gameObject);
        }


        //get all connectors within container
        foreach(Connector connector in container.GetComponentsInChildren<Connector>())
        {
            if(connector.isConnected == false)
            {
                if(availableConnectors.Contains(connector) == false)
                    availableConnectors.Add(connector);
            }
        }

        //branching
        dungeonState = DungeonState.generatingBranches;
        for(int b = 0; b < numBranches; b++)
        {
            if(availableConnectors.Count > 0)
            {
                goContainer = new GameObject("Branch" + (b + 1).ToString());
                container = goContainer.transform;
                container.SetParent(transform);
                int availIndex = rng.Range(0, availableConnectors.Count);
                tileRoot = availableConnectors[availIndex].transform.parent.parent;
                availableConnectors.RemoveAt(availIndex);
                tileTo = tileRoot;

                //깊이이
                for(int i = 0; i < branchLength - 1; i++)
                {
                    yield return new WaitForSeconds(constructionDelay);
                    tileFrom = tileTo;
                    tileTo = CreateTile();
                    DebugRoomLighting(tileTo, Color.yellow);
                    ConnectTiles();
                    while(isCollision() == true && attempt < maxAttemps*1.5f)
                    {
                        //Debug.Log("브렌치재시도");
                        Coroutine co = StartCoroutine(CollisionCheck());

                        yield return co;
                    }
                    attempt = 0;
                }

                //마지막이 복도면 삭제
                if(tileTo != null)
                {
                    if(hallwayPrefabs.Any(tile => tile.name == tileTo.name) && branchLength > 1)
                    {
                        int toIndex = generatedTiles.FindIndex(x => x.tile == tileTo);
                        if(generatedTiles[toIndex].connector != null)
                        {
                            generatedTiles[toIndex].connector.isConnected = false;
                            availableConnectors.RemoveAll(x => x.transform.parent.parent == tileTo);
                        }
                        generatedTiles.RemoveAt(toIndex);

                        DestroyImmediate(tileTo.gameObject);

                        tileTo = tileFrom;
                    }
                }
            }
            else
                break;
        }

        BlockedPassage();

        if(NetworkServer.active)
        {
            SpawnDoors();
            SetDungeonController();
        }
        
        RoundManager.Instance.SetRooms(dungeonControllers.Count - 1);


        dungeonState = DungeonState.cleanup;
        LightsRestoration();
        CleanupBoxed();

        yield return new WaitForSeconds(0.3f);
        surface.BuildNavMesh();
        dungeonState = DungeonState.completed;

        yield return null;

    }

    void SpawnDoors()
    {
        if(doorPercent > 0)
        {
            Connector[] allConnector = transform.GetComponentsInChildren<Connector>();
            for(int i = 0; i < allConnector.Length; i ++)
            {
                Connector myConnector = allConnector[i];

                if(myConnector.isConnected)
                {
                    //문 소환할 랜덤 확률
                    int roll = rng.Range(1, 101);
                    if(roll <= doorPercent)
                    {
                        Vector3 halfExtents = new Vector3(myConnector.size.x, 1f, myConnector.size.x);
                        Vector3 pos = myConnector.transform.position;
                        Vector3 offset = Vector3.up * 0.5f;

                        Collider[] hits = Physics.OverlapBox(pos + offset, halfExtents, Quaternion.identity, LayerMask.GetMask("Door"));
                        if(hits.Length == 0)
                        {
                            int doorIndex = rng.Range(0, doorPrefabs.Length);
                            GameObject goDoor = Instantiate(doorPrefabs[doorIndex], pos, myConnector.transform.rotation) as GameObject;
                                
                            goDoor.transform.Rotate(-90, 0, 0);
                            goDoor.name = doorPrefabs[doorIndex].name;
                            
                            if(goDoor.GetComponent<NetworkIdentity>() && isServer)
                                NetworkServer.Spawn(goDoor);

                            doors.Add(goDoor);
                        }
                    }
                }
            }
        }
    }

    void BlockedPassage()
    {
        foreach(Connector con in transform.GetComponentsInChildren<Connector>())
        {
            if(con.isConnected == false)
            {   
                Vector3 pos = con.transform.position;
                int wallIndex = rng.Range(0, blockedPrefabs.Length);

                GameObject goWall = Instantiate(blockedPrefabs[wallIndex] , con.transform) as GameObject;

                goWall.name = blockedPrefabs[wallIndex].name;

            }
        }
    }

    void LightsRestoration()
    {
        if(useLightForDebug == false || restoreLightsAfterDebug == false || Application.isEditor == false)return;

        Light[] lights = transform.GetComponentsInChildren<Light>();

        foreach(Light light in lights)
        {
            light.color = startLightColor;
        }
    }
    void CleanupBoxed()
    {
        if(useBoxcollider == true) return;

        foreach(Tile myTile in generatedTiles)
        {
            BoxCollider box = myTile.tile.GetComponent<BoxCollider>();
            if(box != null) {Destroy(box);}
        }
    }

    void DebugRoomLighting(Transform tile, Color lightColor)
    {
        if(useLightForDebug == false || Application.isEditor == false) return;

        Light[] lights = tile.GetComponentsInChildren<Light>();

        if(lights.Length > 0)
        {    
            if(startLightColor == Color.white)
            {
                startLightColor = lights[0].color;
            }
            foreach(Light light in lights)
            {
                light.color = lightColor;
            }
        }
    }

    IEnumerator CollisionCheck()
    {
        attempt++;

        //마지막으로 만든 붙여야 되는 타일
        int toIndex = generatedTiles.FindIndex(x => x.tile == tileTo);
        if(generatedTiles[toIndex].connector != null)
        {
            //다시 뗌
            generatedTiles[toIndex].connector.isConnected = false;
        }
        
        //만든타일에서 없앰
        generatedTiles.RemoveAt(toIndex);
        DestroyImmediate(tileTo.gameObject);

        //역추적 -백트래킹
        if(attempt >= maxAttemps && useBacktracking == true)
        {
            //붙여야 되는곳
            int fromIndex = generatedTiles.FindIndex(x => x.tile == tileFrom);

            if(fromIndex == -1)
            {
                fromIndex = 1;
            }

            Tile myTileFrom = generatedTiles[fromIndex];
            
            //타일이 루트(시작적)이 아닐떄
            if(tileFrom != tileRoot)
            {
                //붙은곳
                if(myTileFrom.connector != null)
                {
                    //떨어짐
                    myTileFrom.connector.isConnected = false;
                }

                //백트래킹을 위한 이전 타일 삭제
                availableConnectors.RemoveAll(x => x.transform.parent.parent == tileFrom);
                generatedTiles.RemoveAt(fromIndex);
                DestroyImmediate(tileFrom.gameObject);

                //(메인 혹은 브렌치의)시작점이 아닐떄
                if(myTileFrom.origin != tileRoot)
                {
                    //불일곳을 이전에 붙인곳으로 대입함
                    tileFrom = myTileFrom.origin;
                }
                //메인 루트이자 붙여진곳이 시작점일때
                else if(container.name.Contains("Main"))
                {
                    //현재 붙여진 곳이 있을떄
                    if(myTileFrom.origin != null)
                    {
                        //시작점은 현재 타일의 붙여진곳
                        tileRoot = myTileFrom.origin;
                        //마직막으로 붙여진곳은 시작점
                        tileFrom = tileRoot;
                    }
                }
                //붙일곳이 있을때, 브렌치일때
                else if(availableConnectors.Count > 0)
                { 
                    //Debug.Log($"{availableConnectors.Count} {attempt}");

                    //붙일곳중 랜덤
                    int availIndex = rng.Range(0, availableConnectors.Count);
                    //시작점은 붙일수 있는곳의 타일
                    tileRoot = availableConnectors[availIndex].transform.parent.parent;
                    availableConnectors.RemoveAt(availIndex);
                    tileFrom = tileRoot;                      
                }
            }
            else if(container.name.Contains("Main"))
            {
                if(myTileFrom.origin != null)
                {
                    tileRoot = myTileFrom.origin;
                    tileFrom = tileRoot;
                }
            }
            //연결할수 있는게 있을때
            else if(availableConnectors.Count > 0)
            {   
                //Debug.Log($"{availableConnectors.Count} {attempt}");
                //랜덤으로 하나
                int availIndex = rng.Range(0, availableConnectors.Count);
                tileRoot = availableConnectors[availIndex].transform.parent.parent;
                availableConnectors.RemoveAt(availIndex);
                tileFrom = tileRoot;                     
            }
            //else tileTo = tileFrom;
        }
        //else return;
        //재시도
        if(tileFrom != null)
        {
            if(container.name.Contains("Main"))
            {
                //붙일 타일 만듬
                tileTo = CreateTile();
                //색바꿈꿈
                Color retryColor = container.name.Contains("Branch") ? Color.green : Color.yellow;
                DebugRoomLighting(tileTo, retryColor * 2);

                ConnectTiles();
            }
            else if(availableConnectors.Count > 0)
            {
                //Debug.Log($"{availableConnectors.Count} {attempt}");

                //붙일 타일 만듬
                tileTo = CreateTile();
                //색바꿈꿈
                Color retryColor = container.name.Contains("Branch") ? Color.green : Color.yellow;
                DebugRoomLighting(tileTo, retryColor * 2);

                ConnectTiles();
            }
        }

        yield return new WaitForSeconds(constructionDelay);
    }

    bool isCollision()
    {        
        Physics.SyncTransforms();

        if(tileTo == null) return false;

        BoxCollider box = tileTo.GetComponent<BoxCollider>();

        if(box == null) return false;

        Vector3 offset = (tileTo.right * box.center.x)+(tileTo.up * box.center.y)+(tileTo.forward * box.center.z);

        //Collider[] hits = Physics.OverlapBox(tileTo.transform.position + offset, box.bounds.extents*1.4f, tileTo.rotation, LayerMask.GetMask("Tile"));
        // AABB 교차 검사 방식으로 변경
        bool isOverlapping = false;
        foreach (var otherTile in generatedTiles)
        {
            if (otherTile.tile == tileTo || otherTile.tile == tileFrom)
                continue;

            BoxCollider otherBox = otherTile.tile.GetComponent<BoxCollider>();
            if (otherBox == null)
                continue;

            Bounds a = box.bounds;
            Bounds b = otherBox.bounds;

            if (a.Intersects(b))
            {
                isOverlapping = true;
                break;
            }
        }

        //bool isColl = hits.Any(x => x.transform != tileTo && x.transform != tileFrom);

        return isOverlapping;
    }

    Transform CreateStartRoom()
    {
        //Quaternion rotation = Quaternion.Euler(0, 0, 0);

        int index = rng.Range(0, startPrefabs.Length);
        GameObject goTile = Instantiate(startPrefabs[index], Vector3.zero, startPrefabs[index].transform.rotation, container) as GameObject;
        goTile.name = "Start Room";
        //float yRot = (int)Range(0,4) * 90f;
        //goTile.transform.Rotate(0, yRot, 0);

        //add to tilelist
        generatedTiles.Add(new Tile(goTile.transform, null));
        return goTile.transform;
    }

    Transform CreateTile()
    {
        if(tileFrom == null)
            return null;

        int index = 0;
        GameObject tile;
        if (roomPrefabs.Any(prefab => prefab.name == tileFrom.name))
        {
            index = rng.Range(0, hallwayPrefabs.Length);
            tile = hallwayPrefabs[index];
        }
        else
        {
            index = rng.Range(0, roomPrefabs.Length);
            tile = roomPrefabs[index];
        }


        GameObject goTile = Instantiate(tile, Vector3.zero, tile.transform.rotation, container) as GameObject;
        goTile.name = tile.name;

        //Transform origin = generatedTiles.Find(x => x.tile == tileFrom).tile;

        //add to tilelist
        generatedTiles.Add(new Tile(goTile.transform, tileFrom));
        return goTile.transform;
    }

    void ConnectTiles()
    {
        Transform connectFrom = GetRandomConnector(tileFrom);
        if(connectFrom == null) return;

        Transform connectTo = GetRandomConnector(tileTo);
        if(connectTo == null) return;

        connectTo.SetParent(connectFrom);
        tileTo.SetParent(connectTo);
        connectTo.localPosition = Vector3.zero;
        connectTo.localRotation = Quaternion.Euler(0, 0, 0);
        connectTo.Rotate(0, 180f, 0);

        tileTo.SetParent(container);
        connectTo.SetParent(tileTo.Find("Connectors"));
        generatedTiles.Last().connector = connectFrom.GetComponent<Connector>();
    }

    Transform  GetRandomConnector(Transform from)
    {
        if(from == null) return null;
        List<Connector> connectorList = from.GetComponentsInChildren<Connector>().ToList().FindAll(x => x.isConnected == false);

        if(connectorList.Count > 0)
        {
            int connectorIndex = rng.Range(0, connectorList.Count);
            connectorList[connectorIndex].isConnected = true;
            if(from == tileFrom)
            {
                BoxCollider box = from.GetComponent<BoxCollider>();

                if(box == null)
                {
                    box = from.gameObject.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                }
            }
            return connectorList[connectorIndex].transform;
        }

        return null;
    }

    private void SetStartPos(Transform start)
    {
        startPos.transform.position = start.transform.position;
        startPos.transform.LookAt(start.GetComponentInChildren<Connector>().transform);
    }

    void SetDungeonController()
    {
        for(int  i = 0; i < generatedTiles.Count; i++)
        {
            if(hallwayPrefabs.Any(x => x.name ==generatedTiles[i].tile.name))
            {
                continue;
            }
            

            if(generatedTiles[i].tile.GetComponent<BoxCollider>() != null)
            {
                GameObject conObj = Instantiate(dungeonControllerPrefab, generatedTiles[i].tile.position, generatedTiles[i].tile.rotation);
                DungeonController con = conObj.GetComponent<DungeonController>();

                BoxCollider srcBox = generatedTiles[i].tile.GetComponent<BoxCollider>();
                BoxCollider dstBox = con.GetComponent<BoxCollider>();
                if(srcBox != null && dstBox != null)
                {
                    dstBox.center = srcBox.center;
                    dstBox.size = srcBox.size;
                }

                if(i == 0)
                {
                    con.roomType = RoomType.Start;
                }

                con.barrierPrefab = barrierObject;

                GameObject sp = new GameObject("SpawnPoint");

                sp.transform.SetParent(con.transform);
                sp.transform.localPosition = Vector3.zero;
                con.spawnPoints.Add(sp.transform);

                //라운드용 이벤트 추가
                con.OnEndCombat += RoundManager.Instance.ClearRoom;
                
                NetworkServer.Spawn(conObj);
                dungeonControllers.Add(con);
                
            }
        }
    }
}
public class DungeonRNG
{
    private System.Random rng;

    public DungeonRNG(int seed)
    {
        rng = new System.Random(seed);
    }

    public int Range(int min, int max)
    {
        return rng.Next(min, max);
    }
}