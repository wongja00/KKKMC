
using Mirror;

public interface ICharacter
{
    void Attack(ICharacter target);

    void TakeDamage(int damage);

    void GainExperience(int amount);

    bool IsAlive();
}

[System.Serializable]
public struct Status
{
    Status(Status other)
    {
        this.strength = other.strength;
        this.defense = other.defense;
        this.agility = other.agility;
        this.intelligence = other.intelligence;
        this.attackSpeed = other.attackSpeed;
        this.critChance = other.critChance;
        this.critDamage = other.critDamage;
    }

    public int strength;     // 힘
    public int defense;      // 방어력
    public int agility;      // 민첩성
    public int intelligence; // 지능
    public float attackSpeed; //공속
    public float critChance; //치확
    public float critDamage; //치피
}
