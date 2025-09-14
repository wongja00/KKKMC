using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HotBarSlotsController : MonoBehaviour
{
    public int slotCount {get; set;}
    public int curSelSlot {get; private set;}

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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        curSelSlot = 0;
    }

    // Update is called once per frame
    void Update()
    {
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

    private void SelectSlot(int i)
    {
        curSelSlot = i;
    }
}
