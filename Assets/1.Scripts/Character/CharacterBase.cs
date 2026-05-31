
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using System;

public abstract class CharacterBase : NetworkBehaviour, ICharacter 
{
    [SyncVar]
    public string Name = "Unknown";
    [SyncVar]
    public int Level = 1;
    [SyncVar]
    public int maxExp = 100;
    [SyncVar]
    public int curExp = 10;

    [SyncVar]
    public float MaxHP = 100;
    [SyncVar(hook = nameof(OnHpChange))]
    public float CurHP= 100;
    [SyncVar(hook = nameof(OnHpChange))]
    public float MaxMp = 100;

    [SyncVar]
    public float CurMp= 100;
    [SyncVar(hook = nameof(OnStatChange))]
    public Status stat = new Status();
    [SyncVar]
    public float speed = 5f;
    [SyncVar]
    public bool isDead = false;
    [SyncVar]
    public bool isHitStun= false;
    [SyncVar]
    public float stunDuration= 1f;

    public event Action OnHpChanged;
    public event Action OnHpDecrease;
    public event Action OnStatChanged;
    public event Action OnGetBuffServer;
    public event Action OnGetBuff;
    public event Action<Vector3, float, float> OnKnockback;

    public event Action<float> OnStun;

    public CharacterBase()
    {

    }

    void Awake()
    {
        stat.agility = 1;
        stat.strength = 1;
        stat.critChance = 1;
        stat.critDamage = 1;
        stat.attackSpeed = 1;
        stat.intelligence = 1;
        stat.defense = 1;
    }

    public void Attack(ICharacter target)
    {
        target.TakeDamage(stat.strength);  
    }
    public void GainExperience(int amount)
    {
        curExp += amount;

        while(curExp >= maxExp)
        {
            curExp -= maxExp;
            LevelUp();
        }
    }

    virtual public void TakeDamage(int damage)
    {
        int finalDamage = damage - stat.defense;

        isHitStun = true;

        //최소 뎀
        if (finalDamage < 1) finalDamage = 1;

        CurHP -= finalDamage;

        DamageTextManager.Instance.ShowDamageText(transform.position + (Vector3.up * 2), finalDamage);

        if (CurHP <= 0) 
        {
            CurHP = 0;
            
        }
    }

    [Server]
    virtual public void HitStun(float duration)
    {
        //if(isHitStun) return;

        stunDuration = duration;
        OnStun?.Invoke(stunDuration);

        Debug.Log(stunDuration + "초 스턴");
    }

    [Server]
    virtual public void CmdKnockback(Vector3 attackPos, float distance, float duration)
    {
        OnKnockback?.Invoke(attackPos, distance, duration);
    }

    virtual public void Die()
    {

    }

    [Server]
    public void ApplyBuff(BuffType buf, float Value)
    {
        Status updateStat = stat;

        switch(buf)
        {
            case BuffType.None:
            break;
            case BuffType.Agility:
                updateStat.agility += (int)Value;
            break;
            case BuffType.Strength:
                updateStat.strength += (int)Value;
            break;
            case BuffType.Defense:
                updateStat.defense += (int)Value;
            break;
            case BuffType.CritChance:
                updateStat.critChance += Value;
            break;
            case BuffType.Health:
                MaxHP += Value;
                CurHP += Value;
            break;
            case BuffType.Speed:
                speed += Value;
            break;
            case BuffType.CritDamage:
                updateStat.critDamage += Value;
            break;
            case BuffType.AttackSpeed:
                updateStat.attackSpeed += Value;
            break;
        }
        stat = updateStat;

        TargetRpcOnBuff(connectionToClient, buf, Value);
        OnGetBuffServer?.Invoke();
    }

    [TargetRpc]
    void TargetRpcOnBuff(NetworkConnection network,BuffType buf, float Value)
    {
        Debug.Log($"{buf.ToString()} {Value} 버프 적용");
        ChatManager.Instance.AddSystemMessage($"{Name}님이 {buf.ToString()} {Value} 버프를 획득했습니다!");

        OnGetBuff?.Invoke();
    }

    private void LevelUp()
    {
        Level++;
        
        //능력치 상승등

    }

    public void OnHpChange(float oldValue, float newValue)
    {
        if(newValue < oldValue)
        {
        }

            OnHpDecrease?.Invoke();
        OnHpChanged?.Invoke();
    }

    public void OnStatChange(Status oldValue, Status newValue)
    {
        OnStatChanged?.Invoke();
        Debug.Log($"스탯변화");
    }

    [Server]
    public void StatChange(Status newValue)
    {
        this.stat = newValue;
    }

    public bool IsAlive()
    {
        return !isDead;
    }
}

