using UnityEngine;

public class StatusEffectBase : ScriptableObject
{
    public int ID;
    public string EffectName;
    public float TickInterval = 1f;
    public float Duration = 5f;
    public int StackStride = 1;//때릴 때마다 오를 스택  수
    public int MaxStack = 999;
    public int effectID; //비주얼 이펙트  ID

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