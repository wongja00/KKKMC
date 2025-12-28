using UnityEngine;
using System.Collections.Generic;
using WebSocketSharp;

public class DungeonController : MonoBehaviour
{
    public RoomType roomType;

    public Door[] doors;
    private List<GameObject> spawnEnemies = new List<GameObject>();
    public List<Transform> spawnPoints = new List<Transform>();

    private int aliveEnemies = 0;
    private bool activated = false;

    private BoxCollider boxCollider;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        
        doors = FindDoorsInRoom();
    }

    void OnTriggerEnter(Collider other)
    {
        if(activated) return;
        if(!other.CompareTag("Player")) return;

        activated = true;

        StartCombat();
    }

    void StartCombat()
    {
        CloseDoors();

        if(roomType == RoomType.Start || roomType == RoomType.Treasure)
        {
            EndCombat();
            SpawnStartWeapon();
            return;
        }

        SpawnEnemies();
    }

    void OpenDoors()
    {
        foreach(var d in doors)
            d.Open();
    }

    void CloseDoors()
    {
        foreach(var d in doors)
            d.Close();
    }

    void EndCombat()
    {
        OpenDoors();
    }

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
            EndCombat();
    }

    Door[] FindDoorsInRoom()
    {
        List<Door> result = new();

        Collider[] hits = Physics.OverlapBox(
            transform.position + boxCollider.center,
            boxCollider.size * 0.5f,
            Quaternion.identity
        );

        foreach(var h in hits)
        {
            Door d = h.GetComponent<Door>();
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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
