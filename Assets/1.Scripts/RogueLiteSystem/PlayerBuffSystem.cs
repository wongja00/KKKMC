using System;
using Mirror;
using UnityEngine;

public class PlayerBuffSystem : NetworkBehaviour
{
    //[SerializeField] private CharacterBase character;
    
    [SyncVar(hook = nameof(InvokeSelectBuffCount))]
    private int selectableBuffCount = 0;
    
    public event Action<int> OnSelectBuff;
    public event Action<BuffType, float> OnApplyBuff;

    [Command]
    public void CharacterStatUp(int id)
    {
        BuffDataObject buf = BuffManager.Instance.buffDataDic[id];

        if(buf == null)
        {
            Debug.Log("버프없");
            return;
        }

        Debug.Log("버프");
        
        OnApplyBuff?.Invoke(buf.buffType, buf.buffWeight);
    }

    [Server]
    public void IncreaseSelectBuffCount()
    {
        selectableBuffCount++;
    }

    [Server]
    public void DecreaseSelectBuffCount()
    {
        selectableBuffCount = Mathf.Max(0, selectableBuffCount - 1);
        
    }

    public int GetselectableBuffCount(){return selectableBuffCount;}

    public void InvokeSelectBuffCount(int old, int newValue)
    {
        OnSelectBuff?.Invoke(newValue);
    }
}
