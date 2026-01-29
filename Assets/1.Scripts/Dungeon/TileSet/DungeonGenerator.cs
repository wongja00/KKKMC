using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class DungeonGenerator : NetworkBehaviour
{
    [Header("내비")]
    [SerializeField] private NavMeshSurface surface;
    [Header("시작점")]
    public NetworkStartPosition startPos;

    [Header("디버깅 옵션")]
    public bool useBoxcollider;
    public bool useLightForDebug;
    public bool restoreLightsAfterDebug;
    public KeyCode toggleMapKey = KeyCode.M;
    public Color startLightColor = Color.white;


    [Header("방갯수")]
    [Range(2, 100)]public int mainLength = 10;

    [Header("사이드방 갯수(얼마나 깊게 만들건지)")]
    [Range(0, 50)]public int branchLength = 5;

    [Header("사이드방 갯수(얼마나 채울건지)")]
    [Range(0, 25)]public int numBranches = 10;

    [Header("문 확률")]
    [Range(0, 100)]public int doorPercent = 25;

    [Header("딜레이")]
    [Range(0, 1f)]public float constructionDelay = 0;

    [Header("타일셋팅")]
    //타일
    public GameObject[] tilePrefabs;
    //시작 타일
    public GameObject[] startPrefabs;
    public GameObject[] exitPrefabs;
    public GameObject[] blockedPrefabs;
    public GameObject[] doorPrefabs;

    [Header("만든 타일")]
    public List<Tile> generatedTiles = new List<Tile>();

    public List<Connector> availableConnectors = new List<Connector>();

    [Header("몇 번 시도할지")]
    [SerializeField]private int maxAttemps = 15;
    int attempt = 0;

    Transform tileFrom;
    Transform tileTo;
    Transform tileRoot;
    Transform container;


    Coroutine coroutine;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
        
        //tileTo.SetParent(container);

        for(int i = 0; i < mainLength - 1; i++)
        {
            yield return new WaitForSeconds(constructionDelay);
            tileFrom = tileTo;
            tileTo = CreateTile();
            DebugRoomLighting(tileTo, Color.blue);
            tileTo.SetParent(container);
            ConnectTiles();
            CollisionCheck();
            if(attempt >= maxAttemps) break;
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
        for(int b = 0; b < numBranches; b++)
        {
            if(availableConnectors.Count > 0)
            {
                goContainer = new GameObject("Branch" + (b + 1).ToString());
                container = goContainer.transform;
                container.SetParent(transform);
                int availIndex = Random.Range(0, availableConnectors.Count);
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
                    CollisionCheck();
                    if(attempt >= maxAttemps) break;
                }
            }
            else
                break;
        }
        LightsRestoration();
        CleanupBoxed();

        surface.BuildNavMesh();
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

    void CollisionCheck()
    {
        //마지막으로 새운 타일 기준
        BoxCollider box = tileTo.GetComponent<BoxCollider>();

        if(box == null)
        {
            box = tileTo.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
        }

        //로컬기준 중심점
        Vector3 offset = (tileTo.right * box.center.x) + (tileTo.up * box.center.y) + (tileTo.forward * box.center.z);
        //반너비(중심을 기준으로 양옆으로 늘어나기 때문에)
        Vector3 halfExtends = box.bounds.extents;
        List<Collider> hits = Physics.OverlapBox(tileTo.position + offset, halfExtends, tileTo.rotation, LayerMask.GetMask("Tile")).ToList();

        if(hits.Count > 0)
        {
            //붙일거끼리가 아닐떄
            if(hits.Exists(x => x.transform != tileFrom && x.transform != tileTo))
            {
                //붙일것도 아닌거거에 충돌 헀을떄
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
                Destroy(tileTo.gameObject);

                //역추적
                //최대치만큼 시도
                if(attempt >= maxAttemps)
                {
                    //붙여야 되는곳
                    int fromIndex = generatedTiles.FindIndex(x => x.tile == tileFrom);
                    Tile myTileFrom = generatedTiles[fromIndex];
                    
                    //타일이 루트(시작적)이 아닐떄
                    if(tileFrom != tileRoot)
                    {
                        //붙여야 되는곳이 있을때
                        if(myTileFrom.connector != null)
                        {
                            //붙인거 뗌
                            myTileFrom.connector.isConnected = false;
                        }

                        //아직 연결안됀 커넥터들 중  부모의부모 즉 타일이 붙여야돼는 곳의 타일이면 지움(왜냐면 붙일수 없는 연결점이기 때문에에)
                        availableConnectors.RemoveAll(x => x.transform.parent.parent == tileFrom);
                        //타일 없앰
                        generatedTiles.RemoveAt(fromIndex);
                        Destroy(tileFrom.gameObject);

                        //시작점이 아닐떄
                        if(myTileFrom.origin != tileRoot)
                        {
                            //불일곳을 이전에 붙인곳으로 대입함
                            tileFrom = myTileFrom.origin;
                        }
                        //메인 루트일이자 붙여진곳이 시작점일때
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
                        //붙일곳이 있을때때
                        else if(availableConnectors.Count > 0)
                        { 
                            //붙일곳중 랜덤
                            int availIndex = Random.Range(0, availableConnectors.Count);
                            //시작점은 붙일수 있는곳의 타일
                            tileRoot = availableConnectors[availIndex].transform.parent.parent;
                            //그리고 지움
                            availableConnectors.RemoveAt(availIndex);
                            //시작점에 붙인거임
                            tileFrom = tileRoot;                      
                        }
                        //붙일수 있는곳이 없을떄
                        else return;
                    }
                    else if(container.name.Contains("Main"))
                    {
                        if(myTileFrom.origin != null)
                        {
                            tileRoot = myTileFrom.origin;
                            tileFrom = tileRoot;
                        }
                    }
                    //연결할수 있는게 있을때때
                    else if(availableConnectors.Count > 0)
                    {   
                        //랜덤으로 하나나
                        int availIndex = Random.Range(0, availableConnectors.Count);
                        tileRoot = availableConnectors[availIndex].transform.parent.parent;
                        availableConnectors.RemoveAt(availIndex);
                        tileFrom = tileRoot;                     
                    }
                    else return;
                }
                //else return;
                //재시도(재귀)
                if(tileFrom != null)
                {
                    //붙일 타일 만듬
                    tileTo = CreateTile();
                    //색바꿈꿈
                    Color retryColor = container.name.Contains("Branch") ? Color.green : Color.yellow;
                    DebugRoomLighting(tileTo, retryColor * 2);
                    //연결하고 또 콜라이더 체크
                    ConnectTiles();
                    CollisionCheck();
                }
            }
            else
            {
                attempt = 0;//타일 붙일때 아무것도 닿은게 없을 때
            }
        }

    }

    Transform CreateStartRoom()
    {
        Quaternion rotation = Quaternion.Euler(0, 0, 0);

        int index = Random.Range(0, startPrefabs.Length);
        GameObject goTile = Instantiate(startPrefabs[index], Vector3.zero, startPrefabs[index].transform.rotation, container) as GameObject;
        goTile.name = "Start Room";
        //float yRot = (int)Random.Range(0,4) * 90f;
        //goTile.transform.Rotate(0, yRot, 0);

        //add to tilelist
        generatedTiles.Add(new Tile(goTile.transform, null));
        return goTile.transform;
    }

    Transform CreateTile()
    {
        Quaternion rotation = Quaternion.Euler(0, 0, 0);
        int index = Random.Range(0, tilePrefabs.Length);
        GameObject goTile = Instantiate(tilePrefabs[index], Vector3.zero, tilePrefabs[index].transform.rotation, container) as GameObject;
        goTile.name = tilePrefabs[index].name;

        Transform origin = generatedTiles.Find(x => x.tile == tileFrom).tile;

        //add to tilelist
        generatedTiles.Add(new Tile(goTile.transform, origin));
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
            int connectorIndex = Random.Range(0, connectorList.Count);
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
}