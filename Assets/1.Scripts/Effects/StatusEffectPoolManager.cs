using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StatusEffectPoolManager : MonoBehaviour
{
    /*
     출: 1
     독: 2
    화상: 3
    감전: 4
    공포:5
    냉기: 6
    패혈증:7
    빙결:8
    열파쇄:9  
    강제동조: 10
     */

    public static StatusEffectPoolManager Instance;

    private Dictionary<int, Queue<StatusEffectHandler>> poolDictionary = new Dictionary<int, Queue<StatusEffectHandler>>();

    [SerializeField] private Transform bleedPool;
    [SerializeField] private Transform poisonPool;
    [SerializeField] private Transform burnPool;
    [SerializeField] private Transform sparkPool;
    [SerializeField] private Transform freezePool;
    [SerializeField] private Transform fearPool;
    [SerializeField] private Transform sepsisPool;
    [SerializeField] private Transform icePool;
    [SerializeField] private Transform thermoPool;
    [SerializeField] private Transform forcePool;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        InitEffects();
        //SceneManager.sceneLoaded += LoadInitEffects;
    }

    private void Start()
    {
    }

    void LoadInitEffects(Scene scene, LoadSceneMode mode)
    {
        InitEffects();
    }

    void InitEffects()
    {
        InitializePool(bleedPool, 1);
        InitializePool(poisonPool, 2);
        InitializePool(burnPool, 3);
        InitializePool(sparkPool, 4);
        InitializePool(fearPool, 5);
        InitializePool(freezePool, 6);
        InitializePool(sepsisPool, 7);
        InitializePool(icePool, 8);
        InitializePool(thermoPool, 9);
        InitializePool(forcePool, 10);
    }

    private void InitializePool(Transform poolParent, int ID)
    {
        int effectID = ID;
        Queue<StatusEffectHandler> effectQueue = new Queue<StatusEffectHandler>();

        foreach (Transform child in poolParent)
        {
            StatusEffectHandler handler = child.GetComponent<StatusEffectHandler>();
            handler.effectID = effectID;
            if (handler != null)
            {
                handler.gameObject.SetActive(false);
                effectQueue.Enqueue(handler);
            }
        }
        poolDictionary.Add(effectID, effectQueue);
    }

    public StatusEffectHandler GetStatusEffectHandler(int effectID)
    {
        if (poolDictionary.TryGetValue(effectID, out Queue<StatusEffectHandler> effectQueue))
        {
            if (effectQueue.Count > 0)
            {
                StatusEffectHandler handler = effectQueue.Dequeue();
                handler.gameObject.SetActive(true);
                return handler;
            }
            else
            {
                Debug.LogWarning($"사용할수 있는 이펙트 없음 {effectID}");
                return null;
            }
        }
        else
        {
            Debug.LogError($"풀에 이펙트 없음: {effectID}");
            return null;
        }
    }

    //사용하고 난뒤 다시 풀에 넣어주기
    public void ReturnStatusEffectHandler(StatusEffectHandler handler)
    {
        int effectID = handler.effectID;
        if (poolDictionary.TryGetValue(effectID, out Queue<StatusEffectHandler> effectQueue))
        {
            handler.gameObject.SetActive(false);
            effectQueue.Enqueue(handler);
        }
        else
        {
            Debug.LogError($"풀에 이펙트 없음: {effectID}");
        }
    }

    
}