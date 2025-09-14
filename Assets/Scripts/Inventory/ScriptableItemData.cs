using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class ScriptableItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemId = "0";
    public string itemName = "Unknown Item";
    [TextArea(3, 5)]
    public string description = "Unknown Item";
    public ItemType itemType = ItemType.Unknown;
    
    [Header("아이콘")]
    public Sprite itemIcon = null;
    
    [Header("스택 설정")]
    public bool isStackable = true;
    public int maxStackSize = 99;
    
    [Header("가격 정보")]
    public int buyPrice = 0;
    public int sellPrice = 0;
    
    [Header("추가 속성")]
    public bool isConsumable = false;
    public bool isTradeable = true;
    public bool isQuestItem = false;

    public GameObject modulePrefab;
}
