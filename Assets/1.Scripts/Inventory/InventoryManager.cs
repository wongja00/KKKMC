using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("인벤토리 매니저")]

    
    public static InventoryManager Instance;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
    }

    public void AddItemInToInventory(Item item)
    {
    }
}
