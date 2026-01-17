using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class HotBarSlotsController : NetworkBehaviour
{
    public int slotCount {get; set;}
    public int curSelSlot {get; private set;}

    public static event Action OnChangeSlot;

    private static readonly KeyCode[] AlphaKeys = {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
        KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
        KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
    };
    
    private static readonly KeyCode[] KeypadKeys = {
        KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3,
        KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6,
        KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9, KeyCode.Keypad0
    };

    // 마우스 휠로도 슬롯을 선택할 수 있도록 기능 추가
    private float wheelInputBuffer = 0f;
    private float wheelInputDelay = 0.1f; // 너무 빠른 입력 방지용 딜레이

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        curSelSlot = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if(!isLocalPlayer) return;

        int max = Mathf.Min(slotCount, 10);
        for(int i = 0; i< max; i++)
        {
            if(Input.GetKeyDown(AlphaKeys[i]) || Input.GetKeyDown(KeypadKeys[i]))
            {
                SelectSlot(i);
                break;
            }
        }
    }
    void LateUpdate()
    {
        MouseWheelSlotControll();
    }

    private void MouseWheelSlotControll()
    {
        // 마우스 휠 입력 처리
        wheelInputBuffer -= Time.deltaTime;
        if (slotCount > 0 && wheelInputBuffer <= 0f)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                // 위로 스크롤: 다음 슬롯
                int nextSlot = (curSelSlot + 1) % Mathf.Min(slotCount, 10);
                SelectSlot(nextSlot);
                wheelInputBuffer = wheelInputDelay;
            }
            else if (scroll < 0f)
            {
                // 아래로 스크롤: 이전 슬롯
                int prevSlot = (curSelSlot - 1 + Mathf.Min(slotCount, 10)) % Mathf.Min(slotCount, 10);
                SelectSlot(prevSlot);
                wheelInputBuffer = wheelInputDelay;
            }
        }

    }

    private void SelectSlot(int i)
    {
        curSelSlot = i;

        OnChangeSlot?.Invoke();
    }
}
