using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class StatusEffectHandler : MonoBehaviour
{
    [SerializeField] private VisualEffect statusEffectVFX;

    public int effectID = 0;
    public void PlayStatusEffectVFX()
    {
        if (statusEffectVFX != null)
        {
            statusEffectVFX.enabled = true;
            statusEffectVFX.Play();

        }
    }

    public void StopStatusEffectVFX()
    {
        if (statusEffectVFX != null)
        {
            statusEffectVFX.Stop();
            statusEffectVFX.enabled = false;
        }

        StatusEffectPoolManager.Instance.ReturnStatusEffectHandler(this);
    }

    IEnumerator WaitAndStopVFX(float duration)
    {
        yield return new WaitForSeconds(duration);
        StopStatusEffectVFX();
    }
}
