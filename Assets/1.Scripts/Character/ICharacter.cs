
using Mirror;

public interface ICharacter
{
    string Name{get; set;}
    int Level{get; set;}
    int Experience{get; set;}

    float MaxHP{get; set;}
    float CurHP{get; set;}
    float MaxMp{get; set;}
    float CurMp{get; set;}
    bool isDead{get; set;}
  
    Status stat{get; set;}

    void Attack(ICharacter target);

    void TakeDamage(int damage);

    void GainExperience(int amount);

    bool IsAlive();
}

public struct Status
{
    public int Strength{get; set;}
    public int Defense{get; set;}
    public int Agility{get; set;}
    public int Intelligence{get; set;}
}
