using UnityEngine;

[CreateAssetMenu(fileName = "ThermoFractureEffect", menuName = "StatusEffects/ThermoFractureEffect")]
public class ThermoFractureEffect : StatusEffectBase
{
    /*
        ºùÆÄ¼â ¹ÝÀÀ(ºù°á + È­»ó)

        1. ¼ø°£ Æøµô
        2. ºù°á : ÀÌµ¿¼Óµµ 50% °¨¼Ò, °ø°Ý¼Óµµ 30% °¨¼Ò
     */

    int Damage = 5;

    public ThermoFractureEffect()
    {
        this.Damage = 5;
        this.Duration = 30f;

        effectID = 9;
    }

    public ThermoFractureEffect(int Damage, float duration)
    {
        this.Damage = Damage;
        this.Duration = duration;

        effectID = 9;
    }

    public override void OnApply(CharacterBase target, StatusEffectInstance instance)
    {
        int damage = (int)((instance.stack1 * Damage) + (instance.stack2 * Damage));

        target.SetStat(BuffType.Speed, -target.speed * 0.5f, instance.UID);
        target.SetStat(BuffType.AttackSpeed, -target.stat.attackSpeed * 0.7f, instance.UID * 100);

        target.TakeDamage(damage);

        StatusEffectHandler effHandle = StatusEffectPoolManager.Instance.GetStatusEffectHandler(effectID);
        effHandle.gameObject.transform.SetParent(target.transform);
        effHandle.gameObject.transform.localPosition = Vector3.zero;
        effHandle.gameObject.transform.localPosition = target.GetComponent<CapsuleCollider>().center;
        effHandle.PlayStatusEffectVFX();

        ChatManager.Instance.AddSystemMessage($"{target.Name}ÀÌ ¿­ÆÄ¼â ¹ÝÀÀ!");
    }

    public override void OnTick(CharacterBase target, StatusEffectInstance instance)
    {
    }

    public override void OnRemove(CharacterBase target, StatusEffectInstance instance)
    {
        target.RemoveBuff(instance.UID);
        target.RemoveBuff(instance.UID * 100);

        StatusEffectHandler[] effHandles = target.GetComponentsInChildren<StatusEffectHandler>();
        foreach (var effHandle in effHandles)
        {
            if (effHandle.effectID == effectID)
                effHandle.StopStatusEffectVFX();
        }
    }
}