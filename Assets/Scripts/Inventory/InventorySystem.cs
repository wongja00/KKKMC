using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    private List<Item> items = new List<Item>();

    private Dictionary<string, List<GameObject>> itemObjects = new Dictionary<string, List<GameObject>>();

    public List<InventorySlot> inventorySlots = new List<InventorySlot>();
    
    public List<InventorySlot> hotBarSlots = new List<InventorySlot>();

    [SerializeField] private HotBarSlotsController hotBarContoller;

    [SerializeField] private ItemController itemController;

    [SerializeField] private KeyCode inventoryKey = KeyCode.I;

    [SerializeField] private KeyCode getItemKey = KeyCode.F;

    [SerializeField] private GameObject pickupItemUI;

    [SerializeField] private GameObject AttachUI;

    //인벤토리 패널
    [SerializeField] private GameObject inventoryPanel;

     //UI 핫바 - 퀵 슬롯 아이템 UI 패널
    [SerializeField] private Transform hotBarItemPanel;

    //UI상 보일 아이템 프리팹
    [SerializeField] private InventorySlot inventoryItemPrefab;

    //UI상 아이템 컨테이너  - 인벤토리 패널
    [SerializeField] private Transform inventoryItemContainer;


    private int slotMask;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slotMask = LayerMask.GetMask("Slot");

        foreach(InventorySlot slot in inventoryPanel.GetComponentsInChildren<InventorySlot>())
        {
            inventorySlots.Add(slot);
        }

        foreach(InventorySlot slot in hotBarItemPanel.GetComponentsInChildren<InventorySlot>())
        {
            hotBarSlots.Add(slot);
        }

        if(hotBarContoller == null)
        {
            hotBarContoller = GetComponent<HotBarSlotsController>();

            hotBarContoller.slotCount = hotBarSlots.Count;
        }

        if(itemController == null)
        {
            itemController = GetComponent<ItemController>();

            //itemController.OnAttachItem += DeleteItem;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(inventoryKey))
        {
            if(inventoryPanel.activeSelf)
            {
                inventoryPanel.SetActive(false);
            }
            else
            {
                inventoryPanel.SetActive(true);
            }
        }
        
        ShowPickupItemUI();

        ShowAttachUI();
        
        SelectHotSlot(hotBarContoller.curSelSlot);
    }

    public void AddItem(Item item)
    {
        if(item == null)
        {
            Debug.Log("Item is null");
            return;
        }

         var existingItem = items.Find(i => i.GetItemType() == item.GetItemType() && i.GetName() == item.GetName());

        if(existingItem != null)
        {
            existingItem.SetCount(existingItem.GetCount() + 1);
        }
        else
        {
            items.Add(item);
            AddItemUI(item);
        }
        
    }

    public void AddItemUI(Item item)
    {        
        //일단 임시적으로 핫바에 
        foreach(InventorySlot slot in hotBarSlots)
        {
            if(slot.GetSlotType() == SlotType.NullSlot)
            {
                slot.SetItem(item);
                break;
            }
        }


    }

    public void DeleteItem(GameObject itemobject)
    {
        Item item = itemobject?.GetComponent<Item>();

        if(item == null)
        {
            return;
        }

        foreach(InventorySlot slot in hotBarSlots)
        {
            if(item.GetItemId() == slot.itemData.itemId)
            {
                slot.SetEmptySlot();
                items.Remove(item);

                if(itemObjects.TryGetValue(item.GetItemId(), out var list))
                {
                    if(list.Count > 0) list.RemoveAt(0);
                    if(list.Count == 0) itemObjects.Remove(item.GetItemId());
                }
            }
        }
    }

    public void ShowPickupItemUI()
    {
        Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, 3f);
        if(hit.collider != null && hit.collider.gameObject.CompareTag("Item"))
        {
            pickupItemUI.SetActive(true);

            if(Input.GetKeyDown(getItemKey))
            {
                GameObject item = hit.collider.gameObject;
                if(item != null)
                {
                    AddItem(item.GetComponent<Item>());

                    var id = item.GetComponent<Item>().GetItemId();
                    if(!itemObjects.TryGetValue(id, out var list))
                    {
                        list = new List<GameObject>();
                        itemObjects[id] = list;
                    }
                    list.Add(item);

                    item.transform.SetParent(this.transform);
                    item.transform.localPosition = Vector3.zero;

                    item.SetActive(false);
                }
            }

        }
        else
        {
            pickupItemUI.SetActive(false);

        }
    }

    public void ShowAttachUI()
    {
        Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, 3f, slotMask);
        
        if(hit.collider != null)
        {
            AttachUI.SetActive(true);
        }
        else
        {
            AttachUI.SetActive(false);
        }
    }

    private void SelectHotSlot(int index)
    {
        if(hotBarSlots == null || hotBarSlots.Count == 0) return;
        if(index < 0 || index >= hotBarSlots.Count) return;

        var slot = hotBarSlots[index];
        if(slot == null) return;

        slot.SelectSlot(true);
        
        if(slot.GetSlotType() == SlotType.NullSlot || slot.itemData == null) return;

        if(!itemObjects.TryGetValue(slot.itemData.itemId, out var list) || list == null)
        {            
            Debug.Log("없서용 ㅠㅠ");

            return; 
        } 

        var attachobject = list[0];
        if(attachobject == null) return;

        itemController.SetCurObject(attachobject);
    }
}
