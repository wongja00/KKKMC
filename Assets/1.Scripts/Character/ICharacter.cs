
using Mirror;

public interface ICharacter
{
    void Attack(ICharacter target);

    void TakeDamage(int damage);

    void GainExperience(int amount);

    bool IsAlive();
}

public struct Status
{
    public int strength;     // 힘
    public int defense;      // 방어력
    public int agility;      // 민첩성
    public int intelligence; // 지능
    public float attackSpeed; //공속
    public float critChance; //치확
    public float critDamage; //치피
}
