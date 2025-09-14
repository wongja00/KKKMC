using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;


public class WheelModuleParent : MonoBehaviour
{
    [SerializeField] private List<AttachedModule> wheelModules;
    
    public System.Action<WheelCollider, WheelType> OnWheelAttached;

    void Start()
    {
        wheelModules = new List<AttachedModule>();

        // 자식 컴포넌트의 AttachedModule을 wheelModules 리스트에 추가
        wheelModules.AddRange(GetComponentsInChildren<AttachedModule>());

        foreach(AttachedModule AM in wheelModules)
        {
            AM.OnAttach +=OnSetWheel;
        }

        SetWheelModules();
    }

    private void SetWheelModules()
    {
        if(wheelModules.Count <= 0) return;

        for (int i = 1; i < Enum.GetValues(typeof(WheelType)).Length; i++)
        {
            if (i - 1 < wheelModules.Count)
            {
                wheelModules[i - 1].SetWheelType((WheelType)i);
            }
        }
    }

    private void OnSetWheel(WheelCollider col, WheelType type)
    {
        OnWheelAttached?.Invoke(col, type);
    }



}
