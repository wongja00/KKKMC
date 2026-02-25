
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

    public event Action OnHpChanged;
    public event Action OnStatChanged;
    public event Action OnGetBuffServer;
    public event Action OnGetBuff;

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

        //최소 뎀
        if(finalDamage < 1) finalDamage = 1;

        CurHP -= finalDamage;

        if(CurHP <= 0) 
        {
            CurHP = 0;
            
        }
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

        RpcOnBuff(connectionToClient, buf, Value);
        OnGetBuffServer?.Invoke();
    }

    [TargetRpc]
    void RpcOnBuff(NetworkConnection network,BuffType buf, float Value)
    {
        Debug.Log($"{buf.ToString()} {Value} 버프 적용");

        OnGetBuff?.Invoke();
    }

    private void LevelUp()
    {
        Level++;
        
        //능력치 상승등

    }

    public void OnHpChange(float oldValue, float newValue)
    {
        OnHpChanged?.Invoke();
    }

    public void OnStatChange(Status oldValue, Status newValue)
    {
        OnStatChanged?.Invoke();
        Debug.Log($"스탯변화");
    }

    public bool IsAlive()
    {
        return !isDead;
    }
}

