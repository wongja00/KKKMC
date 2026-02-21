using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class BuffUICard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI bufName;
    [SerializeField] private TextMeshProUGUI desc;
    [SerializeField] private Image iCon;
    [SerializeField] private Button btn;

    int cardID;

    BuffType buffType;

    public event Action OnBuff;

    public void SetBuffCard(int ID)
    {
        cardID = ID;
        BuffDataObject buf = BuffManager.Instance.buffDataDic[ID];

        bufName.text = buf.buffName;
        desc.text = buf.buffDesc;
        buffType = buf.buffType;

        btn.onClick.AddListener(OnApplyBuff);
    }

    void OnApplyBuff()
    {
        OnBuff?.Invoke();

        BuffManager.Instance.ApplyBuff(cardID);

        //btn.interactable = false;
    }
}
