using UnityEngine;

[CreateAssetMenu(fileName = "BurnEffect", menuName = "StatusEffects/BurnEffect")]
public class BurnEffect : StatusEffectBase
{
    int Damage = 20;
    int down = 0;

    public BurnEffect()
    {
        effectID = 3;
    }

    public BurnEffect(int damage, float duration, int defensedown)
    {
        stackType = StackType.Stack;

        Damage = damage;
        Duration = duration;
        down = defensedown;
        StackStride = 2;

        effectID = 3;
    }

    public override void OnApply(CharacterBase target, StatusEffectInstance instance)
    {
        Debug.Log($"화상 시작 {target.name}");

        StatusEffectHandler effHandle = StatusEffectPoolManager.Instance.GetStatusEffectHandler(effectID);
        effHandle.gameObject.transform.SetParent(target.transform);
        effHandle.gameObject.transform.localPosition = Vector3.zero;
        effHandle.gameObject.transform.localPosition = target.GetComponent<CapsuleCollider>().center;
        effHandle.PlayStatusEffectVFX();
    }
    
    public override void OnTick(CharacterBase target, StatusEffectInstance instance)
    {
        Debug.Log($"화상 피해 {Damage * instance.Stack} 데미지");
        target.TakeDamage(Damage * instance.Stack);

        target.SetStat(BuffType.Defense, -down, instance.UID);
        ChatManager.Instance.AddSystemMessage($"{target.name}이(가) 화상으로 {Damage * instance.Stack} 데미지를 입었습니다.");
    }


    public override void OnRemove(CharacterBase target, StatusEffectInstance instance)
    {
        StatusEffectHandler[] effHandles = target.GetComponentsInChildren<StatusEffectHandler>();
        foreach (var effHandle in effHandles)
        {
            if (effHandle.effectID == effectID)
                effHandle.StopStatusEffectVFX();
        }

        Debug.Log($"화상 끝 {target.name}");
        target.RemoveBuff(instance.UID);
    }


}
