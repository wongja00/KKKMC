using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;



//UI에서 보일 아이템 슬롯
public class InventorySlot : MonoBehaviour, IPointerClickHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image itemIcon;
    private Sprite originIcon;
    [SerializeField] private TextMeshProUGUI itemCountText;

    //슬롯 타입 - 초기에는 빈 슬롯으로 설정
    [SerializeField] private SlotType slotType = SlotType.NullSlot;

    //슬롯 외곽선
    [SerializeField] private GameObject slotOutLine;

    [SerializeField] private Toggle selectToggle;

    public ScriptableItemData itemData;

    [SerializeField] private string itemane;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        

        if(itemIcon == null)
        {
            itemIcon = GetComponent<Image>();
            originIcon = itemIcon.sprite;
        }
        if(itemCountText == null)
        {
            itemCountText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if(slotType == SlotType.NullSlot)
        {
            if(itemCountText != null)
            {
                itemCountText.gameObject.SetActive(false);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public SlotType GetSlotType()
    {
        return slotType;
    }

    public void OnPointerClick(PointerEventData eventData)
    {

    }

    public void OnDrag(PointerEventData eventData)
    {

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        
    }

    public void OnDrop(PointerEventData enventData)
    {

    }

    public void OnPointerEnter(PointerEventData eventData)
    {

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        
    }

    public void SetItem(Item item)
    {
        itemIcon.sprite = item.GetIcon();
        itemCountText.text = item.GetCount().ToString();

        slotType = SlotType.ActiveSlot;

        itemData = item.GetItemData();
        itemane = item.GetName();
    }

    public void SetEmptySlot()
    {
        itemIcon.sprite = originIcon;
        itemCountText.text = "";

        slotType = SlotType.NullSlot;

        itemData = null;
    }

    public void SelectSlot(bool active)
    {
        if(slotOutLine == null) return;

        if(selectToggle != null) selectToggle.isOn = active;
    }

    
}
