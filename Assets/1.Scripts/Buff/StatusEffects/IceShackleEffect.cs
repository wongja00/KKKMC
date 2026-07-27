using UnityEngine;

[CreateAssetMenu(fileName = "IceShackleEffect", menuName = "StatusEffects/IceShackleEffect")]
public class IceShackleEffect : StatusEffectBase
{
    /*
        ºù°á È¿°ú(ºù°á½ºÅÃ 3°³ ÀÌ»ó)
     */

    public IceShackleEffect()
    {
        this.Duration = 60f;

        effectID = 8;
    }



    public override void OnApply(CharacterBase target, StatusEffectInstance instance)
    {
        target.SetStat(BuffType.Speed, -target.speed, instance.UID);

        ChatManager.Instance.AddSystemMessage($"{target.Name}°¡ °á¹Úºù!");

        StatusEffectHandler effHandle = StatusEffectPoolManager.Instance.GetStatusEffectHandler(effectID);
        effHandle.gameObject.transform.SetParent(target.transform);
        effHandle.gameObject.transform.localPosition = Vector3.zero;
        effHandle.gameObject.transform.localPosition = target.GetComponent<CapsuleCollider>().center;
        effHandle.PlayStatusEffectVFX();
    }

    public override void OnRemove(CharacterBase target, StatusEffectInstance instance)
    {
        target.RemoveBuff(instance.UID);

        StatusEffectHandler[] effHandles = target.GetComponentsInChildren<StatusEffectHandler>();
        foreach (var effHandle in effHandles)
        {
            if (effHandle.effectID == effectID)
                effHandle.StopStatusEffectVFX();
        }
    }
}
