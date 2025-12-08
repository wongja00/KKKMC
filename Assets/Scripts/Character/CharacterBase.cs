
using UnityEngine;

public abstract class CharacterBase : MonoBehaviour, ICharacter 
{
    public string Name {get; set;} = "Unknown";
    public int Level {get; set;} = 1;
    public int Experience {get; set;} = 0;
    public float MaxHP {get; set;} = 100;
    public float CurHP{get; set;} = 100;
    public float MaxMp{get; set;} = 100;
    public float CurMp{get; set;} = 100;
    public Status stat{get; set;} = new Status();
    public bool isDead{get; set;} = false;

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

