using UnityEngine;

//아이템 인터페이스
public interface Item
{
    string GetName();
    string GetDescription();
    int GetCount();
    void SetCount(int count);

    ItemType GetItemType();

    bool IsStackable();
    
    bool IsAttached();

    void SetIsAttached(bool set);

    int GetMaxStackSize();
    
    // 아이콘을 가져오는 메서드 추가
    Sprite GetIcon();
    
    // 아이템 ID를 가져오는 메서드 추가
    string GetItemId();

    void SetItemData(ScriptableItemData itemData);

    GameObject GetObject();

    ScriptableItemData GetItemData();
}

public enum ItemType
{
    Unknown,
    Weapon,
    Armor,
    Consumable,
    Material,
    Quest,
    Currency,
    Node,
    HarvestTool,

//자동차 부품
    VehicleFrame,
    VehicleWheel,
    VehicleEngine,
    VehicleChassis,

}

public enum WheelType
{
    None,
    FrontLeft,
    FrontRight,
    RearRight,
    RearLeft
}
public enum SlotType
{
    NullSlot,
    ActiveSlot,
    QuickSlot,
    InventorySlot,
    EquipmentSlot,
    CraftingSlot,
    QuestSlot,
    CurrencySlot,
    VehicleSlot
}
