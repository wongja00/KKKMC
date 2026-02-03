
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
    public int Strength;
    public int Defense;
    public int Agility;
    public int Intelligence;
}
