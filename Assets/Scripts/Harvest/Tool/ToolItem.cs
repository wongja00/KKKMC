using UnityEngine;
using System;
using System.Collections;
using Unity.VisualScripting;

public class ToolItem : MonoBehaviour, Item, ToolBase
{
    [SerializeField]
    private string itemID = "";

    [SerializeField]
    private string toolID = "";
    
    private ScriptableItemData scriptableItemData;

    private HarvestToolData harvestToolData;

    private int curCount = 0;
    private int curDurability = 100;

    private bool isAttached = false; 


    public int GetCount()
    {
        return curCount;
    }

    public string GetDescription()
    {
        if(scriptableItemData != null)
        {
            return scriptableItemData.description;
        }
        else
        {
            return "";
        }
    }

    public int GetDurability()
    {
        return curDurability;
    }

    public Sprite GetIcon()
    {
        if(scriptableItemData != null)
        {
            return scriptableItemData.itemIcon;
        }
        else
        {
            return null;
        }
    }

    public ScriptableItemData GetItemData()
    {
        return scriptableItemData;
    }

    public string GetItemId()
    {
        return itemID;
    }

    public ItemType GetItemType()
    {
        if(scriptableItemData != null)
        {
            return scriptableItemData.itemType;
        }
        else
        {
            return ItemType.Unknown;
        }
    }

    public int GetMaxStackSize()
    {
        if(scriptableItemData != null)
        {
            return scriptableItemData.maxStackSize;
        }
        else
        {
            return 0;
        }
    }

    public string GetName()
    {
        if(scriptableItemData != null)
        {
            return scriptableItemData.itemName;
        }
        else
        {
            return "Unknown";
        }
    }

    public ToolType GetToolType()
    {
        if(harvestToolData != null)
        {
            return harvestToolData.toolType;
        }
        else
        {
            return ToolType.None;
        }
    }

    public bool IsAttached()
    {
        return this.isAttached;
    }
    public GameObject GetObject()
    {
        return gameObject;
    }

    public bool IsStackable()
    {
        return false;
    }

    public void SetCount(int count)
    {
        return;
    }

    public void SetIsAttached(bool set)
    {
        isAttached = set;
    }

    public void SetItemData(ScriptableItemData itemData)
    {
        this.scriptableItemData = itemData;
    }

     public int GetHarvestAmount()
     {
        if(harvestToolData != null)
            return harvestToolData.harvestAmount;
        else
            return 0;

     }

    public void DoHarvest()
    {

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scriptableItemData = ItemManager.Instance.GetItemData(itemID);

        harvestToolData = HarvestToolManager.instance.GetHarvestToolData(toolID);

        curDurability = harvestToolData.maxDuration;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
