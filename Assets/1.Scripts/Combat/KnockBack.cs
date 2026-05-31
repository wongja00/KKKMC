using UnityEngine;
using System.Collections;
using Mirror;

public class KnockBack : NetworkBehaviour
{
    [SerializeField] private AnimationCurve knockBackCurve;
    [SerializeField] CharacterBase characterBase;
    private bool isKnockBack = false;

    Coroutine knockBackCoroutine;

    private void Awake()
    {
        characterBase.OnKnockback += ServerApplyKnockback;
    }

    [Server]
    public void ServerApplyKnockback(Vector3 attackPos, float distance, float duration)
    {
        Vector3 knockbackDirection = (transform.position - attackPos).normalized;
        knockbackDirection.y = 0; 

        Vector3 targetPosition = transform.position + knockbackDirection * distance;

        RpcPlayKnockback(targetPosition, duration);
    }

    [ClientRpc]
    private void RpcPlayKnockback(Vector3 targetPos, float duration)
    {
        if (isKnockBack) StopCoroutine(knockBackCoroutine);

        knockBackCoroutine = StartCoroutine(KnockbackCoroutine(targetPos, duration));
    }

    private IEnumerator KnockbackCoroutine(Vector3 targetPos, float duration)
    {
        isKnockBack = true;
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float curveValue = knockBackCurve.Evaluate(t);

            transform.position = Vector3.Lerp(startPos, targetPos, curveValue);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        isKnockBack = false;
    }
}
