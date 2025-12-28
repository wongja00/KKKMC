using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public GameObject meleeEnemyPrefab;
    public GameObject rangedEnemyPrefab;
    public GameObject eliteEnemyPrefab;

    public List<GameObject> assultRifles = new(); 
    public List<GameObject> pistlos = new(); 
    public List<GameObject> shotguns = new(); 

    void Awake()
    {
        EnemyFactory.Init(new Dictionary<EnemyType, GameObject>
        {
            {EnemyType.Melee, meleeEnemyPrefab},
            {EnemyType.Ranged, rangedEnemyPrefab},
            {EnemyType.Elite, eliteEnemyPrefab}
        });

        if(assultRifles.Count > 0)
        {
            WeaponFactory.Init(new Dictionary<GunType, List<GameObject>>
            {
                {GunType.AssultRifle, assultRifles}
            });
        }
        
        if(pistlos.Count > 0)
        {
            WeaponFactory.Init(new Dictionary<GunType, List<GameObject>>
            {
                {GunType.Pistol, pistlos}
            });
        }
        
        if(shotguns.Count > 0)
        {
            WeaponFactory.Init(new Dictionary<GunType, List<GameObject>>
            {
                {GunType.ShotGun, shotguns}
            });
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
