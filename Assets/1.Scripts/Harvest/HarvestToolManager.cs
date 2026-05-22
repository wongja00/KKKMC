using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HarvestToolManager : MonoBehaviour
{
    public static HarvestToolManager instance;

    private static Dictionary<string, HarvestToolData> harvestToolDic = new Dictionary<string, HarvestToolData>();

    [SerializeField] private HarvestToolDataContainer[] containers; 

    void Awake()
    {
        if(instance == null)
        {
            instance = this;

            foreach(HarvestToolDataContainer container in containers)
            {
                foreach(HarvestToolData data in container.datas)
                {
                    harvestToolDic.TryAdd(data.toolID, data);
                }
            }
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

    public HarvestToolData GetHarvestToolData(string ID)
    {
        harvestToolDic.TryGetValue(ID, out HarvestToolData tempData);

        return tempData;
    }
}
