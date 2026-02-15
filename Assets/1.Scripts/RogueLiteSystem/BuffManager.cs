using System.Collections.Generic;
using UnityEngine;

    public enum BuffType
    {
        None,
        Speed,
        AttackSpeed,
        Agility,
        Strength,
        Health,
        Defense,
        CritChance,
        CritDamage,
    }

public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance;

    public BuffDataObject[] buffDataObjects;

    public Dictionary<int, BuffDataObject> buffDataDic;

    void Awake()
    {
        if(Instance == null)
            Instance = this;

        foreach(BuffDataObject data in buffDataObjects)
        {
            buffDataDic.Add(data.buffID, data);
        }
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
