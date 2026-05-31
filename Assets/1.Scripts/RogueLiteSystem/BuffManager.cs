using System;
using System.Collections.Generic;
using Mirror;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum BuffType
    {
        None,
        Speed,
        AttackSpeed,
        Agility,
        Strength,
        Health,
        Defense,
        CritChance,
        CritDamage,
    }

public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance;

    [SerializeField] private Transform buffUI;
    [SerializeField] private Transform buffCardParent;
    [SerializeField] private BuffUICard cardPrefab;
    [SerializeField] private KeyCode buffKey = KeyCode.B;

    public BuffDataObject[] buffDataObjects;

    public Dictionary<int, BuffDataObject> buffDataDic = new Dictionary<int, BuffDataObject>();
    public event Action OnApplyBuff;

    void Awake()
    {
        if(Instance == null)
            Instance = this;

    }

    private void Start()
    {
        SceneManager.sceneLoaded += GetUIOnSceneLoad;

        buffCardParent = InteractUIManager.Instance.GetBuffUIParent();
        buffUI = InteractUIManager.Instance.GetBuffUIPanel();


        foreach (BuffDataObject data in buffDataObjects)
        {
            buffDataDic.Add(data.buffID, data);
        }

        SetBuffCards();

        SetBuffUI(false);
    }

    void GetUIOnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        buffCardParent = InteractUIManager.Instance.GetBuffUIParent();
        buffUI = InteractUIManager.Instance.GetBuffUIPanel();

        SetBuffCards();

        SetBuffUI(false);
    }

    void SetBuffCards()
    {
        foreach(BuffDataObject data in buffDataDic.Values)
        {
            BuffUICard card = Instantiate(cardPrefab, buffCardParent);
            card.SetBuffCard(data.buffID);
        }

    }

    void Update()
    {
        if(Input.GetKeyDown(buffKey))
        {
            if(buffUI.gameObject.activeSelf)
                SetBuffUI(false);
            else
                SetBuffUI(true);
        }
    }

    public void SetBuffUI(bool isOn)
    {
        buffUI.gameObject.SetActive(isOn);
    }

    // 버프 적용 함수
    public void ApplyBuff(int buffID)
    {
        
        if (!buffDataDic.ContainsKey(buffID))
        {
            Debug.LogWarning($"{buffID} - 버프 없음 ");
            return;
        }

        
                
        var myPlayerObj = Mirror.NetworkClient.localPlayer;
        if (myPlayerObj == null)
        {
            Debug.LogWarning("로컬 플레이어 오브젝트를 찾을 수 없습니다.");
            return;
        }
        var playerBuffSystem = myPlayerObj.GetComponentInChildren<PlayerBuffSystem>();
        if (playerBuffSystem == null)
        {
            Debug.LogWarning("PlayerBuffSystem 컴포넌트를 찾을 수 없습니다.");
            return;
        }

            Debug.Log("버프매니저");
        playerBuffSystem.CharacterStatUp(buffID);

        //DecreaseSelectBuffCount();

        OnApplyBuff?.Invoke();
    }
}