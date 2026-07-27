
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;


public enum Team
{
    Player,
    Enemy,
    Other
}

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
    public float speed = 7f;
    [SyncVar]
    public bool isDead = false;
    [SyncVar]
    public bool isHitStun= false;
    [SyncVar]
    public float stunDuration= 1f;
    [SyncVar(hook = nameof(OnIsMove))]
    public bool isMove = false;
    [SyncVar(hook = nameof(OnIsAttacking))]
    public bool isAttacking = false;
    [SyncVar(hook = nameof(OnIsFeard))]
    public bool isFeard = false;

    [SerializeField] StatusEffectManager statusEffectManager;

    public event Action OnHpChanged;
    public event Action OnHpDecrease;
    public event Action OnStatChanged;
    public event Action OnGetBuffServer;
    public event Action OnGetBuff;
    public event Action<CharacterBase> OnAttack;
    public event Action<CharacterBase> OnMove;
    public event Action<Vector3, float, float> OnKnockback;

    public event Action<float> OnStun;

    [SyncVar]
    public float curDistance = 0f;

    [SyncVar]
    public Vector3 prePos = Vector3.zero;

    [SyncVar]
    public List<BuffSyncData> activeDeBuffs = new List<BuffSyncData>();

    public Team characterTeam = Team.Player;
    public CharacterBase()
    {

    }

    public virtual void Awake()
    {
        SetPrePos(transform.position);
        stat.agility = 1;
        stat.strength = 1;
        stat.critChance = 1;
        stat.critDamage = 1;
        stat.attackSpeed = 1;
        stat.intelligence = 1;
        stat.defense = 1;
    }

    public virtual void Update()
    {
        if(isMove)
        {
            curDistance += Vector3.Distance(prePos, transform.position);
            SetDistance(curDistance);

            OnMove?.Invoke(this);
            SetPrePos(transform.position);
        }
        else
        {
            curDistance = 0f;
            SetDistance(curDistance);
        }
    }

    [Command(requiresAuthority=false)]
    public void SetDistance(float distance)
    {
        curDistance = distance;
    }

    [Command(requiresAuthority = false)]
    public void SetPrePos(Vector3 pos)
    {
        prePos = pos;
    }

    [Command(requiresAuthority = false)]
    public void SetIsMove(bool value)
    {
        isMove = value;
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
        if (finalDamage < 1) finalDamage = 1;

        CurHP -= finalDamage;

        DamageTextManager.Instance.ShowDamageText(transform.position + (Vector3.up * 2), finalDamage);

        if (CurHP <= 0) 
        {
            CurHP = 0;
            
        }
    }

    [Server]
    virtual public void AddStatusEffect(StatusEffectBase effect)
    {
        statusEffectManager.AddEffect(effect);
    }

    [Server]
    virtual public void HitStun(float duration)
    {
        //if(isHitStun) return;

        isHitStun = true;
        stunDuration = duration;
        OnStun?.Invoke(stunDuration);

        Debug.Log(stunDuration + "초 스턴");
    }

    [Command(requiresAuthority = false)]
    public void CmdHitStun(float duration)
    {
        HitStun(duration);
    }

    [Server]
    virtual public void CmdKnockback(Vector3 attackPos, float distance, float duration)
    {
        OnKnockback?.Invoke(attackPos, distance, duration);
    }

    virtual public void Die()
    {
        switch(characterTeam)
        {
            case Team.Player:
                PlayerRegistry.Unregister(this.transform);

                break;
            case Team.Enemy: 
                break;

            default:
                break;
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

        TargetRpcOnBuff(connectionToClient, buf, Value);
        OnGetBuffServer?.Invoke();
    }

    [Command(requiresAuthority = false)]
    public void RemoveBuff(int bufID)
    {
        BuffSyncData buffInstance = activeDeBuffs.Find(b => b.ID == bufID);

        if(buffInstance.ID == -1)
        {
            Debug.LogWarning($"버프 ID {bufID}를 찾을 수 없습니다.");
            return;
        }

        if (buffInstance.ID != -1)
        {
            Status updateStat = stat;
            switch (buffInstance.Type)
            {
                case BuffType.None:
                    break;
                case BuffType.Agility:
                    updateStat.agility -= (int)buffInstance.Value;
                    break;
                case BuffType.Strength:
                    updateStat.strength -= (int)buffInstance.Value;
                    break;
                case BuffType.Defense:
                    updateStat.defense -= (int)buffInstance.Value;
                    break;
                case BuffType.CritChance:
                    updateStat.critChance -= buffInstance.Value;
                    break;
                case BuffType.Health:
                    MaxHP -= buffInstance.Value;
                    CurHP -= buffInstance.Value;
                    break;
                case BuffType.Speed:
                    speed -= buffInstance.Value;
                    break;
                case BuffType.CritDamage:
                    updateStat.critDamage -= buffInstance.Value;
                    break;
                case BuffType.AttackSpeed:
                    updateStat.attackSpeed -= buffInstance.Value;
                    break;
            }
            stat = updateStat;
            activeDeBuffs.Remove(buffInstance);
        }
    }

    [Command(requiresAuthority = false)]
    public void SetStat(BuffType buf, float Value, int bufID)
    {
        //일단 임시적으로 버프는 아래와 같이 적용함
        BuffSyncData buffInstance = new BuffSyncData();

        buffInstance.ID = bufID;

        buffInstance.Type = buf;
        buffInstance.Value = Value;

        activeDeBuffs.Add(buffInstance);

        Status updateStat = stat;

        switch (buf)
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

        ChatManager.Instance.AddSystemMessage($"{Name}님이 {buf.ToString()}: {Value} 스탯변화");
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

    [Command(requiresAuthority = false)]
    public void SetStat(Status newValue)
    {
        this.stat = newValue;
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

    void OnIsMove(bool oldValue, bool newValue)
    {
        isMove = newValue;

        if(newValue)
        {
            OnMove?.Invoke(this);
        }
    }
     void OnIsAttacking(bool oldValue, bool newValue)
    {
        isAttacking = newValue;

        if(newValue)
        {
            OnAttack?.Invoke(this);
        }
    }

    void OnIsFeard(bool oldValue, bool newValue)
    {
        isFeard = newValue;
        if(newValue)
        {
            Debug.Log("공포 상태");
        }
    }

    [Command(requiresAuthority = false)]
    public void SetIsFeard(bool value)
    {
        isFeard = value;
    }
}

