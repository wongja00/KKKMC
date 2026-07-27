using UnityEngine;

[CreateAssetMenu(fileName = "FreezeEffect", menuName = "StatusEffects/FreezeEffect")]
public class FreezeEffect : StatusEffectBase
{
    public FreezeEffect ()
    {
        effectID = 6;
    }

    public FreezeEffect (float duration)
    {
        this.Duration = duration;

        effectID = 6;
    }


    public override void OnApply(CharacterBase target, StatusEffectInstance instance)
    {
        ChatManager.Instance.AddSystemMessage($"{target.Name} °¡ ¾óÀ½!");

        target.SetStat(BuffType.Speed,  -target.speed/2f, instance.UID);

        StatusEffectHandler effHandle = StatusEffectPoolManager.Instance.GetStatusEffectHandler(effectID);
        effHandle.gameObject.transform.SetParent(target.transform);
        effHandle.gameObject.transform.localPosition = Vector3.zero;
        effHandle.gameObject.transform.localPosition = target.GetComponent<CapsuleCollider>().center;
        effHandle.PlayStatusEffectVFX();
    }


    public override void OnTick(CharacterBase target, StatusEffectInstance instance)
    {

    }

    public override void OnRemove(CharacterBase target, StatusEffectInstance instance)
    {
        StatusEffectHandler[] effHandles = target.GetComponentsInChildren<StatusEffectHandler>();
        foreach (var effHandle in effHandles)
        {
            if (effHandle.effectID == effectID)
                effHandle.StopStatusEffectVFX();
        }

        target.RemoveBuff(instance.UID);
    }
}
