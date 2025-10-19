using Unity.VisualScripting;
using UnityEngine;

public class NodeItem : MonoBehaviour, Item
{
    [SerializeField] string nodeItemID;

    [SerializeField]private ScriptableItemData nodeItemData;

    private int curCount = 0;

    private bool isAttached;

    public int GetCount()
    {
        return curCount;
    }

    public GameObject GetObject()
    {
        if(this.gameObject)
            return this.gameObject;
            else
            {
                return null;
            }
    }

    public string GetDescription()
    {
        if(nodeItemData != null)
            return nodeItemData.description;
        else
            return "";
    }

    public Sprite GetIcon()
    {
        if(nodeItemData !=null)
            return nodeItemData.itemIcon;

            else
            return null;
    }

    public ScriptableItemData GetItemData()
    {
        if(nodeItemData != null)
            return nodeItemData;
        else
            return null;
    }

    public string GetItemId()
    {
        if(nodeItemData != null)
            return nodeItemID;    
        else
            return "";
    }

    public ItemType GetItemType()
    {
        if(nodeItemData != null)
            return nodeItemData.itemType;    
        else
            return ItemType.Node;
    }

    public int GetMaxStackSize()
    {
        if(nodeItemData != null)
            return nodeItemData.maxStackSize;    
        else
            return 0;
    }

    public string GetName()
    {
        if(nodeItemData != null)
            return nodeItemData.name;    
        else
            return "UnKnown";
    }

    public bool IsAttached()
    {
        return isAttached;
    }

    public bool IsStackable()
    {
        if(nodeItemData != null)
            return nodeItemData.isStackable;
        else
            return false;

    }

    public void SetCount(int count)
    {
        this.curCount = Mathf.Clamp(count, 0, int.MaxValue);
    }

    public void SetIsAttached(bool set)
    {
        isAttached = set;
    }

    public void SetItemData(ScriptableItemData itemData)
    {
        this.nodeItemData = itemData;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isAttached = false;

        if(ItemManager.Instance != null)
        {
            nodeItemData = ItemManager.Instance.GetItemData(nodeItemID);

            if(nodeItemData == null)
            {
                Debug.Log("데이터 없음");

                nodeItemData = new ScriptableItemData();
            }
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
