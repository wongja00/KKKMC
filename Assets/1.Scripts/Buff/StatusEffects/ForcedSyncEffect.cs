using UnityEngine;

[CreateAssetMenu(fileName = "ForcedSyncEffect", menuName = "StatusEffects/ForcedSyncEffect")]
public class ForcedSyncEffect : StatusEffectBase
{
    /*
     강제동조: 내 편으로 만듬
     */

    public ForcedSyncEffect()
    {
        effectID = 10;
    }

    public override void OnApply(CharacterBase target, StatusEffectInstance instance)
    {
        target.characterTeam = Team.Player;

        StatusEffectHandler effHandle = StatusEffectPoolManager.Instance.GetStatusEffectHandler(effectID);
        effHandle.gameObject.transform.SetParent(target.transform);
        effHandle.gameObject.transform.localPosition = Vector3.zero;
        effHandle.gameObject.transform.localPosition = target.GetComponent<CapsuleCollider>().center;

        effHandle.PlayStatusEffectVFX();

        EnemyRegistry.Unregister(target.transform); 
    }
}
