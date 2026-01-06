using UnityEngine;
using System.Collections.Generic;
using Mirror;
using Unity.AI.Navigation;
using System.Collections;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField]
    private NetworkStartPosition startPos;

    [Header("Dungeon Size")]
    public int dungeonWidth = 300;
    public int dungeonHeight = 300;    
    public int roomMargin = 3;
    
    [Header("Dungeon Settings")]
    public int roomCount = 15;
    public int roomThreshold = 50;
    public Vector2Int roomMinSize = new Vector2Int(32, 32);
    public Vector2Int roomMaxSize = new Vector2Int(128, 128);

    public int seed;
    public bool useRandomSeed = true;

    private List<Room> rooms = new List<Room>();
    private List<Edge> edges = new List<Edge>();
    Dictionary<Room, Room> parent = new Dictionary<Room, Room>();
    private List<(Vector2Int from, Vector2Int to, Vector2Int dirA, Vector2Int dirB)> conrridors
        = new List<(Vector2Int from, Vector2Int to, Vector2Int dirA, Vector2Int dirB)>();

    private List<(Vector2Int cell, Vector2Int dir)> doorRequests
        = new List<(Vector2Int cell, Vector2Int dir)>();//셀 - 방 바깍쪽 복또, 디 - 방에서 뾲뚀로!

    int[,] map;

    [SerializeField] GameObject floorPrefab;
    [SerializeField] GameObject wallPrefab;
    [SerializeField] Transform dungeonRoot;
    [SerializeField] GameObject doorPrefab;
    [SerializeField] Transform doorRoot;
    [SerializeField] GameObject barrierPrefab;

    Vector2Int[] directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    class BSPNode
    {
        public RectInt area;
        public BSPNode left;
        public BSPNode right;
        public RectInt? room;

        public BSPNode(RectInt area)
        {
            this.area = area;
        }

        public bool IsLeaf => left == null && right == null;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //GenerateRooms();
        //AssignRoomTypes();
        //ConnectRoomsWithMST();

        rooms.Clear();

        BSPNode root = new BSPNode(new RectInt(1, 1, dungeonWidth - 2, dungeonHeight - 2));

        Split(root, depth: 4);
        CreateRoomsFromBSP(root);
        AssignRoomTypes();

        InitMap();
        CarveRooms();
        doorRequests.Clear();
        //CarveAllCorridors();

        GetAnyRoom(root);

        //BuildFloor3D();
        StartCoroutine(BuildWalls3D());
        //BuildDoors3D();
        //BuildDoorsFromRequests();

        BuildRoomControllers();

        dungeonRoot.GetComponent<NavMeshBuilder>().NavMeshbuilding();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
        //사각형을 쪼갬
    void Split(BSPNode node, int depth)
    {
        if(depth <= 0) return;

        bool horizontal = Random.value < 0.5f;

        int max = (horizontal ? node.area.height : node.area.width) - roomThreshold;
        if(max <= roomThreshold) return;

        if(horizontal && node.area.width - node.area.height >= 50)
        {
            horizontal = false;
        }
        if(!horizontal && node.area.height - node.area.width >= 50)
        {
            horizontal = true;
        }

        int split = Random.Range(roomThreshold, max);

        if(horizontal)
        {
            node.left = new BSPNode(
                new RectInt(node.area.x, node.area.y, node.area.width, split)
            );
            node.right = new BSPNode(
                new RectInt(node.area.x, node.area.y + split, node.area.width, node.area.height - split)
            );
        }
        else
        {
            node.left = new BSPNode(
                new RectInt(node.area.x, node.area.y, split, node.area.height)
            );
            node.right = new BSPNode(
                new RectInt(node.area.x + split, node.area.y , node.area.width - split, node.area.height)
            );
        }

        Split(node.left, depth - 1);
        Split(node.right, depth - 1);
    }

    //쪼갠 사각형을 바탕으로 방으로 만듬
    void CreateRoomsFromBSP(BSPNode node)
    {
        if(!node.IsLeaf)
        {
            CreateRoomsFromBSP(node.left);
            CreateRoomsFromBSP(node.right);
            return;
        }

        int margin = 4;//딱붙어있지 않게 마진을 남김

        int w = Random.Range(node.area.width / 2, node.area.width - margin);
        int h = Random.Range(node.area.height / 2, node.area.height - margin);

        int x = Random.Range(node.area.x + margin, node.area.xMax - w - margin);
        int y = Random.Range(node.area.y + margin, node.area.yMax - h - margin);

        RectInt roomRect = new RectInt(x, y, w, h);
        node.room = roomRect;

        rooms.Add(new Room(roomRect));
    }

    //방끼리 이어줌
    RectInt GetAnyRoom(BSPNode node)
    {
        if(node.IsLeaf) return node.room.Value;
        
        RectInt a = GetAnyRoom(node.left);
        RectInt b = GetAnyRoom(node.right);

        var (exitA, dirA) = GetRoomExit(a, Vector2Int.RoundToInt(b.center));
        var (exitB, dirB) = GetRoomExit(b, Vector2Int.RoundToInt(a.center));

        CarveCorridor(
            exitA,
            exitB,
            dirA
        );

        return a;
    }

    private struct DoorCandidate
    {
        public Vector2Int roomCell; //방 내부의 바닥 셀
        public Vector2Int corridorCell; // 방 밖(복도) 바닥 셀
        public Vector2Int dir; //roomCell -> corridorCell 방향

        public DoorCandidate(Vector2Int r, Vector2Int c, Vector2Int d)
        {
            roomCell = r;
            corridorCell = c;
            dir = d;
        }

    }
    
    private void GenerateRooms()
    {
        rooms.Clear();

        if(useRandomSeed)
            seed = Random.Range(0, int.MaxValue);

        Random.InitState(seed);

        for(int i = 0; i < roomCount; i++)
        {
            int width = Random.Range(roomMinSize.x, roomMaxSize.x);
            int height = Random.Range(roomMinSize.y, roomMaxSize.y);

            int x = Random.Range(1, dungeonWidth - width - 1);
            int y = Random.Range(1, dungeonHeight - height - 1);

            RectInt newRect = new RectInt(x, y, width, height);
            Room newRoom = new Room(newRect);

            bool overlapped = false;
            foreach(var room in rooms)
            {
                if(IsTooClose(newRect, room.rect, roomMargin))
                {
                    overlapped = true;
                    break;
                }

                if(newRect.Overlaps(room.rect))
                {
                    overlapped = true;
                    break;
                }
            }

            if(overlapped == false)
            {
                rooms.Add(newRoom);
            }
        }

        Debug.Log($"생성된 방 개수: {rooms.Count}");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        foreach(var room in rooms)
        {
        switch (room.type)
        {
            case RoomType.Start:
                Gizmos.color = Color.cyan;
                break;
            case RoomType.Combat:
                Gizmos.color = Color.green;
                break;
            case RoomType.Elite:
                Gizmos.color = Color.red;
                break;
            case RoomType.Treasure:
                Gizmos.color = Color.yellow;
                break;
            case RoomType.Boss:
                Gizmos.color = Color.magenta;
                break;
        }

            Vector3 center = new Vector3(
                room.Center.x,
                0,
                room.Center.y
            );

            Vector3 size = new Vector3(
                room.rect.width,
                1,
                room.rect.height
            );

            Gizmos.DrawWireCube(center, size);
        }
            if (map == null) return;

    for (int x = 0; x < dungeonWidth; x++)
    {
        for (int y = 0; y < dungeonHeight; y++)
        {
            if (map[x, y] == 1)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawCube(new Vector3(x, 0, y), Vector3.one * 0.9f);
            }
        }
    }
    }

    void AssignRoomTypes()
    {
        if(rooms.Count == 0) return;

        //1. 시작
        Room startRoom = rooms[0];

        startPos.transform.position = new Vector3(startRoom.Center.x, floorPrefab.transform.localScale.y, startRoom.Center.y);

        startRoom.type = RoomType.Start;

        //2. 보스 방(시작 방에서 가장 먼 방)
        Room bossRoom = startRoom;
        float maxDistance = 0f;

        foreach(var room in rooms)
        {
            float dist = Vector2Int.Distance(startRoom.Center, room.Center);
            if(dist > maxDistance)
            {
                maxDistance = dist;
                bossRoom = room;
            }
        }

        bossRoom.type = RoomType.Boss;

        //3. 나머지 방 타입 분배
        foreach(var room in rooms)
        {
            if(room.type != RoomType.Combat) continue;

            float roll = Random.value;

            if(roll < 0.15f)
                room.type = RoomType.Elite;
            else if (roll < 0.25f)
                room.type = RoomType.Treasure;
            //나머지는 전투방 유지            
        }
    }

    bool IsTooClose(RectInt a, RectInt b, int margin)
    {
        RectInt expanded = new RectInt(
            b.xMin - margin,
            b.yMin - margin,
            b.width + margin * 2,
            b.height + margin * 2
        );

        return a.Overlaps(expanded);
    }


    void ConnectRoomsWithMST()
    {
        conrridors.Clear();
        edges.Clear();
        
        for(int i = 0; i < rooms.Count; i++)
        {
            for(int j = i + 1; j < rooms.Count; j++)
            {
                edges.Add(new Edge(rooms[i], rooms[j]));
            }
        }

        edges.Sort((a, b) => a.cost.CompareTo(b.cost));

        parent.Clear();
        foreach(var room in rooms)
            parent[room] = room;

        foreach(var edge in edges)
        {
            if(Find(edge.a) != Find(edge.b))
            {
                Union(edge.a, edge.b);

                var (from, dirA) = GetRoomEdgeDir(edge.a, edge.b.Center);
                var (to , dirB)= GetRoomEdgeDir(edge.b, edge.a.Center);

                conrridors.Add((from, to, dirA, dirB));
            }
        }
    }

    Room Find(Room r)
    {
        if(parent[r] != r)
            parent[r] = Find(parent[r]);

        return parent[r];        
    }

    void Union(Room a, Room b)
    {
        Room rootA = Find(a);
        Room rootB = Find(b);
        if(rootA != rootB)
            parent[rootB] = rootA;
    }

    void InitMap()
    {
        map = new int[dungeonWidth, dungeonHeight];

        for(int x = 0; x < dungeonWidth; x++)
        {
            for(int y = 0; y < dungeonHeight; y++)
            {
                map[x, y] = 0;
            }
        }
    }

    void CarveRooms()
    {
        foreach(var room in rooms)
        {
            for(int x = room.rect.x; x < room.rect.xMax; x++)
            {
                for(int y = room.rect.y; y < room.rect.yMax; y++)
                {
                    map[x, y] = 1;
                }
            }
        }
    }

void CarveCorridor(Vector2Int from, Vector2Int to, Vector2Int exitDir)
{
    // 1. 방에서 한 칸 나가기
    Vector2Int cur = from + exitDir;
    if (!InBounds(cur.x, cur.y)) return;

    map[cur.x, cur.y] = 1;
    doorRequests.Add((cur, exitDir));

    int safety = dungeonWidth + dungeonHeight;

    // 2. 이제부터는 "목표를 향한 방향"으로 이동
    while (cur != to && safety-- > 0)
    {
        Vector2Int delta = to - cur;

        Vector2Int step;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            step = new Vector2Int((int)Mathf.Sign(delta.x), 0);
        else
            step = new Vector2Int(0, (int)Mathf.Sign(delta.y));

        CarveWideAt(cur);
        cur += step;
    }

    // 3. 도착 지점 문
    Vector2Int backDir = new Vector2Int(
        Mathf.Clamp(from.x - cur.x, -1, 1),
        Mathf.Clamp(from.y - cur.y, -1, 1)
    );

    if (InBounds(cur.x, cur.y))
        doorRequests.Add((cur, -backDir));
}

    (Vector2Int, Vector2Int) GetRoomEdgeDir(Room room, Vector2Int target)
    {
        Vector2Int d = target - room.Center;

        if(Mathf.Abs(d.x) > Mathf.Abs(d.y))
        {
            int y = Mathf.Clamp(
                target.y,
                room.rect.yMin + 1,
                room.rect.yMax - 2
            );

            //좌 / 우 벽
            if(d.x > 0)
                return (new Vector2Int(room.rect.xMax, y), Vector2Int.right);
            else
                return (new Vector2Int(room.rect.xMin - 1, y), Vector2Int.left);
        }
        else
        {
            int x = Mathf.Clamp(
                target.x,
                room.rect.xMin + 1,
                room.rect.xMax - 2
            );

            //위 / 아래 뱍
            if(d.y > 0)
                return (new Vector2Int(x, room.rect.yMax), Vector2Int.up);
            else
                return (new Vector2Int(x, room.rect.yMin - 1), Vector2Int.down);
        }
    }

    (Vector2Int edge, Vector2Int dir) GetRoomExit(RectInt room, Vector2Int target)
    {
        Vector2Int center = Vector2Int.RoundToInt(room.center);
        Vector2Int d = target - center;

        if(Mathf.Abs(d.x) > Mathf.Abs(d.y))
        {
            int y = Mathf.Clamp(center.y, room.yMin + 1, room.yMax - 2);
            if(d.x > 0)
                return (new Vector2Int(room.xMax - 1, y), Vector2Int.right);
                else
                return (new Vector2Int(room.xMin, y), Vector2Int.left);
            
        }
        else
        {
            int x = Mathf.Clamp(center.x, room.xMin + 1, room.xMax - 2);
            if(d.y > 0)
                return(new Vector2Int(x, room.yMax - 1), Vector2Int.up);
                else
                return (new Vector2Int(x, room.yMin), Vector2Int.down);
        }
    }

    void CarveWideAt(Vector2Int p)
    {
        for(int dx = -2; dx <= 2; dx++)
        for(int dy = -2; dy <= 2; dy++)
        {
            int x = p.x + dx;
            int y = p.y + dy;
            if(InBounds(x, y))
                map[x, y] = 1;
        }
    }

    void CarveAllCorridors()
    {
        foreach(var conrridor in conrridors)
        {
            CarveCorridor(conrridor.from, conrridor.to, conrridor.dirA);
        }
    }

    void BuildFloor3D()
    {
        for(int x = 0; x < dungeonWidth; x++)
        {
            for(int z = 0; z < dungeonHeight; z++)
            {
                if(map[x, z] == 1)
                {
                    Vector3 pos = new Vector3(x, 0, z);
                    Instantiate(floorPrefab, pos, Quaternion.identity, dungeonRoot);
                }
            }
        }
    }

    IEnumerator BuildWalls3D(int batch = 1000)
    {
        int placed = 0;

        for(int x = 0; x < dungeonWidth; x++)
        {
            for(int z = 0; z < dungeonHeight; z++)
            {
                if(map[x, z] != 1) continue;

                foreach(var dir in directions)
                {
                    int nx = x + dir.x;
                    int nz = z + dir.y;
                 
                    if(nx < 0 || nz < 0 || nx >= dungeonWidth || nz >= dungeonHeight)
                    {
                        continue;
                    }

                    if(map [nx, nz] == 0)
                    {
                        Vector3 wallPos = new Vector3(
                            x + dir.x * 0.5f,
                            1.5f,
                            z + dir.y * 0.5f
                        );
    
                    Quaternion rot =
                    (dir == Vector2Int.left || dir == Vector2Int.right)
                    ? Quaternion.Euler(0, 90, 0)
                    : Quaternion.identity;

                    Instantiate(wallPrefab, wallPos, rot, dungeonRoot);

                        if(++placed % batch == 0)
                        {
                            yield return null;   
                        }
                    }
}

            }
        }
    }

    bool InBounds(int x, int z)
    {
        return x >= 0 && z >= 0 && x < dungeonWidth && z < dungeonHeight;
    }

    void BuildRoomControllers()
    {
        foreach(var room in rooms)
        {
            GameObject go = new GameObject($"Room_{room.type}");
            go.AddComponent<NetworkIdentity>();
            go.layer = LayerMask.NameToLayer("DetectColl");
            go.transform.parent = dungeonRoot;
            //go.transform.position = new Vector3(room.Center.x, 0, room.Center.y);

            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;

            box.center = new Vector3(
                room.Center.x,
                1,
                room.Center.y
            );

            box.size = new Vector3(
                room.rect.width + 10,
                3,
                room.rect.height + 10
            );

            DungeonController rc = go.AddComponent<DungeonController>();
            rc.barrierPrefab = barrierPrefab;
            rc.roomType = room.type;
            rc.area = room;

            BuildSpawnPoints(room, rc);
        }
    }
    
    bool IsNearWall(int x, int y)
    {
        foreach( var d in directions)
        {
            int nx = x + d.x;
            int ny = y + d.y;

            if(!InBounds(nx, ny)) return true;
            if(map[nx, ny] == 0) return true;
        }
        return false;
    }

    bool IsNearDoor(Vector3 pos, float radius = 1.5f)
    {
        Collider[] hits = Physics.OverlapSphere(pos, radius);
        foreach(var h in hits)
            if(h.GetComponent<Door>() != null)
                return true;
        return false;
    }

    int GetSpawnCount(Room room)
    {
        return room.type switch
        {
            RoomType.Start => 0,
            RoomType.Treasure => 0,
            RoomType.Combat => Mathf.Clamp(room.rect.width * room.rect.height / 30, 2, 5),
            RoomType.Elite => Mathf.Clamp(room.rect.width * room.rect.height / 25, 4, 7),
            RoomType.Boss => Mathf.Clamp(room.rect.width * room.rect.height / 20, 6, 10),
            _ => 2
        };
    }


    void BuildSpawnPoints(Room room, DungeonController rc)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        int xMin = room.rect.xMin + 10;
        int xMax = room.rect.xMax - 20;
        int yMin = room.rect.yMin + 10;
        int yMax = room.rect.yMax - 20;

        for(int x = xMin; x <= xMax; x++)
        {
            for(int y = yMin; y <= yMax; y++)
            {
                //가장자리 링만 사용
                bool isEdge =
                x == xMin || x == xMax ||
                y == yMin || y == yMax;

                if(!isEdge) continue;
                if(map[x, y] != 1) continue;
                if(IsNearWall(x, y)) continue;

                Vector3 worldPos = new Vector3(x, 0, y);
                if(IsNearDoor(worldPos)) continue;

                candidates.Add(new Vector2Int(x, y));
            }
        }

        int want = Mathf.Min(GetSpawnCount(room), candidates.Count);

        //섞기
        for(int i =0; i< candidates.Count; i++)
        {
            int j = Random.Range(i, candidates.Count);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        for(int i = 0; i< want; i++)
        {
            Vector2Int p = candidates[i];

            GameObject sp = new GameObject("SpawnPoint");
            sp.transform.parent = rc.transform;
            sp.transform.position = new Vector3(p.x, 0, p.y);

            rc.spawnPoints.Add(sp.transform);
        }
    }
    
    void TryAddDoor(Room room, Vector2Int roomCell, List<DoorCandidate> list)
    {
        //방 내부 바닥이어야 함
        if(!InBounds(roomCell.x, roomCell.y)) return;
        if(map[roomCell.x, roomCell.y] != 1) return;

        foreach(var dir in directions)//up / down / left / right
        {
            int nx = roomCell.x + dir.x;
            int ny = roomCell.y + dir.y;

            if(!InBounds(nx, ny)) continue;

            //옆칸이 바닥이어야 함(복도)
            if(map[nx, ny] != 1) continue;

            //옆칸이 다른방 내부면 문으로 취급하면 방이 합쳐짐
            //방 밖 바닥(복도)으로만 문 생성
            if(IsInsideAnyRoom(nx, ny)) continue;

            list.Add(new DoorCandidate(roomCell, new Vector2Int(nx, ny), dir));
        }
    }

    bool IsInsideAnyRoom(int x, int y)
    {
        var p = new Vector2Int(x, y);
        foreach(var r in rooms)
            if(r.rect.Contains(p)) return true;
        return false;
    } 

    void BuildDoorsFromRequests()
    {
        if(doorPrefab == null || doorRoot == null) return;

        var used = new HashSet<Vector3>();

        foreach(var req in doorRequests)
        {
            Vector2Int c = req.cell;

            Vector3 pos = new Vector3(
                req.cell.x - req.dir.x * 0.5f,
                1.25f,
                req.cell.y - req.dir.y * 0.5f
            );

            if(!used.Add(pos)) continue;

            //복도의 실제 방향 확인
            bool isHorizontal = IsCorridorHorizontal(c);

            Quaternion rot = 
            !isHorizontal
            ? Quaternion.Euler(0, 90, 0)
            : Quaternion.identity;

            Instantiate(doorPrefab, pos, rot, doorRoot);
        }
    }

    bool IsCorridorHorizontal(Vector2Int cell)
    {
        //문위치 주변의 복도 타일 확인
        int horizontalCount = 0;
        int verticalCount = 0;


        //좌우 확인
        for(int i = -2; i <= 2; i++)
        {
            Vector2Int check = new Vector2Int(cell.x + i, cell.y);
            if(InBounds(check.x, check.y) &&
            map[check.x, check.y] == 1 &&
            !IsInsideAnyRoom(check.x, check.y))
            {
                horizontalCount++;
            }
        }

        //상하 확인
        for(int i = -2; i <= 2; i++)
        {
            Vector2Int check = new Vector2Int(cell.x, cell.y + i);
            if(InBounds(check.x, check.y) &&
            map[check.x, check.y] == 1 &&
            !IsInsideAnyRoom(check.x, check.y))
            {
                verticalCount++;
            }
        }

        //수평복도가 더 많으면 수평
        return horizontalCount > verticalCount;
    }
}
