using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System;

public class ItemController : MonoBehaviour
{
    [SerializeField] private KeyCode itemUseKeyCode = KeyCode.Mouse1;

    public event Action<int> OnUseItemEvent;

    public event Action<GameObject> OnAttachItem;

    public event Action<GameObject> OnDetachItem;

    public int curSelSlot = 0;

    private GameObject curItem;

    int mask;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mask = LayerMask.GetMask("Slot");

        OnAttachItem += AttachedModuleItem;
        OnDetachItem += DetachModule;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(itemUseKeyCode))
        {
            useItem();
        }
    }

    public void useItem()
    {
        OnUseItemEvent?.Invoke(curSelSlot);
        
        if(curItem != null)
        {
            OnAttachItem?.Invoke(curItem);
        }
    }

    public void AttachedModuleItem(GameObject AttachItem)
    {   
        Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, 3f, mask);
        
        if(hit.collider != null)
        {
            AttachedModule slot = hit.collider.gameObject.GetComponent<AttachedModule>();
            
            if(AttachItem != null)
            {
                GameObject item = Instantiate(AttachItem);
                slot.SetModule(item);
                
                curItem = null;
            }
        }
    }

    public void DetachModule(GameObject DetachItem)
    {
        Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, 3f, mask);
        
        if(hit.collider != null)
        {
            
        }
    }

    public void SetCurSelSlot(in int sel)
    {
        curSelSlot = sel;
    }

    public void SetCurObject(GameObject gameObject)
    {
        curItem = gameObject;
    }

}
