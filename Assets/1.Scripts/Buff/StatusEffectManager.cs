using Mirror;
using Mirror.BouncyCastle.Asn1.Crmf;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
public class StatusEffectInstance
{
    public StatusEffectInstance(int stack1, int stack2) 
    {
        this.stack1 = stack1;
        this.stack2 = stack2;
    }

    public StatusEffectInstance()
    {
    }

    public int UID;
    public StatusEffectBase Data;
    public float RemainingTime;
    public float TickTimer;
    public int Stack;
    public Action<CharacterBase> OnAttack;
    public Action<CharacterBase> OnMove;
    public StatusEffectType effectType;

    //이펙트
    public StatusEffectHandler effectHandler;

    //조합때 쓸 스택
    public int stack1;
    public int stack2;
}
public struct StatusEffectSyncData
{
    public int UID;
    public int EffectID;
    public float RemainingTime;
    public int Stack;
}

//상태이상을 줄수있는 구조체
[System.Serializable]
public struct EffectApplyData
{
    public StatusEffectBase Effect;

    public float Chance;

    public int Stack;

    public float DurationMultiplier;
}

public enum StatusEffectType
{
    Buff,
    Debuff
}

public enum StackType
{
    Refresh,//갱신
    Stack,//중첩
    Independent,//일단 무시
    AddDuration,//지속시간 추가 
    Ignore//무시
}

public class StatusEffectManager : NetworkBehaviour
{
    List<StatusEffectInstance> activeEffects = new List<StatusEffectInstance>();
    readonly SyncList<StatusEffectSyncData> syncActiveEffects = new SyncList<StatusEffectSyncData>();

    int nextUID = 1;

    [SerializeField] CharacterBase character;

    public override void OnStartClient()
    {
        syncActiveEffects.OnAdd += OnEffectAdded;
        syncActiveEffects.OnRemove += OnEffectRemoved;
        syncActiveEffects.OnSet += OnEffectChanged;
    }
    public override void OnStopClient()
    {
        syncActiveEffects.OnAdd -= OnEffectAdded;
        syncActiveEffects.OnRemove -= OnEffectRemoved;
        syncActiveEffects.OnSet -= OnEffectChanged;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!isServer) return;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffectInstance effect = activeEffects[i];
            EffectUpdate(effect, i);
        }
    }

    public void EffectUpdate(StatusEffectInstance effect, int index)
    {
        effect.TickTimer -= Time.deltaTime;
        effect.RemainingTime -= Time.deltaTime;

        if (effect.TickTimer <= 0)
        {
            effect.TickTimer = effect.Data.TickInterval;

            effect.Data.OnTick(character, effect);
        }

        if(effect.RemainingTime <= 0)
        {
            RemoveEffect(effect);
        }
    }

    [Server]
    public void AddEffect(StatusEffectBase effectData)
    {
        switch(effectData.stackType)
        {
            case StackType.Refresh:
            {
                StatusEffectInstance existing = activeEffects.Find(e => e.Data.ID == effectData.ID);
                if(existing != null)
                {
                    existing.RemainingTime = effectData.Duration;
                    int effectIndex = FindEffectIndexByUID(existing.UID);
                    if(effectIndex != -1)
                    {
                        syncActiveEffects[effectIndex] = new StatusEffectSyncData
                        {
                            UID = existing.UID,
                            EffectID = effectData.ID,
                            RemainingTime = existing.RemainingTime,
                            Stack = existing.Stack
                        };
                    }
                    return;
                }
                break;
            }
            case StackType.Stack:
            {
                StatusEffectInstance existing = activeEffects.Find(e => e.Data.ID == effectData.ID);
                if(existing != null)
                {
                    existing.Stack += effectData.StackStride;
                    int effectIndex = FindEffectIndexByUID(existing.UID);
                    if(effectIndex != -1)
                    {
                        syncActiveEffects[effectIndex] = new StatusEffectSyncData
                        {
                            UID = existing.UID,
                            EffectID = effectData.ID,
                            RemainingTime = existing.RemainingTime,
                            Stack = existing.Stack
                        };
                    }
                    return;
                }
                break;
            }
            case StackType.AddDuration:
            {
                StatusEffectInstance existing = activeEffects.Find(e => e.Data.ID == effectData.ID);
                if(existing != null)
                {
                    existing.RemainingTime += effectData.Duration;
                    int effectIndex = FindEffectIndexByUID(existing.UID);
                    if(effectIndex != -1)
                    {
                        syncActiveEffects[effectIndex] = new StatusEffectSyncData
                        {
                            UID = existing.UID,
                            EffectID = effectData.ID,
                            RemainingTime = existing.RemainingTime,
                            Stack = existing.Stack
                        };
                    }
                    return;
                }
                break;
            }
            case StackType.Ignore:
            {
                if(activeEffects.Exists(e => e.Data.ID == effectData.ID))
                {
                    return;
                }
                break;
            }
            case StackType.Independent:
            {
                    return;
                break;
            }

        }

        StatusEffectInstance instance = new StatusEffectInstance();

        instance.UID = nextUID++;

        instance.Data = effectData;

        instance.RemainingTime = effectData.Duration;

        instance.TickTimer = effectData.TickInterval;

        instance.Stack = effectData.StackStride;

        syncActiveEffects.Add(new StatusEffectSyncData
        {
            UID = instance.UID,
            EffectID = effectData.ID,
            RemainingTime = instance.RemainingTime,
            Stack = instance.Stack
        });

        activeEffects.Add(instance);

        effectData.OnApply(character, instance);


        instance.OnAttack = (CharacterBase target) => effectData.OnAttack(target, instance);
        instance.OnMove = (CharacterBase target) => effectData.OnMove(target, instance);
        character.OnAttack += instance.OnAttack;
        character.OnMove += instance.OnMove;


    }

    void OnEffectAdded(int index)
    {
        var effect = syncActiveEffects[index];

        Debug.Log("상태이상 추가");
    }

    void OnEffectRemoved(int index, StatusEffectSyncData effect)
    {
        Debug.Log("상태이상 제거");


    }

    void OnEffectChanged(int index, StatusEffectSyncData newEffect)
    {
        Debug.Log("상태이상 변경");
    }

    int FindEffectIndexByUID(int uid)
    {
        for (int i = 0; i < syncActiveEffects.Count; i++)
        {
            if (syncActiveEffects[i].UID == uid)
                return i;
        }
        return -1;
    }

    StatusEffectInstance FindEffectInstanceByUID(int uid)
    {
        return activeEffects.Find(e => e.UID == uid);
    }

    void RemoveEffect(StatusEffectInstance effect)
    {
        int activeIndex =
            activeEffects.FindIndex(e => e.UID == effect.UID);

        int syncIndex =
            FindEffectIndexByUID(effect.UID);

        effect.Data.OnRemove(character, effect);

        if (activeIndex != -1)
        {
            character.OnAttack -= activeEffects[activeIndex].OnAttack;
            character.OnMove -= activeEffects[activeIndex].OnMove;
            activeEffects.RemoveAt(activeIndex);
        }

        if (syncIndex != -1)
            syncActiveEffects.RemoveAt(syncIndex);
    }

    public void ConsumeStack(StatusEffectInstance effect)
    {
        effect.Stack--;

        if (effect.Stack <= 0)
        {
            RemoveEffect(effect);
            return;
        }

        int syncIndex =
        FindEffectIndexByUID(effect.UID);

        if (syncIndex != -1)
        {
            syncActiveEffects[syncIndex] =
                new StatusEffectSyncData
                {
                    UID = effect.UID,
                    EffectID = effect.Data.ID,
                    RemainingTime = effect.RemainingTime,
                    Stack = effect.Stack
                };
        }
    }
}