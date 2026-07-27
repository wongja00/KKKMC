using UnityEngine;

[CreateAssetMenu(fileName = "ElectricEffect", menuName = "StatusEffects/ElectricEffect")]
public class ElectricEffect : StatusEffectBase
{
    public ElectricEffect()
    {
        Duration = 10f; // Duration in seconds
        TickInterval = 1f; // Time between each tick in seconds

        effectID = 4;
    }

    public ElectricEffect(float duration, float tickInterval)
    {
        Duration = duration;
        TickInterval = tickInterval;

        effectID = 4;
    }

    public override void OnApply(CharacterBase target, StatusEffectInstance instance)
    {
        // Called when the status effect is applied to the target
            ChatManager.Instance.AddSystemMessage($"{target.Name} °¨Àü!");

        StatusEffectHandler effHandle = StatusEffectPoolManager.Instance.GetStatusEffectHandler(effectID);
        effHandle.gameObject.transform.SetParent(target.transform);
        effHandle.gameObject.transform.localPosition = Vector3.zero;
        effHandle.gameObject.transform.localPosition = target.GetComponent<CapsuleCollider>().center;
        effHandle.PlayStatusEffectVFX();
    }

    public override void OnTick(CharacterBase target, StatusEffectInstance instance)
    {
        target.CmdHitStun(0.25f);
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
        //target.RemoveBuff(instance.UID);
    }

}
