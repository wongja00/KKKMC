using UnityEngine;

public class VehicleWheel : MonoBehaviour, Item
{
    [SerializeField] private string itemId;

    [SerializeField] private string Itemname;
    [SerializeField] private string description;
    [SerializeField] private int count;
    [SerializeField] private ItemType itemType = ItemType.VehicleWheel;
    [SerializeField] private bool isStackable = false;
    [SerializeField] private int maxStackSize = 1;
    [SerializeField] private Sprite icon;

    [SerializeField] private WheelCollider wheelCollider;

    ScriptableItemData itemData;

    public string GetName()
    {
        return Itemname;
    }
    public string GetDescription()
    {
        return description;
    }
    public int GetCount()
    {
        return count;
    }
    public void SetCount(int count)
    {
        this.count = count;
    }
    public ItemType GetItemType()
    {
        return itemType;
    }
    public bool IsStackable()
    {
        return isStackable;
    }
    public int GetMaxStackSize()
    {
        return maxStackSize;
    }
    public Sprite GetIcon()
    {
        return icon;
    }
    public string GetItemId()
    {
        return itemId;
    }
    public void SetItemData(ScriptableItemData itemData)
    {
        Itemname = itemData.itemName;
        description = itemData.description;
        itemType = itemData.itemType;
        isStackable = itemData.isStackable;
        maxStackSize = itemData.maxStackSize;
        icon = itemData.itemIcon;
        itemId = itemData.itemId;

        this.itemData = itemData;
    }

    public ScriptableItemData GetItemData()
    {
        return itemData;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetItemData(ItemManager.Instance.GetItemData(itemId));

        wheelCollider = GetComponentInChildren<WheelCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public WheelCollider GetWheelCollider()
    {
        return wheelCollider;
    }
}
