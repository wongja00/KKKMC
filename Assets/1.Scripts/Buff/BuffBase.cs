using UnityEngine;
using System.Collections.Generic;

public enum CombatEventType 
{ 
    Attack, 
    Hit, 
    Critical, 
    Kill, 
    Dodge, 
    TakeDamage, 
    StatusApplied, 
    Dash 
}
public enum BuffTag 
{ 
    Attack, 
    Survival, 
    Dodge, 
    Bleed, 
    Burn, 
    Shock, 
    Poison, 
    Freeze, 
    Critical, 
    Explosion, 
    Summon, 
    Risk 
}

public enum  DamageType
{
    Physical,
    Fire,
    Electric,
    Ice,
    Poison
}

public abstract class BuffBase : ScriptableObject
{
    public abstract void OnEvent(
        CombatEventType eventType, 
        CombatEventData eventData);

    public int ID;
    public int UID;//각 버프별 아이디
    public string BuffName;
    public float TickInterval = 1f;
    public float Duration = 5f;
    public int StackStride = 1;//얻을때마다 오를 버프스택

    public StackType stackType = StackType.Ignore;
    public virtual void OnApply(CharacterBase target, StatusEffectInstance instance)
    {
        // Called when the status effect is applied to the target
    }

    public virtual void OnTick(CharacterBase target, StatusEffectInstance instance)
    {
        // Called on each tick interval
    }

    public virtual void OnRemove(CharacterBase target, StatusEffectInstance instance)
    {
        // Called when the status effect is removed from the target
    }

    public virtual void OnAttack(CharacterBase target, StatusEffectInstance instance)
    {
        // Called when the target attacks
    }

    public virtual void OnMove(CharacterBase target, StatusEffectInstance instance)
    {
        // Called when the target moves
    }

    public virtual void SetStack(int stack)
    {

    }
}
public class CombatEventData 
{ 
    public GameObject Attacker; 
    public GameObject Target; 
    public float Damage; 
    public bool IsCritical;
    public DamageType DamageType;
    public Vector3 HitPoint;
    public List<BuffTag> Tags = new();
}