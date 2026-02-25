using UnityEngine;
using System.Collections.Generic;
using Mirror;
public class DungeonController : NetworkBehaviour
{
    public RoomType roomType;

    public DoorInteractor[] doors;
    private List<GameObject> spawnEnemies = new List<GameObject>();
    public List<Transform> spawnPoints = new List<Transform>();
    public List<GameObject> barriers = new List<GameObject>();
    public GameObject barrierPrefab;
    public Room area;

    private int aliveEnemies = 0;

    [SyncVar]
    private bool activated = false;

    private BoxCollider boxCollider;

    public event System.Action OnEndCombat;
    

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        
        doors = FindDoorsInRoom();
    }

    void Start()
    {
        //OpenDoors();
        //CreateRoomWall();
        boxCollider.size = new Vector3(boxCollider.size.x - 1, 3, boxCollider.size.z - 1);

    }

    void OnTriggerEnter(Collider other)
    {        
        if(activated) return;
        
        if(!other.CompareTag("Player")) return;
        
        OnEnterCombat();
    }

    [Command(requiresAuthority = false)]
    private void OnEnterCombat()
    {
        activated = true;

        StartCombat();
    }

    

    [Server]
    void StartCombat()
    {
        CloseDoors();

        if(roomType == RoomType.Start || roomType == RoomType.Treasure)
        {
            EndCombat();
            //SpawnStartWeapon();
            return;
        }
         Debug.Log("전투");

        SpawnEnemies();
    }

    void OpenDoors()
    {
        foreach(var d in doors)
        {
            if(d.isOpen == false)
            {
                d.Interact();
            }
        }
    }

    void CloseDoors()
    {
        foreach(var d in doors)
        {
            if(d.isOpen == true)
            {
                d.Interact();
            }
        }
        
        //ActiveBarriers();
    }

    void EndCombat()
    {
        OpenDoors();

        if(roomType == RoomType.Combat || roomType == RoomType.Boss|| roomType == RoomType.MiniBoss)
            OnEndCombat?.Invoke();
    }

    //[ClientRpc] 
    void SpawnEnemies()
    {
        foreach(var sp in spawnPoints)
        {
            EnemyType type = ChooseEnemyType();
            GameObject enemy = EnemyFactory.Spawn(type, sp.position);
            aliveEnemies++;

            if(enemy.GetComponent<Enemy>() != null)
            {
                enemy.GetComponent<Enemy>().OnDeath += OnEnemyDead;
                enemy.GetComponent<Enemy>().isDugeon = true;
            }
        }
    }

    EnemyType ChooseEnemyType()
    {
        float r = Random.value;
        if(r < 0.6f) return EnemyType.Melee;
        if(r < 0.9f) return EnemyType.Ranged;
        return EnemyType.Elite;
    }

    void OnEnemyDead()
    {
        aliveEnemies--;
        if(aliveEnemies <= 0)
        {
            EndCombat();
            BuffIncrease();
            //CmdBuffUI();
        }
    }

    
    [Server]
    void CmdBuffUI()
    {
        //BuffIncrease();
    }
    
    [Server]
    void BuffIncrease()
    {
        foreach(Transform user in PlayerRegistry.Players)
        {
            user.GetComponentInChildren<PlayerBuffSystem>().IncreaseSelectBuffCount();
        }
    }

    DoorInteractor[] FindDoorsInRoom()
    {
        List<DoorInteractor> result = new List<DoorInteractor>();

        Collider[] hits = Physics.OverlapBox(
            transform.position + boxCollider.center,
            boxCollider.size * 0.5f,
            Quaternion.identity
        );

        foreach(var h in hits)
        {
            DoorInteractor d = h.GetComponent<DoorInteractor>();
            if(d != null)
                result.Add(d);
        }

        return result.ToArray();
    }

    void SpawnStartWeapon()
    {
        if(roomType == RoomType.Start)
        {
            Vector3 centerPos = new Vector3(boxCollider.center.x, 1, boxCollider.center.z);

            WeaponFactory.SpawnRandomWeaponByType(GunType.AssultRifle, centerPos);
        }

    }

    // 방 밖 못나가게 방벽(벽) 세우는 함수
    void CreateRoomWall()
    {
        Vector3 northPos = new Vector3(boxCollider.center.x, boxCollider.center.y, boxCollider.center.z + boxCollider.size.z/2);
        Vector3 southPos = new Vector3(boxCollider.center.x, boxCollider.center.y, boxCollider.center.z - boxCollider.size.z/2);
        Vector3 eastPos = new Vector3(boxCollider.center.x + boxCollider.size.x/2, boxCollider.center.y, boxCollider.center.z);
        Vector3 westPos = new Vector3(boxCollider.center.x - boxCollider.size.x/2, boxCollider.center.y, boxCollider.center.z);

        GameObject a = Object.Instantiate(barrierPrefab, northPos, Quaternion.identity);
        GameObject b = Object.Instantiate(barrierPrefab, southPos, Quaternion.identity);
        GameObject c = Object.Instantiate(barrierPrefab, eastPos, Quaternion.identity);
        GameObject d = Object.Instantiate(barrierPrefab, westPos, Quaternion.identity);

        NetworkServer.Spawn(a);
        NetworkServer.Spawn(b);
        NetworkServer.Spawn(c);
        NetworkServer.Spawn(d);

        a.transform.localScale = new Vector3 (boxCollider.size.x, 5, 0.3f); 
        b.transform.localScale = new Vector3 (boxCollider.size.x, 5, 0.3f); 
        c.transform.localScale = new Vector3 (0.3f, 5, boxCollider.size.z); 
        d.transform.localScale = new Vector3 (0.3f, 5, boxCollider.size.z); 

        barriers.Add(a);
        barriers.Add(b);
        barriers.Add(c);
        barriers.Add(d);

        DisabeBarriers();
    }

    void DisabeBarriers()
    {
        foreach(GameObject bar in barriers)
        {
            bar.SetActive(false);
        }
    }

    void ActiveBarriers()
    {
        foreach(GameObject bar in barriers)
        {
            bar.SetActive(true);
        }
    }
}
