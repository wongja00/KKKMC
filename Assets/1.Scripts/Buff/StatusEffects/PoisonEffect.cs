using UnityEngine;

[CreateAssetMenu(fileName = "PoisonEffect", menuName = "StatusEffects/PoisonEffect")]
public class PoisonEffect : StatusEffectBase
{
    public float Damage = 5f;
    int down = 0;

    public PoisonEffect()
    {
        effectID = 2;
    }

    public PoisonEffect(float damage, float duration, int attackdown)
    {
        stackType = StackType.AddDuration;

        this.Damage = damage;
        this.Duration = duration;
        this.TickInterval = 0.1f;
        down = attackdown;

        effectID = 2;
    }

    public override void OnApply(CharacterBase target, StatusEffectInstance instance)
    {
        Debug.Log($"중독 시작 {target.name}");

        StatusEffectHandler effHandle = StatusEffectPoolManager.Instance.GetStatusEffectHandler(effectID);
        effHandle.gameObject.transform.SetParent(target.transform);
        effHandle.gameObject.transform.localPosition = Vector3.zero;
                effHandle.gameObject.transform.localPosition = target.GetComponent<CapsuleCollider>().center;
        effHandle.PlayStatusEffectVFX();
    }

    public override void OnTick(CharacterBase target, StatusEffectInstance instance)
    {
        Debug.Log($"중독피해 {Damage} 데미지");
        target.TakeDamage(((int)Damage));

        target.SetStat(BuffType.Strength, -down, instance.UID);

        ChatManager.Instance.AddSystemMessage($"{target.name}이(가) 중독으로 {Damage} 데미지를 입었습니다.");

        StatusEffectHandler[] effHandles = target.GetComponentsInChildren<StatusEffectHandler>();
        foreach (var effHandle in effHandles)
        {
            if (effHandle.effectID == effectID)
                effHandle.PlayStatusEffectVFX();
        }
    }

    public override void OnRemove(CharacterBase target, StatusEffectInstance instance)
    {
        Debug.Log($"중독끝 {target.name}");
        target.RemoveBuff(instance.UID);

        StatusEffectHandler[] effHandles = target.GetComponentsInChildren<StatusEffectHandler>();
        foreach (var effHandle in effHandles)
        {
            if (effHandle.effectID == effectID)
                effHandle.StopStatusEffectVFX();
        }
    }
}
