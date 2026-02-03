
using Mirror;
using UnityEngine;

public abstract class CharacterBase : NetworkBehaviour, ICharacter 
{
    [SyncVar]
    public string Name = "Unknown";
    [SyncVar]
    public int Level = 1;
    [SyncVar]
    public int Experience = 0;
    [SyncVar]
    public float MaxHP = 100;
    [SyncVar]
    public float CurHP= 100;
    [SyncVar]
    public float MaxMp = 100;

    [SyncVar]
    public float CurMp= 100;
    [SyncVar]
    public Status stat = new Status();
    [SyncVar]
    public bool isDead = false;

    public int GetLevel() {return Level;}
    public void SetLevel(int Inlevel){Level = Inlevel;}
    public int GetEXP(){return Experience;}
    public void SetEXP(int Inexp){Experience = Inexp;}

    public void Attack(ICharacter target)
    {
        target.TakeDamage(stat.Agility);        
    }
    public void GainExperience(int amount)
    {
        Experience += amount;

        //임시로 100단위
        while(Experience >= 100)
        {
            Experience -= 100;
            Level++;
        }
    }


    [Server]
    virtual public void TakeDamage(int damage)
    {
        int finalDamage = damage - stat.Defense;

        //최소 뎀
        if(finalDamage < 1) finalDamage = 1;

        CurHP -= finalDamage;

        if(CurHP < 0) 
        {
            CurHP = 0;
        }
    }

    public bool IsAlive()
    {
        return !isDead;
    }
}

