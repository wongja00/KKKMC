using UnityEngine;
using System;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.PlayerLoop;
using Mirror;
using Unity.Cinemachine;

public class InventorySystem : NetworkBehaviour
{
    //private List<Item> items = new List<Item>();

    private Dictionary<string, List<Item>> itemObjects = new Dictionary<string, List<Item>>();

    public List<InventorySlot> inventorySlots = new List<InventorySlot>();
    
    public List<InventorySlot> hotBarSlots = new List<InventorySlot>();

    [SerializeField] private HotBarSlotsController hotBarContoller;

    [SerializeField] private ItemController itemController;

    [SerializeField] private CharacterHarvest harvesetContoller;

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

    [SerializeField] private Collider itemCollider;
    [SerializeField] private float interactrange;
    
    public static event Action<Item, int> OnInventoryChanged;

    private int slotMask;

    [SerializeField]private GameObject PlayerUI;

    [SerializeField] CinemachineCamera playerCamera;

    private LayerMask itemMask;

    private GameObject curItem;

    void Awake()
    {
        PlayerUI = GameObject.Find("PlayerUI");

        if (PlayerUI != null)
        {
            Transform[] allChildren = PlayerUI.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                switch (child.name)
                {
                    case "GetItem":
                        pickupItemUI = child.gameObject;
                        break;
                    case "AttachUI":
                        AttachUI = child.gameObject;
                        break;
                    case "InvenPanel":
                        inventoryPanel = child.gameObject;
                        break;
                    case "HatBarPanel":
                        hotBarItemPanel = child;
                        break;
                    case "InvenContent":
                        inventoryItemContainer = child;
                        break;
                }
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slotMask = LayerMask.GetMask("Slot");
        itemMask = LayerMask.GetMask("Item");

        foreach(InventorySlot slot in inventoryPanel.GetComponentsInChildren<InventorySlot>())
        {
            inventorySlots.Add(slot);
        }

        foreach(InventorySlot slot in hotBarItemPanel.GetComponentsInChildren<InventorySlot>())
        {
            hotBarSlots.Add(slot);
        }

        if (hotBarContoller == null)
            hotBarContoller = GetComponent<HotBarSlotsController>();
        hotBarContoller.slotCount = hotBarSlots.Count;

        if(itemController == null)
        {
            itemController = GetComponent<ItemController>();

            //itemController.OnAttachItem += DeleteItem;
        }

        if(harvesetContoller != null)
        {
            harvesetContoller.OnHarvest += GetNode;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!isLocalPlayer) return;
        
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

    public void AddItem(Item item, int Amount = 1)
    {
        if(!isLocalPlayer) return;

        if(item == null)
        {
            Debug.Log("Item is null");
            return;
        }

        item.SetIsAttached(false);
        
        //딕셔러니에서 키 값 찾음
        if(!itemObjects.TryGetValue(item.GetItemId(), out var existingList))
        {
            existingList = new List<Item>();
            itemObjects[item.GetItemId()] = existingList;
        }

        if(existingList.Count > 0)
        {   
            //쌓이는지 안쌓이는지
            if(existingList[0].IsStackable())
            {
                existingList[0].SetCount(existingList[0].GetCount() + Amount);
            }
            else
            {
                //새로운 아이템 추가
                existingList.Add(item);             
                AddItemUI(item);
            }

            OnInventoryChanged?.Invoke(item, existingList[0].GetCount());
        }     
        else
        {
            existingList.Add(item);
            existingList[0].SetCount(1);

            AddItemUI(item);

            OnInventoryChanged?.Invoke(item, existingList.Count);
        } 
    }

    private void AddItem(GameObject item, int Amount = 1)
    {
        if(!isLocalPlayer) return;

        if(item != null)
        {
            item.TryGetComponent<Item>(out Item tempItem);
            
            //데이터 처리
            AddItem(tempItem, Amount);            

            uint ni = item.GetComponent<NetworkIdentity>().netId;

            if(isLocalPlayer)        
                ItemDeleteOnServer(ni);
        }
    }

    [Command]
    void ItemDeleteOnServer(uint netID)
    {
        if (NetworkServer.spawned.TryGetValue(netID, out NetworkIdentity ni))
        {
            if (ni.gameObject.scene.IsValid())
                NetworkServer.Destroy(ni.gameObject);
        }
    }

    public void AddItemUI(Item item)
    {        
        if(!isLocalPlayer) return;

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

        Collider[] colliders = Physics.OverlapSphere(transform.position, interactrange, itemMask);

        if(colliders.Length <= 0) {pickupItemUI.SetActive(false); return;}
        if(curItem == colliders[0].gameObject) {pickupItemUI.SetActive(false); return;}

        else {pickupItemUI.SetActive(true);}



        PickupItem(colliders[0]);

        


        //if(Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, 10f))
        //{
        //    if(hit.collider != null && hit.collider.gameObject.CompareTag("Item"))
        //    {
        //        pickupItemUI.SetActive(true);
//
        //        PickupItem(hit);
        //    }
        //    else
        //    {
        //        pickupItemUI.SetActive(false);
//
        //    }
        //}
        //else
        //{
        //    pickupItemUI.SetActive(false);
        //}
    }

    public void PickupItem(Collider hit)
    {   
        if(!isLocalPlayer) return;

        //아이템 줍줍줍
        if(Input.GetKeyDown(getItemKey))
        {
            GameObject item = hit.gameObject;

            AddItem(item);
        }
    }
  
    public bool HasItem(ScriptableItemData item, int Amount)
    {
        if(itemObjects.TryGetValue(item.itemId, out List<Item> itemList) == false)
        {
            return false;
        }

        if(item.isStackable == true)
        {
                if(itemList[0].GetCount() < Amount)
                {
                    return false;
                }
                else
                {
                    return true;
                }
        }
        else
        {
            if(itemList.Count < Amount)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public void RemoveItem(ScriptableItemData item, int amount)
    {
        if(itemObjects.TryGetValue(item.itemId, out List<Item> itemList))
        {
            if(itemList.Count <= 0) return;

            //스택, 즉 쌓이는 거일때
            if(item.isStackable == true)
            {
                if(itemList[0].GetCount() < amount)
                {
                    return;
                }
                else
                {
                    itemList[0].SetCount(itemList[0].GetCount() - amount);

                    if(itemList[0].GetCount() <= 0)
                    {                        
                        OnInventoryChanged?.Invoke(itemList[0], 0);
                        itemObjects.Remove(item.itemId);
                        
                        return;
                    }
                }

                OnInventoryChanged?.Invoke(itemList[0], itemList[0].GetCount());
            }
            else
            {
                //안쌓이는 거
                if(itemList.Count < amount)
                {
                    return;
                }
                else
                {
                    for (int i = 0; i < amount && itemList.Count > 0; i++)
                    {
                        itemList.RemoveAt(0);
                    }

                    if(itemList.Count <= 0)
                    {
                        OnInventoryChanged?.Invoke(itemList[0], 0);

                        itemObjects.Remove(item.itemId);
                    }

                    OnInventoryChanged?.Invoke(itemList[0], itemList.Count);
                }                
            }
        }        
    }

    public void AddItem(ScriptableItemData item, int amount)
    {
        AddItem(item.itemPrefab, amount);
    }
    

    public void ShowAttachUI()
    {
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, 3f, slotMask))
        {
            if (hit.collider.gameObject.TryGetComponent<AttachedModule>(out AttachedModule module) && module != null && !module.GetIsAttached())
            {
                AttachUI.SetActive(true);
            }
            else
            {
                AttachUI.SetActive(false);
            }
        }
        else
        {
            AttachUI.SetActive(false);
        }
    }

    private void SelectHotSlot(int index)
    {        
        harvesetContoller.SetCurItem(null);
        if(hotBarSlots == null || hotBarSlots.Count == 0) return;
        if(index < 0 || index >= hotBarSlots.Count) return;

        var slot = hotBarSlots[index];
        if(slot == null) return;

        slot.SelectSlot(true);
        
        if(slot.GetSlotType() == SlotType.NullSlot || slot.itemData == null) 
        {
            harvesetContoller.SetCurItem(null);
            return;
        }
        if(!itemObjects.TryGetValue(slot.itemData.itemId, out var list) || list == null)
        {            
            Debug.Log("없서용 ㅠㅠ");

            return; 
        } 

        var attachobject = list[0];
        if(attachobject == null) return;

        // attachobject가 붙일 수 있는 Item일 때만 SetCurObject 설정
        Item item1 = attachobject;

        if(item1 != null && item1.GetItemData().isAttachable == true)
        {
            itemController.SetCurObject(attachobject.GetObject());
        }
        else
        {
            itemController.SetCurObject(null);
        }

        //채집/채굴 도구일떄
        attachobject.GetItemData().itemPrefab.TryGetComponent<ToolItem>(out ToolItem toolitem);

        if(toolitem != null)
        {
            harvesetContoller.SetCurItem(toolitem);
        }
        else
        {
            harvesetContoller.SetCurItem(null);
        }
    }

    public void GetNode(GameObject getNode, int Amount)
    {
        if (getNode == null) return;

        // NodeItem 컴포넌트가 있는지 확인
        NodeItem nodeItem = getNode.GetComponent<NodeItem>();
        if (nodeItem != null)
        {
            GameObject tempItem = Instantiate(getNode, transform);

            AddItem(tempItem, Amount);  
        }      
    }

    public Item GetItemByID(string id)
    {
        if(itemObjects.TryGetValue(id, out List<Item> value))
        {
            return value[0];
        }
        else
        {
            return null;
        }
        
        
    }

    public int GetItemCount(string ID)
    {
        if(itemObjects.TryGetValue(ID, out List<Item> items))
        {
            if(items[0].IsStackable())
            {
                return items[0].GetCount();
            }
            else
            {
                return items.Count;
            }
        }
        else
        {
            return 0;
        }
    }

    public void SetCurItme(GameObject item)
    {
        curItem = item;
    }

}
