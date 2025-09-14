using UnityEngine;

/// <summary>
/// 인벤토리 UI에 보일 아이템 구현체
/// 스크립터블 오브젝트를 사용하는 아이템 구현체
/// </summary>
[System.Serializable]
public class InventoryItem : Item
{
    private ScriptableItemData itemData;
    private int count;
    
    public InventoryItem(ScriptableItemData data, int initialCount = 1)
    {
        itemData = data;
        count = initialCount;
    }
    
    public string GetName()
    {
        return itemData?.itemName ?? "Unknown Item";
    }
    
    public string GetDescription()
    {
        return itemData?.description ?? "No description available";
    }
    
    public int GetCount()
    {
        return count;
    }
    
    public void SetCount(int newCount)
    {
        if (newCount < 0)
        {
            count = 0;
        }
        else if (IsStackable() && newCount > GetMaxStackSize())
        {
            count = GetMaxStackSize();
        }
        else
        {
            count = newCount;
        }
    }
    
    public ItemType GetItemType()
    {
        return itemData?.itemType ?? ItemType.Material;
    }
    
    public bool IsStackable()
    {
        return itemData?.isStackable ?? false;
    }
    
    public int GetMaxStackSize()
    {
        return itemData?.maxStackSize ?? 1;
    }
    
    public Sprite GetIcon()
    {
        return itemData?.itemIcon;
    }
    
    public string GetItemId()
    {
        return itemData?.itemId ?? "";
    }

    public void SetItemData(ScriptableItemData itemData)
    {
        this.itemData = itemData;
    }
    
    /// <summary>
    /// 아이템 데이터를 직접 가져옵니다.
    /// </summary>
    /// <returns>스크립터블 아이템 데이터</returns>
    public ScriptableItemData GetItemData()
    {
        return itemData;
    }
    
    /// <summary>
    /// 아이템을 복사합니다.
    /// </summary>
    /// <returns>새로운 인벤토리 아이템</returns>
    public InventoryItem Clone()
    {
        return new InventoryItem(itemData, count);
    }
    
    /// <summary>
    /// 아이템이 같은지 확인합니다.
    /// </summary>
    /// <param name="other">비교할 아이템</param>
    /// <returns>같은 아이템인지 여부</returns>
    public bool IsSameItem(InventoryItem other)
    {
        return other != null && GetItemId() == other.GetItemId();
    }
}
