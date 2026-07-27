using UnityEngine;

[CreateAssetMenu(fileName = "FearEffect", menuName = "StatusEffects/FearEffect")]
public class FearEffect : StatusEffectBase
{
    public FearEffect()
    {
        effectID = 5;
    }
    public FearEffect(float duration)
    {
        this.Duration = duration;
        effectID = 5;
    }


    public override void OnApply(CharacterBase target, StatusEffectInstance instance)
    {
        // Called when the status effect is applied to the target
        target.SetIsFeard(true);
            ChatManager.Instance.AddSystemMessage($"{target.Name} °øÆ÷!");

        StatusEffectHandler effHandle = StatusEffectPoolManager.Instance.GetStatusEffectHandler(effectID);
        effHandle.gameObject.transform.SetParent(target.transform);
        effHandle.gameObject.transform.localPosition = Vector3.zero;
        effHandle.gameObject.transform.localPosition = target.GetComponent<CapsuleCollider>().center;
        effHandle.PlayStatusEffectVFX();
    }

    public override void OnTick(CharacterBase target, StatusEffectInstance instance)
    {
        // Called on each tick interval
    }

    public override void OnRemove(CharacterBase target, StatusEffectInstance instance)
    {
        StatusEffectHandler[] effHandles = target.GetComponentsInChildren<StatusEffectHandler>();
        foreach (var effHandle in effHandles)
        {
            if (effHandle.effectID == effectID)
                effHandle.StopStatusEffectVFX();
        }

        // Called when the status effect is removed from the target
        // target.RemoveBuff(instance.UID);
        target.SetIsFeard(false);
    }
}
