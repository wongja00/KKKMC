using UnityEngine;

[CreateAssetMenu(fileName = "SepsisEffect", menuName = "StatusEffects/SepsisEffect")]
public class SepsisEffect : StatusEffectBase
{
    /*
        패혈 반응(출혈 + 독)
        영구 체력 깎 + 공격력 스택당 10%감소 최대 5스택
     */


    public SepsisEffect()
    {
        stackType = StackType.Stack;

        this.Duration = 10f;

        effectID = 7;
    }

    public SepsisEffect(float duration, float interval)
    {
        stackType = StackType.Stack;

        this.Duration = duration;
        this.TickInterval = interval;

        effectID = 7;
    }

    public override void OnApply(CharacterBase target, StatusEffectInstance instance)
    {
        target.SetStat(BuffType.Health, (target.MaxHP * 0.1f * instance.Stack), instance.UID);
        target.SetStat(BuffType.Strength, -(target.stat.strength * 0.1f * instance.Stack), instance.UID * 100);

        ChatManager.Instance.AddSystemMessage($"{target.Name} 패혈!");

        StatusEffectHandler effHandle = StatusEffectPoolManager.Instance.GetStatusEffectHandler(effectID);
        effHandle.gameObject.transform.SetParent(target.transform);
        effHandle.gameObject.transform.localPosition = Vector3.zero;
        effHandle.gameObject.transform.localPosition = target.GetComponent<CapsuleCollider>().center;
        effHandle.PlayStatusEffectVFX();

    }

    public override void OnTick(CharacterBase target, StatusEffectInstance instance)
    {
        target.TakeDamage((int)(target.stat.strength * 0.1f * instance.Stack));

        ChatManager.Instance.AddSystemMessage($"{target.Name} 패혈로 인한 {(int)(target.stat.strength * 0.1f * instance.Stack)} 피해!");

        StatusEffectHandler[] effHandles = target.GetComponentsInChildren<StatusEffectHandler>();
        foreach (var effHandle in effHandles)
        {
            if(effHandle.effectID == effectID)
                effHandle.PlayStatusEffectVFX();
        }
    }

    public override void OnRemove(CharacterBase target, StatusEffectInstance instance)
    {
        StatusEffectHandler[] effHandles = target.GetComponentsInChildren<StatusEffectHandler>();
        foreach (var effHandle in effHandles)
        {
            if (effHandle.effectID == effectID)
                effHandle.StopStatusEffectVFX();
        }

        target.RemoveBuff(instance.UID * 100);
    }
}
