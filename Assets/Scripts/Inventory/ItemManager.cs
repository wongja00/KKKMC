using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }
    
    [Header("아이템 데이터베이스")]
    [SerializeField] private ItemDatasContaner[] itemDatabase;
    
    private Dictionary<string, ScriptableItemData> itemDictionary;
    
    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
        }

        
        InitializeItemDatabase();
    }

    private void Start()
    {        
    }
    
    private void InitializeItemDatabase()
    {
        itemDictionary = new Dictionary<string, ScriptableItemData>();
        
        foreach (var itemContaner in itemDatabase)
        {
            // 모든 스크립터블 오브젝트를 딕셔너리에 등록
            foreach (var itemData in itemContaner.itemDatas)
            {
                if (itemData != null && !string.IsNullOrEmpty(itemData.itemId))
                {
                    if (itemDictionary.ContainsKey(itemData.itemId))
                    {
                        Debug.LogWarning($"아이템 ID가 중복됩니다: {itemData.itemId}");
                    }
                    else
                    {
                        itemDictionary[itemData.itemId] = itemData;
                    }
                }
                else
                {
                    Debug.LogWarning($"아이템 ID가 비어있습니다: {itemData.itemId}");
                }
            }
        }
    }
    
    /// <summary>
    /// 아이템 ID로 아이템 데이터를 가져옵니다.
    /// </summary>
    /// <param name="itemId">아이템 ID</param>
    /// <returns>아이템 데이터, 없으면 null</returns>
    public ScriptableItemData GetItemData(string itemId)
    {
        if (itemDictionary.TryGetValue(itemId, out ScriptableItemData itemData))
        {
            return itemData;
        }
        
        Debug.LogWarning($"아이템을 찾을 수 없습니다: {itemId}");
        return null;
    }
    
    /// <summary>
    /// 아이템 타입으로 모든 아이템을 가져옵니다.
    /// </summary>
    /// <param name="itemType">아이템 타입</param>
    /// <returns>해당 타입의 아이템 리스트</returns>
    public List<ScriptableItemData> GetItemsByType(ItemType itemType)
    {
        return itemDictionary.Values.Where(item => item.itemType == itemType).ToList();
    }
    
    /// <summary>
    /// 모든 아이템 데이터를 가져옵니다.
    /// </summary>
    /// <returns>모든 아이템 데이터 리스트</returns>
    public List<ScriptableItemData> GetAllItems()
    {
        return itemDictionary.Values.ToList();
    }
    
    /// <summary>
    /// 아이템이 존재하는지 확인합니다.
    /// </summary>
    /// <param name="itemId">아이템 ID</param>
    /// <returns>존재 여부</returns>
    public bool HasItem(string itemId)
    {
        return itemDictionary.ContainsKey(itemId);
    }
    
    /// <summary>
    /// 아이템 개수를 반환합니다.
    /// </summary>
    /// <returns>총 아이템 개수</returns>
    public int GetItemCount()
    {
        return itemDictionary.Count;
    }
}
