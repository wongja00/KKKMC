using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
        public static GameManager Instance { get; private set; }

    [SerializeField] List<GameObject> meleeEnemyList;
    [SerializeField] List<GameObject> rangedEnemyList;
    [SerializeField] List<GameObject> bossEnemyList;
    [SerializeField] List<GameObject> eliteEnemyList;

    public List<GameObject> assultRifles = new(); 
    public List<GameObject> pistlos = new(); 
    public List<GameObject> shotguns = new(); 

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        if(Instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            Instance = this;
        }

        Application.runInBackground = true;
        Application.targetFrameRate = 60; // 원하는 FPS

        
        EnemyFactory.Init(new Dictionary<EnemyType, List<GameObject>>
        {
            {EnemyType.Melee, meleeEnemyList},
            {EnemyType.Ranged, rangedEnemyList},
            {EnemyType.Elite, eliteEnemyList},
            {EnemyType.Boss, bossEnemyList}
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
